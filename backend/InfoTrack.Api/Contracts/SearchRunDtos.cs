namespace InfoTrack.Api.Contracts;

public sealed record SearchRunRequestDto(
    IReadOnlyList<string>? Locations,
    bool CompareWithPreviousRun = true);
