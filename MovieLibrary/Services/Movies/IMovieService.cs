using MovieLibrary.DTOs.Movies;
using MovieLibrary.DTOs.Movies.Responses;

namespace MovieLibrary.Services.Movies
{
    public interface IMovieService
    {
        Task<MovieDetailResponse?> GetMovieByIdAsync(int id);
        Task<List<MovieListResponse>> GetMoviesAsync(int? languageId, int? genreId, string? sortOrder);
        Task<MovieDetailResponse> CreateMovieAsync(MovieCreateRequest request);
        Task<MovieDetailResponse?> UpdateMovieAsync(int id, MovieCreateRequest request);
        Task<bool> DeleteMovieAsync(int id);
    }

}
