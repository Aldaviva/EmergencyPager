using EmergencyPager.API;
using EmergencyPager.API.Toast;
using EmergencyPager.Data;
using Kasa;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pager.Duty.Webhooks;
using Pager.Duty.Webhooks.Requests;

namespace Tests;

public class PagerDutyResourceTest {

    private static readonly ILogger<PagerDutyResource> LOGGER = NullLogger<PagerDutyResource>.Instance;

    private readonly ToastDispatcher toasts = A.Fake<ToastDispatcher>();

    [Fact]
    public async Task turnOutletOn() {
        using SemaphoreSlim        done           = new(0, 1);
        var                        kasa           = A.Fake<IKasaOutlet>();
        KasaSingleOutletController kasaController = new(kasa);
        PagerDutyResource          resource       = new(A.Fake<IWebhookResource>(), _ => [kasaController], toasts, LOGGER);
        var                        kasaSystem     = A.Fake<IKasaOutletBase.ISystemCommands.ISingleSocket>();

        A.CallTo(() => kasa.System).Returns(kasaSystem);
        A.CallTo(() => kasaSystem.SetSocketOn(A<bool>._)).Invokes(() => { done.Release(); });

        IncidentWebhookPayload incident = new() {
            Metadata       = new WebhookPayloadMetadata { EventType = "incident.unacknowledged" },
            HtmlUrl        = new Uri("https://mysubdomain.pagerduty.com/incidents/ABC123"),
            Status         = IncidentStatus.Triggered,
            IncidentNumber = 456,
            Title          = "Test 1"
        };
        await resource.onIncidentReceived(incident);

        await done.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);

        A.CallTo(() => kasaSystem.SetSocketOn(true)).MustHaveHappenedOnceExactly();
        A.CallTo(() => toasts.incidentUpdated(incident)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task turnOutletOff() {
        using SemaphoreSlim       done           = new(0, 1);
        var                       kasa           = A.Fake<IMultiSocketKasaOutlet>();
        KasaMultiOutletController kasaController = new(kasa, 0);
        PagerDutyResource         resource       = new(A.Fake<IWebhookResource>(), _ => [kasaController], toasts, LOGGER);
        var                       kasaSystem     = A.Fake<IKasaOutletBase.ISystemCommands.IMultiSocket>();

        A.CallTo(() => kasa.System).Returns(kasaSystem);
        A.CallTo(() => kasaSystem.SetSocketOn(An<int>._, A<bool>._)).Invokes(() => { done.Release(); });

        IncidentWebhookPayload incident = new() {
            Metadata       = new WebhookPayloadMetadata { EventType = "incident.unacknowledged" },
            HtmlUrl        = new Uri("https://mysubdomain.pagerduty.com/incidents/ABC123"),
            Status         = IncidentStatus.Triggered,
            IncidentNumber = 456,
            Title          = "Test 1"
        };
        await resource.onIncidentReceived(incident);

        await done.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);

        A.CallTo(() => kasaSystem.SetSocketOn(0, true)).MustHaveHappenedOnceExactly();
        A.CallTo(() => toasts.incidentUpdated(incident)).MustHaveHappenedOnceExactly();
    }

}