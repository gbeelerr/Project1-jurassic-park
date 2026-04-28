using Jurassic.movieserviceapi.Models;

namespace Jurassic.movieserviceapi.Repositories;

public interface IMovieRepository
{
    Task<IEnumerable<MoviePosterDto>> GetMoviePostersAsync();
    Task<IEnumerable<NowPlayingMovieDto>> GetNowPlayingAsync(DateTime? date = null);
    Task<ShowtimeDetailsDto?> GetShowtimeDetailsAsync(Guid showtimeId);
}
