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
using Unfucked.DI.Logging;
using Unfucked.HTTP;

using CancellationTokenSource cts = new CancellationTokenSource().CancelOnCtrlC();

HostApplicationBuilder appBuilder = Host.CreateApplicationBuilder(args);

appBuilder.Configuration.AddJsonFile(Environment.ExpandEnvironmentVariables(@"%appdata%\EmergencyPager\Toast.config.json"), false, true);
appBuilder.Logging.AddUnfuckedConsole();

appBuilder.Services
    .Configure<Configuration>(appBuilder.Configuration)
    .AddSingleton<HttpClient>(new UnfuckedHttpClient())
    .AddSingleton<ToastHandler, ToastHandlerImpl>()
    .AddSingleton<PagerDutyRestClientFactory, PagerDutyRestClientFactoryImpl>();

bool isToastCallback = ToastNotificationManagerCompat.WasCurrentProcessToastActivated();
if (!isToastCallback) {
    appBuilder.Services
        .AddSingleton(provider => new HubConnectionBuilder()
            .WithUrl(provider.GetRequiredService<IOptions<Configuration>>().Value.hubAddress)
            .WithAutomaticReconnect(new DelayRetry(Delays.Exponential(TimeSpan.FromSeconds(1), max: TimeSpan.FromMinutes(2))))
            .ConfigureLogging(hubBuilder => {
                hubBuilder.AddConfiguration(appBuilder.Configuration.GetSection("Logging"));
                hubBuilder.AddUnfuckedConsole();
                // Use the same ConsoleFormatter instance as the outer context so the stateful automatic column width is the same, instead of using two instances where the columns would jump around depending on the source
                hubBuilder.Services.Remove(hubBuilder.Services.First(service => service.ImplementationType == typeof(UnfuckedConsoleFormatter)));
                hubBuilder.Services.AddSingleton<ConsoleFormatter>(_ => provider.GetServices<ConsoleFormatter>().OfType<UnfuckedConsoleFormatter>().First());
            })
            .Build())
        .AddSingleton<IHubClient, HubClient>();
}

using IHost app = appBuilder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

ToastHandler toastHandler = app.Services.GetRequiredService<ToastHandler>();
ToastNotificationManagerCompat.OnActivated += async e => {
    await toastHandler.onToastInteraction(e);
    if (isToastCallback) {
        await cts.CancelAsync();
    }
};

if (!isToastCallback) {
    PagerDutyRestClientFactory pagerDutyClientFactory = app.Services.GetRequiredService<PagerDutyRestClientFactory>();
    IOptions<Configuration>    config                 = app.Services.GetRequiredService<IOptions<Configuration>>();

    foreach (PagerDutyAccount account in config.Value.pagerDutyAccountsBySubdomain.Values) {
        PagerDutyUser pagerDutyUser = await pagerDutyClientFactory.createPagerDutyClient(account)
            .Path("users/{id}")
            .ResolveTemplate("id", account.userId)
            .Get<PagerDutyUser>(cts.Token);

        account.userEmailAddress = pagerDutyUser.email;
    }

    HubConnection hubConnection = app.Services.GetRequiredService<HubConnection>();
    IHubClient    hubClient     = app.Services.GetRequiredService<IHubClient>();

    hubClient.incidentUpdated += toastHandler.onIncidentUpdated;
    hubConnection.Closed += e => {
        if (!cts.IsCancellationRequested) {
            logger.Warn("Connection to eventing socket closed: {msg}", e?.Message);
        }
        return Task.CompletedTask;
    };
    logger.Debug("Connecting to eventing socket");
    await hubConnection.StartAsync(cts.Token);
    logger.Info("Waiting for socket events about incident updates");
}

using RuntimeUpgradeNotifier runtimeUpgrades = new() {
    RestartStrategy = RestartStrategy.AutoRestartProcess,
    ExitStrategy    = new HostedLifetimeExit(app),
    LoggerFactory   = app.Services.GetRequiredService<ILoggerFactory>()
};

await app.RunAsync(cts.Token);
logger.Debug("Shutting down");

ToastNotificationManagerCompat.Uninstall();