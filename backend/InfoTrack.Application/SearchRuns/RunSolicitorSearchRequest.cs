namespace InfoTrack.Application.SearchRuns;

public sealed record RunSolicitorSearchRequest(
    IReadOnlyList<string>? Locations,
    bool CompareWithPreviousRun);
