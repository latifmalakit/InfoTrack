namespace InfoTrack.Application.LocationSearch;

public abstract record SolicitorSearchOutcome(string Location)
{
    public static SolicitorSearchOutcome Succeeded(LocationSearchResult result)
    {
        return new SolicitorSearchSucceeded(result);
    }

    public static SolicitorSearchOutcome UpstreamFailed(
        string location,
        string error,
        SolicitorSearchUpstreamFailureReason reason)
    {
        return new SolicitorSearchUpstreamFailure(location, error, reason);
    }
}

public sealed record SolicitorSearchSucceeded(LocationSearchResult Result)
    : SolicitorSearchOutcome(Result.Location);

public sealed record SolicitorSearchUpstreamFailure(
    string Location,
    string Error,
    SolicitorSearchUpstreamFailureReason Reason)
    : SolicitorSearchOutcome(Location);

public enum SolicitorSearchUpstreamFailureReason
{
    HttpStatus,
    Network,
    Timeout,
    ProviderFormat,
    ResponseTooLarge
}
