namespace EmergencyPager.Toast.Data;

public sealed class Configuration {

    public required Uri hubAddress { get; init; }
    public required IReadOnlyDictionary<string, PagerDutyAccount> pagerDutyAccountsBySubdomain { get; init; }

    public override string ToString() =>
        $"{nameof(hubAddress)}: {hubAddress}, {nameof(pagerDutyAccountsBySubdomain)}: {pagerDutyAccountsBySubdomain.Select(pair => $"{pair.Key}={pair.Value}").Join(", ")}";

}

public record PagerDutyAccount(string apiAccessKey, string userId, string userEmailAddress) {

    public override string ToString() => $"{nameof(userEmailAddress)}: {userEmailAddress}, {nameof(apiAccessKey)}: {apiAccessKey}, {nameof(userId)}: {userId}";

}
