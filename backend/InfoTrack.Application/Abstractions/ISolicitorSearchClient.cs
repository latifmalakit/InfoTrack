using InfoTrack.Application.LocationSearch;

namespace InfoTrack.Application.Abstractions;

public interface ISolicitorSearchClient
{
    Task<SolicitorSearchOutcome> SearchAsync(string location, CancellationToken cancellationToken);
}
