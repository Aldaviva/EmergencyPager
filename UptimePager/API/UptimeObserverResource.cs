using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pager.Duty;
using Pager.Duty.Exceptions;
using Pager.Duty.Requests;
using Pager.Duty.Responses;
using System.Collections.Concurrent;
using ThrottleDebounce.Retry;
using UptimePager.Data;
using Monitor = UptimePager.Data.Monitor;

namespace UptimePager.API;

public class UptimeObserverResource: WebResource {

    private static readonly RetryOptions RETRY_OPTIONS = new() {
        MaxAttempts    = 20,
        Delay          = Delays.Exponential(TimeSpan.FromSeconds(5), max: TimeSpan.FromMinutes(5)),
        IsRetryAllowed = (exception, _) => exception is not (OutOfMemoryException or PagerDutyException { RetryAllowedAfterDelay: false })
    };

    private readonly  ILogger<UptimeObserverResource>   logger;
    private readonly  IOptions<Configuration>           configuration;
    internal readonly ConcurrentDictionary<int, string> dedupKeys;

    public UptimeObserverResource(ILogger<UptimeObserverResource> logger, IOptions<Configuration> configuration) {
        this.logger        = logger;
        this.configuration = configuration;
        int monitorCount = configuration.Value.pagerDutyIntegrationKeysByUptimeObserverMonitorId.Count;
        dedupKeys = new ConcurrentDictionary<int, string>(Math.Min(monitorCount * 2, Environment.ProcessorCount), monitorCount);
    }

    public void map(WebApplication webapp) {
        webapp.MapPost("/uptimeobserver", async Task<IResult> ([FromBody] UptimeObserverWebhookPayload payload, PagerDutyFactory pagerDutyFactory, HttpContext ctx) => {
            Monitor monitor = payload.monitor;
            logger.Trace("Received webhook payload from UptimeObserver: {payload}", payload);

            if (configuration.Value.pagerDutyIntegrationKeysByUptimeObserverMonitorId.TryGetValue(monitor.id, out string? integrationKey)) {
                using IPagerDuty pagerDuty = pagerDutyFactory(integrationKey);

                if (payload.incident.status == Incident.Status.ACTIVE) {
                    return await onMonitorDown(monitor, payload.incident, pagerDuty);
                } else {
                    return await onMonitorUp(monitor, pagerDuty);
                }
            } else {
                logger.Warn("No PagerDuty integration key configured for UptimeObserver monitor {monitor} ({id}), not sending an alert to PagerDuty", monitor.name, monitor.id);
                return Results.NoContent();
            }
        });
        logger.Debug("Listening for UptimeObserver webhooks");
    }

    private async Task<IResult> onMonitorDown(Monitor monitor, Incident incident, IPagerDuty pagerDuty) {
        logger.Info("UptimeObserfver reports that {monitor} is down", monitor.name);
        dedupKeys.TryGetValue(monitor.id, out string? oldDedupKey);

        try {
            TriggerAlert triggerAlert = new(Severity.Error, $"#{incident.id}: {monitor.name} is down") {
                DedupKey = oldDedupKey,
                Links = {
                    new Link(incident.url, "UptimeObserver Incident"),
                    new Link(monitor.url, "UptimeObserver Monitor")
                },
                CustomDetails = new Dictionary<string, object> {
                    ["Cause"]        = incident.cause,
                    ["Monitor Name"] = monitor.name
                },

                // The following fields only appear on the webapp alert details page, and nowhere in the mobile app
                Class     = incident.cause,
                Component = monitor.name,
                Source    = "UptimeObserver"
            };

            string newDedupKey = await Retrier.Attempt(async _ => {
                AlertResponse alertResponse = await pagerDuty.Send(triggerAlert);
                dedupKeys[monitor.id] = alertResponse.DedupKey;
                return alertResponse.DedupKey;
            }, RETRY_OPTIONS);

            logger.Info("Triggered alert in PagerDuty for {monitor} being down, got deduplication key {key}", monitor.name, newDedupKey);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            logger.Error(e, "Failed to trigger alert in PagerDuty after {attempts}, giving up", RETRY_OPTIONS.MaxAttempts);
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Failed to trigger PagerDuty alert");
        }
        return Results.Created();
    }

    private async Task<IResult> onMonitorUp(Monitor monitor, IPagerDuty pagerDuty) {
        logger.Info("UptimeObserver reports that {monitor} is up", monitor.name);
        if (dedupKeys.TryRemove(monitor.id, out string? dedupKey)) {
            ResolveAlert resolution = new(dedupKey);
            await Retrier.Attempt(async _ => await pagerDuty.Send(resolution), RETRY_OPTIONS);
            logger.Info("Resolved PagerDuty alert for {monitor} being down using deduplication key {key}", monitor.name, dedupKey);
        } else {
            logger.Warn("No known PagerDuty alerts for monitor {monitor}, not resolving anything", monitor.name);
        }
        return Results.NoContent();
    }

}