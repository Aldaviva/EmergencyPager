using Pager.Duty.Webhooks.Requests;

namespace EmergencyPager.Common.Events;

public interface EventsFromHub {

    /// <summary>A PagerDuty incident was changed.</summary>
    /// <param name="incident">The new incident state</param>
    Task incidentUpdated(IncidentWebhookPayload incident);

}