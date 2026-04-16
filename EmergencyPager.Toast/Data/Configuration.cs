namespace EmergencyPager.Toast.Data;

public sealed class Configuration {

    public required Uri hubAddress { get; init; }
    public required IReadOnlyDictionary<string, PagerDutyAccount> pagerDutyAccountsBySubdomain { get; init; }

}

public record PagerDutyAccount(string apiAccessKey, string userId, string userEmailAddress);