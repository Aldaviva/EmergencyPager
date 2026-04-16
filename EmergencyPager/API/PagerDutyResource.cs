using EmergencyPager.API.Toast;
using EmergencyPager.Data;
using Kasa;
using Pager.Duty.Webhooks;
using Pager.Duty.Webhooks.Requests;
using System.Collections.Concurrent;

namespace EmergencyPager.API;

public sealed class PagerDutyResource(
    IWebhookResource webhookResource,
    KasaControllerFactory kasaControllerFactory,
    ToastDispatcher toasts,
    ILogger<PagerDutyResource> logger
): WebResource {

    private readonly ConcurrentDictionary<string, ValueHolder<int>> triggeredIncidentCountByOutletId = Enumerables.CreateConcurrentDictionary<string, int>();

    public void map(IEndpointRouteBuilder webapp) {
        webhookResource.IncidentReceived += async (_, incident) => await onIncidentReceived(incident);
        webhookResource.PingReceived     += (_, _) => logger.Info("Test webhook event received from PagerDuty");
        webapp.MapPost("/pagerduty", webhookResource.HandlePostRequest);
    }

    internal async Task onIncidentReceived(IncidentWebhookPayload incident) {
        if (incident.EventType is IncidentEventType.Triggered or IncidentEventType.Acknowledged or IncidentEventType.Unacknowledged or IncidentEventType.Resolved or IncidentEventType.Reopened
                or IncidentEventType.Reassigned or IncidentEventType.Escalated && incident.AccountSubdomain is {} pagerdutySubdomain) {

            bool isTriggered = incident.Status == IncidentStatus.Triggered;

            foreach (KasaController kasaController in kasaControllerFactory(pagerdutySubdomain)) {
                using (kasaController) {
                    string incidentCountKey = kasaController.id;
                    triggeredIncidentCountByOutletId.TryAdd(incidentCountKey, new ValueHolder<int>(0));
                    int triggeredIncidentCountForOutlet = (isTriggered
                        ? triggeredIncidentCountByOutletId.AtomicIncrement(incidentCountKey)
                        : triggeredIncidentCountByOutletId.AtomicDecrement(incidentCountKey))!.Value;
                    if (triggeredIncidentCountForOutlet < 0) {
                        // If this program was restarted during an outage, the triggered incident count can go from 0 to negative when an incident is resolved, so set it back to 0.
                        triggeredIncidentCountByOutletId.AtomicAdd(incidentCountKey, -triggeredIncidentCountForOutlet);
                    }
                    bool turnOn = triggeredIncidentCountForOutlet != 0;

                    if (isTriggered || !turnOn) {
                        logger.Info("PagerDuty incident #{num:D} \"{title}\" is {status}", incident.IncidentNumber, incident.Title, incident.Status);
                    } else {
                        logger.Info("PagerDuty incident #{num:D} \"{title}\" is {status}, but leaving outlets on because {otherCount} other incidents are still triggered", incident.IncidentNumber,
                            incident.Title, incident.Status, triggeredIncidentCountForOutlet - 1);
                    }

                    try {
                        switch (kasaController) {
                            case KasaSingleOutletController single:
                                await single.outlet.System.SetSocketOn(turnOn);
                                break;
                            case KasaMultiOutletController multi:
                                try {
                                    await multi.outlet.System.SetSocketOn(multi.socket, turnOn);
                                } catch (ArgumentOutOfRangeException e) {
                                    logger.Error(e, "Failed to turn {onOff} socket {socketId} in Kasa outlet {host} because it does not have a 0-indexed socket with that ID", turnOn ? "on" : "off",
                                        multi.socket, multi.outlet.Hostname);
                                }
                                break;
                        }
                    } catch (KasaException e) {
                        logger.Error(e, "Failed to turn {onOff} Kasa outlet {host} in response to a PagerDuty webhook request", turnOn ? "on" : "off", kasaController.outlet.Hostname);
                    }
                }
            }

            await toasts.incidentUpdated(incident);
        }
    }

}