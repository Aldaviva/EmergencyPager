using EmergencyPager.Toast;
using EmergencyPager.Toast.Data;
using EmergencyPager.Toast.Eventing;
using EmergencyPager.Toast.PagerDuty;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
using RuntimeUpgrade.Notifier;
using RuntimeUpgrade.Notifier.Data;
using ThrottleDebounce.Retry;
using Unfucked.DI;
using Unfucked.DI.Logging;
using Unfucked.HTTP;

Version.PrintProgramVersionAndExitIfRequested();

using CancellationTokenSource cts        = new CancellationTokenSource().CancelOnCtrlC();
Func<long, TimeSpan>          retryDelay = Delays.Exponential(TimeSpan.FromSeconds(1), max: TimeSpan.FromMinutes(5));

HostApplicationBuilder appBuilder = Host.CreateApplicationBuilder(args);

appBuilder.Configuration.AddJsonFile(Environment.ExpandEnvironmentVariables(@"%appdata%\EmergencyPager\Toast.config.json"), false, true, true);
appBuilder.Logging.AddUnfuckedConsole();

appBuilder.Services
    .Configure<Configuration>(appBuilder.Configuration)
    .AddSingleton(new UnfuckedHttpClient(), SuperRegistration.Superclasses)
    .AddSingleton<ToastHandler, ToastHandlerImpl>()
    .AddSingleton<PagerDutyRestClientFactory, PagerDutyRestClientFactoryImpl>();

bool isToastCallback = ToastNotificationManagerCompat.WasCurrentProcessToastActivated();
if (!isToastCallback) {
    appBuilder.Services
        .AddSingleton(provider => new HubConnectionBuilder()
            .WithUrl(provider.GetRequiredService<IOptions<Configuration>>().Value.hubAddress)
            .WithAutomaticReconnect(new DelayRetry(retryDelay))
            .ConfigureLogging(hubBuilder => {
                hubBuilder.AddConfiguration(appBuilder.Configuration.GetSection("Logging"));
                hubBuilder.AddUnfuckedConsole();
                // Use the same ConsoleFormatter instance as the outer context so the stateful automatic column width is the same, instead of using two instances where the columns would jump around depending on the source
                hubBuilder.Services.Remove(hubBuilder.Services.First(static service => service.ImplementationType == typeof(UnfuckedConsoleFormatter)));
                hubBuilder.Services.AddSingleton<ConsoleFormatter>(_ => provider.GetServices<ConsoleFormatter>().OfType<UnfuckedConsoleFormatter>().First());
            })
            .Build())
        .AddSingleton<IHubClient, HubClient>();
}

using IHost app = appBuilder.Build();

ToastHandler toastHandler = app.Services.GetRequiredService<ToastHandler>();
ToastNotificationManagerCompat.OnActivated += async e => {
    await toastHandler.onToastInteraction(ToastArguments.Parse(e.Argument));
    if (isToastCallback) {
        await cts.CancelAsync();
    }
};

if (!isToastCallback) {
    var           logger       = app.Services.GetRequiredService<ILogger<Program>>();
    Configuration config       = app.Services.GetRequiredService<IOptions<Configuration>>().Value;
    RetryOptions  retryOptions = new() { Delay = retryDelay, CancellationToken = cts.Token };
    IHubClient    hubClient    = app.Services.GetRequiredService<IHubClient>();

    hubClient.incidentUpdated += toastHandler.onIncidentUpdated;
    hubClient.HubConnection.Closed += e => {
        if (!cts.IsCancellationRequested) {
            logger.Warn("Connection to eventing socket closed: {msg}", e?.Message);
        }
        return Task.CompletedTask;
    };
    logger.Debug("Connecting to eventing socket on {url}", config.hubAddress);
    await Retrier.Attempt(async _ => await hubClient.HubConnection.StartAsync(cts.Token), retryOptions with {
        AfterFailure = (e, _) => logger.Warn(e, "Connection to eventing socket failed, will retry"),
        BeforeRetry = (_, retryNumber) => logger.Debug("Connecting to eventing socket (attempt #{attempt:N0})", retryNumber + 1),
    });
    logger.Info("Connected, waiting for socket events about incident updates");
}

using RuntimeUpgradeNotifier runtimeUpgrades = new() {
    RestartStrategy = RestartStrategy.AutoRestartProcess,
    ExitStrategy    = new HostedLifetimeExit(app),
    LoggerFactory   = app.Services.GetRequiredService<ILoggerFactory>()
};

await app.RunAsync(cts.Token);

ToastNotificationManagerCompat.Uninstall();