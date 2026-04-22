namespace EmergencyPager.Toast.Data;

public sealed class Configuration {

    public required Uri hubAddress { get; init; }
    public required IReadOnlyDictionary<string, PagerDutyAccount> pagerDutyAccountsBySubdomain { get; init; }

}

public record PagerDutyAccount(string apiAccessKey, string userId, string? userEmailAddress) {

    public string? userEmailAddress { get; set; } = userEmailAddress;

}

/// <seealso href="https://developer.pagerduty.com/api-reference/2395ca1feb25e-get-a-user"/>
internal record PagerDutyUser(string id, string email);