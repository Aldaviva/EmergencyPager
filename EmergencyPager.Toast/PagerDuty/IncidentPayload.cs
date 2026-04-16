using Pager.Duty.Webhooks.Requests;

namespace EmergencyPager.Toast.PagerDuty;

public sealed record IncidentPayload(IncidentUpdate incident);

public sealed record IncidentUpdate(IncidentStatus status) {

    public ReferenceType type => ReferenceType.IncidentReference;

}