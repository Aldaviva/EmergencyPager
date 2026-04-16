using Bom.Squad;
using EmergencyPager.API;
using EmergencyPager.API.Toast;
using EmergencyPager.Data;
using Kasa;
using Microsoft.Extensions.Options;
using Pager.Duty.Webhooks;
using RuntimeUpgrade.Notifier;
using RuntimeUpgrade.Notifier.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Options = Kasa.Options;

BomSquad.DefuseUtf8Bom();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseWindowsService()
    .UseSystemd();

builder.Services
    .Configure<Configuration>(builder.Configuration, options => options.BindNonPublicProperties = true)
    .AddSingleton<KasaControllerFactory>(provider => {
        IReadOnlyDictionary<string, IReadOnlyList<string>>? alarmLightUrlsByPagerDutySubdomain =
            provider.GetRequiredService<IOptions<Configuration>>().Value.alarmLightUrlsByPagerDutySubdomain;
        Options kasaOptions = new() { LoggerFactory = provider.GetService<ILoggerFactory>() };
        return pagerdutySubdomain => alarmLightUrlsByPagerDutySubdomain?.GetValueOrDefault(pagerdutySubdomain)?
            .Select<string, KasaController>(url => new Uri(url, UriKind.Absolute) is var uri && uri.Segments.ElementAtOrDefault(1) is {} s && int.TryParse(s.TrimEnd('/'), out int socketId)
                ? new KasaMultiOutletController(new MultiSocketKasaOutlet(uri.Host, kasaOptions), socketId)
                : new KasaSingleOutletController(new KasaOutlet(uri.Host, kasaOptions))) ?? [];
    })
    .AddSingleton<IWebhookResource>(provider => new WebhookResource(provider.GetRequiredService<IOptions<Configuration>>().Value.pagerDutyWebhookSecrets ?? []))
    .AddHttpClient()
    .AddSingleton<WebResource, PagerDutyResource>()
    .AddSingleton<WebResource, ToastResource>()
    .AddSingleton<ToastDispatcher, ToastDispatcherImpl>()
    .ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)))
    .AddSignalR(options => options.DisableImplicitFromServicesParameters = true);

builder.Logging.AmplifyMessageLevels(options => options.Amplify("Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher", LogLevel.Warning, 2, 3, 5, 11, 13, 14, 15, 19, 21, 22, 23, 24));

await using WebApplication webapp = builder.Build();

foreach (WebResource resource in webapp.Services.GetServices<WebResource>()) {
    resource.map(webapp);
}

using RuntimeUpgradeNotifier runtimeUpgrades = new() {
    LoggerFactory   = webapp.Services.GetRequiredService<ILoggerFactory>(),
    RestartStrategy = RestartStrategy.AutoRestartService,
    ExitStrategy    = new HostedLifetimeExit(webapp)
};

await webapp.RunAsync();