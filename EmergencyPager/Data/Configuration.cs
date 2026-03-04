namespace EmergencyPager.Data;

public class Configuration {

    public IReadOnlyList<string>? pagerDutyWebhookSecrets { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>>? alarmLightUrlsByPagerDutySubdomain {
        get;
        init => field = value is null ? null : new Dictionary<string, IReadOnlyList<string>>(value, StringComparer.OrdinalIgnoreCase);
    }

}