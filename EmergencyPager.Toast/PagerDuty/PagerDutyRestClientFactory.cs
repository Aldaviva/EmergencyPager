using EmergencyPager.Toast.Data;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unfucked.HTTP;
using Unfucked.HTTP.Config;
using HttpHeaders = Unfucked.HTTP.HttpHeaders;

namespace EmergencyPager.Toast.PagerDuty;

public interface PagerDutyRestClientFactory {

    IWebTarget createPagerDutyClient(PagerDutyAccount account);

}

/*
 * https://developer.pagerduty.com/docs/rest-api-overview
 * https://developer.pagerduty.com/docs/authentication
 */
public sealed class PagerDutyRestClientFactoryImpl(HttpClient http): PagerDutyRestClientFactory {

    private static readonly Uri PAGERDUTY_API_BASE = new("https://api.pagerduty.com");

    // https://developer.pagerduty.com/docs/versioning#header-based-versioning
    private static readonly MediaTypeHeaderValue ACCEPTED_API_VERSION = new("application/vnd.pagerduty+json") { Parameters = { new NameValueHeaderValue("version", 2.ToString()) } };

    private static readonly JsonSerializerOptions JSON_OPTIONS = new(JsonSerializerDefaults.Web) {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters           = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public IWebTarget createPagerDutyClient(PagerDutyAccount account) => http.Target(PAGERDUTY_API_BASE)
        .Property(PropertyKey.JsonSerializerOptions, JSON_OPTIONS)
        .Authorization($"Token token={account.apiAccessKey}") // https://developer.pagerduty.com/docs/authentication
        .Accept(ACCEPTED_API_VERSION)
        .Header(HttpHeaders.From, account.userEmailAddress); // https://developer.pagerduty.com/docs/rest-api-overview#http-request-headers

}