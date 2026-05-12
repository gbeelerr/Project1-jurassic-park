namespace Jurassic.movieserviceapi.Repositories;

public interface IMovieAttractionMapRepository
{
    Task<Guid?> GetAttractionIdAsync(Guid movieId, CancellationToken cancellationToken = default);
    Task UpsertAsync(Guid movieId, Guid attractionId, CancellationToken cancellationToken = default);
}
