using Pager.Duty.Webhooks.Requests;

namespace EmergencyPager.Toast.PagerDuty;

public record IncidentPayload(IncidentUpdate incident);

public record IncidentUpdate(IncidentStatus status) {

    public ReferenceType type => ReferenceType.IncidentReference;

}