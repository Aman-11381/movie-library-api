using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieLibrary.DTOs.Common;
using MovieLibrary.DTOs.Movies;
using MovieLibrary.DTOs.Movies.Responses;
using MovieLibrary.DTOs.Reviews.Response;
using MovieLibrary.Entities;

namespace MovieLibrary.Services.Movies
{
    public class MovieService : IMovieService
    {
        private readonly MovieLibraryDbContext _db;
        private readonly ILogger<MovieService> _logger;

        public MovieService(MovieLibraryDbContext db, ILogger<MovieService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MovieDetailResponse> CreateMovieAsync(MovieCreateRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title is required.", nameof(request.Title));

            if (string.IsNullOrWhiteSpace(request.Description))
                throw new ArgumentException("Description is required.", nameof(request.Description));

            if (request.DurationMinutes <= 0)
                throw new ArgumentException("Duration must be greater than 0.", nameof(request.DurationMinutes));

            if (request.GenreIds == null || request.GenreIds.Count == 0)
                throw new ArgumentException("At least one genre is required.", nameof(request.GenreIds));

            if (request.Actors == null || request.Actors.Count == 0)
                throw new ArgumentException("At least one actor is required.", nameof(request.Actors));

            try
            {
                // Validate foreign keys
                var languageExists = await _db.Languages.AnyAsync(l => l.Id == request.LanguageId);
                if (!languageExists)
                    throw new ArgumentException($"Language with ID {request.LanguageId} does not exist.", nameof(request.LanguageId));

                var countryExists = await _db.Countries.AnyAsync(c => c.Id == request.CountryId);
                if (!countryExists)
                    throw new ArgumentException($"Country with ID {request.CountryId} does not exist.", nameof(request.CountryId));

                // Validate all genre IDs exist
                foreach (var genreId in request.GenreIds)
                {
                    var genreExists = await _db.Genres.AnyAsync(g => g.Id == genreId);
                    if (!genreExists)
                        throw new ArgumentException($"Genre with ID {genreId} does not exist.", nameof(request.GenreIds));
                }

                // Validate all actor IDs exist
                foreach (var actor in request.Actors)
                {
                    if (string.IsNullOrWhiteSpace(actor.CharacterName))
                        throw new ArgumentException("Character name is required for all actors.", nameof(request.Actors));

                    var actorExists = await _db.Actors.AnyAsync(a => a.Id == actor.ActorId);
                    if (!actorExists)
                        throw new ArgumentException($"Actor with ID {actor.ActorId} does not exist.", nameof(request.Actors));
                }

                // Create base movie
                var movie = new Movie
                {
                    Title = request.Title,
                    Description = request.Description,
                    ReleaseDate = request.ReleaseDate,
                    DurationMinutes = request.DurationMinutes,
                    LanguageId = request.LanguageId,
                    CountryId = request.CountryId
                };

                // Insert movie
                _db.Movies.Add(movie);
                await _db.SaveChangesAsync();

                // Add genres
                foreach (var genreId in request.GenreIds)
                {
                    _db.MovieGenres.Add(new MovieGenre
                    {
                        MovieId = movie.Id,
                        GenreId = genreId
                    });
                }

                // Add actors
                foreach (var actor in request.Actors)
                {
                    _db.MovieActors.Add(new MovieActor
                    {
                        MovieId = movie.Id,
                        ActorId = actor.ActorId,
                        CharacterName = actor.CharacterName
                    });
                }

                // Save join table rows
                await _db.SaveChangesAsync();

                _logger.LogInformation("Movie created successfully with ID: {MovieId}", movie.Id);

                // Return full detail
                return await GetMovieByIdAsync(movie.Id)
                       ?? throw new InvalidOperationException("Movie not found after creation");
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating movie with title: {Title}", request.Title);
                throw;
            }
        }

        public async Task<MovieDetailResponse?> GetMovieByIdAsync(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid movie ID: {MovieId}", id);
                return null;
            }

            try
            {
                var movie = await _db.Movies
                    .Include(m => m.Language)
                    .Include(m => m.Country)
                    .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                    .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
                    .Include(m => m.Reviews).ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (movie == null)
                    return null;

                // Null checks for navigation properties
                if (movie.Language == null)
                {
                    _logger.LogWarning("Movie {MovieId} has null Language", id);
                    throw new InvalidOperationException($"Movie {id} has invalid Language reference");
                }

                if (movie.Country == null)
                {
                    _logger.LogWarning("Movie {MovieId} has null Country", id);
                    throw new InvalidOperationException($"Movie {id} has invalid Country reference");
                }

                var totalReviews = movie.Reviews?.Count ?? 0;
                double averageRating = totalReviews > 0
                    ? movie.Reviews!.Average(r => r.Rating)
                    : 0;

                var reviewDtos = movie.Reviews?
                    .Where(r => r.User != null)
                    .Select(r => new ReviewResponse
                    {
                        Id = r.Id,
                        MovieId = r.MovieId,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        UserId = r.UserId,
                        UserEmail = r.User.Email ?? string.Empty,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToList() ?? new List<ReviewResponse>();

                var response = new MovieDetailResponse
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Description = movie.Description,
                    ReleaseDate = movie.ReleaseDate,
                    DurationMinutes = movie.DurationMinutes,
                    Language = new LanguageDto
                    {
                        Id = movie.Language.Id,
                        Name = movie.Language.Name
                    },
                    Country = new CountryDto
                    {
                        Id = movie.Country.Id,
                        Name = movie.Country.Name
                    },
                    Genres = movie.MovieGenres?
                        .Where(mg => mg.Genre != null)
                        .Select(mg => new GenreDto
                        {
                            Id = mg.Genre.Id,
                            Name = mg.Genre.Name
                        })
                        .ToList() ?? new List<GenreDto>(),
                    Actors = movie.MovieActors?
                        .Where(ma => ma.Actor != null)
                        .Select(ma => new ActorWithCharacterDto
                        {
                            Id = ma.Actor.Id,
                            Name = ma.Actor.Name,
                            CharacterName = ma.CharacterName
                        })
                        .ToList() ?? new List<ActorWithCharacterDto>(),
                    TotalReviews = totalReviews,
                    AverageRating = averageRating,
                    Reviews = reviewDtos
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movie with ID: {MovieId}", id);
                throw;
            }
        }

        public async Task<List<MovieListResponse>> GetMoviesAsync(int? languageId, int? genreId, string? sortOrder)
        {
            try
            {
                var query = _db.Movies.AsQueryable();

                // Filter by language
                if (languageId.HasValue)
                {
                    if (languageId.Value <= 0)
                    {
                        _logger.LogWarning("Invalid languageId filter: {LanguageId}", languageId.Value);
                        return new List<MovieListResponse>();
                    }
                    query = query.Where(m => m.LanguageId == languageId.Value);
                }

                // Filter by genre
                if (genreId.HasValue)
                {
                    if (genreId.Value <= 0)
                    {
                        _logger.LogWarning("Invalid genreId filter: {GenreId}", genreId.Value);
                        return new List<MovieListResponse>();
                    }
                    query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));
                }

                // Sorting
                if (sortOrder == "asc")
                {
                    query = query.OrderBy(m => m.ReleaseDate);
                }
                else
                {
                    query = query.OrderByDescending(m => m.ReleaseDate);
                }

                var movies = await query
                    .Select(m => new MovieListResponse
                    {
                        Id = m.Id,
                        Title = m.Title,
                        ReleaseDate = m.ReleaseDate,
                        DurationMinutes = m.DurationMinutes,
                        LanguageId = m.LanguageId,
                        CountryId = m.CountryId,
                        GenreIds = m.MovieGenres.Select(mg => mg.GenreId).ToList()
                    })
                    .ToListAsync();

                return movies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movies with filters - LanguageId: {LanguageId}, GenreId: {GenreId}", languageId, genreId);
                throw;
            }
        }

