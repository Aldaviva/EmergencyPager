namespace UptimePager.Data;

public record UptimeObserverWebhookPayload {

    public required Monitor monitor { get; init; }
    public required Incident incident { get; init; }

}

public record Monitor {

    public int id { get; init; }
    public required string name { get; init; }
    public required Uri url { get; init; }
    public required Status status { get; init; }

    public enum Status {

        UP,
        DOWN,
        RETRY,
        PAUSED

    }

}

public record Incident {

    public int id { get; init; }
    public required Uri url { get; init; }
    public required Status status { get; init; }
    public required string cause { get; init; }

    public enum Status {

        ACTIVE,
        RESOLVED

    }

}