using Pager.Duty.Webhooks.Requests;

namespace EmergencyPager.Common.Events;

public interface EventsFromHub {

    Task incidentUpdated(IncidentWebhookPayload incident);

}