        public async Task<MovieDetailResponse?> UpdateMovieAsync(int id, MovieCreateRequest request)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid movie ID for update: {MovieId}", id);
                return null;
            }

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title is required.", nameof(request.Title));

            if (string.IsNullOrWhiteSpace(request.Description))
                throw new ArgumentException("Description is required.", nameof(request.Description));

            if (request.DurationMinutes <= 0)
                throw new ArgumentException("Duration must be greater than 0.", nameof(request.DurationMinutes));

            if (request.GenreIds == null || request.GenreIds.Count == 0)
                throw new ArgumentException("At least one genre is required.", nameof(request.GenreIds));

            if (request.Actors == null || request.Actors.Count == 0)
                throw new ArgumentException("At least one actor is required.", nameof(request.Actors));

            try
            {
                var movie = await _db.Movies.FindAsync(id);
                if (movie == null)
                    return null;

                // Validate foreign keys
                var languageExists = await _db.Languages.AnyAsync(l => l.Id == request.LanguageId);
                if (!languageExists)
                    throw new ArgumentException($"Language with ID {request.LanguageId} does not exist.", nameof(request.LanguageId));

                var countryExists = await _db.Countries.AnyAsync(c => c.Id == request.CountryId);
                if (!countryExists)
                    throw new ArgumentException($"Country with ID {request.CountryId} does not exist.", nameof(request.CountryId));

                // Validate all genre IDs exist
                foreach (var genreId in request.GenreIds)
                {
                    var genreExists = await _db.Genres.AnyAsync(g => g.Id == genreId);
                    if (!genreExists)
                        throw new ArgumentException($"Genre with ID {genreId} does not exist.", nameof(request.GenreIds));
                }

                // Validate all actor IDs exist
                foreach (var actor in request.Actors)
                {
                    if (string.IsNullOrWhiteSpace(actor.CharacterName))
                        throw new ArgumentException("Character name is required for all actors.", nameof(request.Actors));

                    var actorExists = await _db.Actors.AnyAsync(a => a.Id == actor.ActorId);
                    if (!actorExists)
                        throw new ArgumentException($"Actor with ID {actor.ActorId} does not exist.", nameof(request.Actors));
                }

                // Update base properties
                movie.Title = request.Title;
                movie.Description = request.Description;
                movie.ReleaseDate = request.ReleaseDate;
                movie.DurationMinutes = request.DurationMinutes;
                movie.LanguageId = request.LanguageId;
                movie.CountryId = request.CountryId;

                // Update join tables (genres)
                var oldGenres = _db.MovieGenres.Where(mg => mg.MovieId == id);
                _db.MovieGenres.RemoveRange(oldGenres);

                foreach (var genreId in request.GenreIds)
                {
                    _db.MovieGenres.Add(new MovieGenre
                    {
                        MovieId = id,
                        GenreId = genreId
                    });
                }

                // Update join tables (actors)
                var oldActors = _db.MovieActors.Where(ma => ma.MovieId == id);
                _db.MovieActors.RemoveRange(oldActors);

                foreach (var actor in request.Actors)
                {
                    _db.MovieActors.Add(new MovieActor
                    {
                        MovieId = id,
                        ActorId = actor.ActorId,
                        CharacterName = actor.CharacterName
                    });
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Movie updated successfully with ID: {MovieId}", id);

                return await GetMovieByIdAsync(id);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie with ID: {MovieId}", id);
                throw;
            }
        }
        public async Task<bool> DeleteMovieAsync(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid movie ID for deletion: {MovieId}", id);
                return false;
            }

            try
            {
                var movie = await _db.Movies.FindAsync(id);
                if (movie == null)
                    return false;

                _db.Movies.Remove(movie);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Movie deleted successfully with ID: {MovieId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie with ID: {MovieId}", id);
                throw;
            }
        }

    }
}
