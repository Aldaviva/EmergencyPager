using EmergencyPager.Toast.Data;
using EmergencyPager.Toast.Eventing;
using EmergencyPager.Toast.PagerDuty;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
using Pager.Duty.Webhooks.Requests;
using System.Reflection;
using System.Text;
using Unfucked.HTTP.Exceptions;
using Unfucked.HTTP.Serialization;

namespace EmergencyPager.Toast;

public interface ToastHandler {

    Task onIncidentUpdated(IHubClient sender, IncidentWebhookPayload incident);

    Task onToastInteraction(ToastArguments e);

}

/*
 * https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/send-local-toast?tabs=desktop
 */
public sealed class ToastHandlerImpl(PagerDutyRestClientFactory pagerDutyClientFactory, IOptions<Configuration> config, ILogger<ToastHandlerImpl> logger): ToastHandler {

    private const string TOAST_ARG_INCIDENT_ID       = "incidentId";
    private const string TOAST_ARG_ACCOUNT_SUBDOMAIN = "accountSubdomain";

    public async Task onIncidentUpdated(IHubClient sender, IncidentWebhookPayload incident) {
        logger.Info("Incident {id} \"{title}\" was {eventType}", incident.Id, incident.Title, incident.EventType);
        string  tag   = incident.Id;
        string? group = incident.Service.Summary;
        switch (incident.EventType) {
            case IncidentEventType.Triggered:
            case IncidentEventType.Unacknowledged:
            case IncidentEventType.Reopened:
            case IncidentEventType.Reassigned:
            case IncidentEventType.Escalated:
                clearOldToastsForIncident();
                PagerDutyAccount? pagerDutyAccount = getPagerDutyAccount(incident);

                if (incident.Assignees.Count == 0 || incident.Assignees.Any(assignee => assignee.Id == pagerDutyAccount?.userId)) {
                    new ToastContentBuilder()
                        .SetToastDuration(ToastDuration.Long)
                        .SetToastScenario(ToastScenario.Alarm)
                        .SetProtocolActivation(incident.HtmlUrl)
                        .AddArgument(TOAST_ARG_INCIDENT_ID, incident.Id)
                        .AddArgument(TOAST_ARG_ACCOUNT_SUBDOMAIN, incident.AccountSubdomain)
                        .AddAppLogoOverride(await saveLogo(), alternateText: "PagerDuty")
                        .AddText(incident.Service.Summary)
                        .AddText(incident.Title)
                        .AddAttributionText($"#{incident.IncidentNumber} {incident.EventType.ToPhrase()}")
                        .AddButton(new ToastButton()
                            .SetContent("Acknowledge")
                            .AddArgument("action", ButtonAction.ACKNOWLEDGE)
                            .SetBackgroundActivation())
                        .AddButton(new ToastButton()
                            .SetContent("Resolve")
                            .AddArgument("action", ButtonAction.RESOLVE)
                            .SetBackgroundActivation())
                        .Show(toast => {
                            toast.Tag   = tag;
                            toast.Group = group;
                        });
                    logger.Debug("Showed toast for untriaged incident");
                }
                break;
            case IncidentEventType.Acknowledged:
            case IncidentEventType.Resolved:
                clearOldToastsForIncident();
                logger.Debug("Removed toast for triaged incident");
                break;
            default:
                break;
        }

        void clearOldToastsForIncident() => ToastNotificationManagerCompat.History.Remove(tag, group);
    }

    /*
     * https://developer.pagerduty.com/api-reference/8a0e1aa2ec666-update-an-incident
     */
    public async Task onToastInteraction(ToastArguments args) {
        string         incidentId       = args.Get(TOAST_ARG_INCIDENT_ID);
        string         accountSubdomain = args.Get(TOAST_ARG_ACCOUNT_SUBDOMAIN);
        ButtonAction   action           = args.GetEnum<ButtonAction>("action");
        IncidentUpdate incidentUpdate = new(action switch {
            ButtonAction.ACKNOWLEDGE => IncidentStatus.Acknowledged,
            ButtonAction.RESOLVE     => IncidentStatus.Resolved
        });

        if (getPagerDutyAccount(accountSubdomain) is {} pagerDutyAccount && pagerDutyClientFactory.createPagerDutyClient(pagerDutyAccount) is {} client) {
            logger.Info("Setting incident {id} to {newStatus}", incidentId, incidentUpdate.status);
            try {
                string response = await client.Path("incidents/{id}")
                    .ResolveTemplate("id", incidentId)
                    .Put<string>(Entity.Json(new IncidentPayload(incidentUpdate)));
                logger.Debug("PagerDuty API responded with {body}", response);
            } catch (WebApplicationException exception) {
                logger.Error("PagerDuty API responded to {url} with {status} {body}", exception.RequestUrl, exception.StatusCode,
                    exception.ResponseBody is {} body ? Encoding.UTF8.GetString(body.Span) : "");
            } catch (ProcessingException exception) {
                logger.Error(exception, "Processing exception while communicating with PagerDuty API");
            }
        } else {
            logger.Error("No configuration for PagerDuty account {subdomain}", accountSubdomain);
        }
    }

    private PagerDutyAccount? getPagerDutyAccount(IncidentWebhookPayload incident) => incident.AccountSubdomain is {} subdomain ? getPagerDutyAccount(subdomain) : null;

    private PagerDutyAccount? getPagerDutyAccount(string accountSubdomain) {
        PagerDutyAccount? account = config.Value.pagerDutyAccountsBySubdomain.GetValueOrDefault(accountSubdomain);
        if (account == null) {
            logger.Warn("No configured integration key for PagerDuty subdomain {subdomain}, ignoring update to incident", accountSubdomain);
        }
        return account;
    }

    private static async Task<Uri> saveLogo() {
        string logoPath = Path.Combine(Path.GetTempPath(), "PagerDuty.png");
        try {
            await using FileStream logoWriteStream = new(logoPath, FileMode.CreateNew, FileAccess.Write); // only write if file is missing
            await using Stream     logoReadStream  = Assembly.GetExecutingAssembly().GetManifestResourceStream("EmergencyPager.Toast.pagerduty.png")!;
            await logoReadStream.CopyToAsync(logoWriteStream);
        } catch (Exception) {
            /* leave the file nonexistent */
        }
        return new Uri(new Uri("file://"), logoPath);
    }

    private enum ButtonAction {

        ACKNOWLEDGE = 0, RESOLVE = 1

    }

}