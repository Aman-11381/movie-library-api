using Microsoft.EntityFrameworkCore;
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

        public MovieService(MovieLibraryDbContext db)
        {
            _db = db;
        }

        public async Task<MovieDetailResponse> CreateMovieAsync(MovieCreateRequest request)
        {
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

            // Return full detail
            return await GetMovieByIdAsync(movie.Id)
                   ?? throw new Exception("Movie not found after creation");
        }

        public async Task<MovieDetailResponse?> GetMovieByIdAsync(int id)
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

            var totalReviews = movie.Reviews.Count;
            double averageRating = totalReviews > 0
                ? movie.Reviews.Average(r => r.Rating)
                : 0;

            var reviewDtos = movie.Reviews
                .Select(r => new ReviewResponse
                {
                    Id = r.Id,
                    MovieId = r.MovieId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    UserId = r.UserId,
                    UserEmail = r.User.Email,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();

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
                Genres = movie.MovieGenres
                    .Select(mg => new GenreDto
                    {
                        Id = mg.Genre.Id,
                        Name = mg.Genre.Name
                    })
                    .ToList(),
                Actors = movie.MovieActors
                    .Select(ma => new ActorWithCharacterDto
                    {
                        Id = ma.Actor.Id,
                        Name = ma.Actor.Name,
                        CharacterName = ma.CharacterName
                    })
                    .ToList(),
                TotalReviews = totalReviews,
                AverageRating = averageRating,
                Reviews = reviewDtos
            };

            return response;
        }

        public async Task<List<MovieListResponse>> GetMoviesAsync(int? languageId, int? genreId, string? sortOrder)
        {
            var query = _db.Movies.AsQueryable();

            // Filter by language
            if (languageId.HasValue)
            {
                query = query.Where(m => m.LanguageId == languageId.Value);
            }

            // Filter by genre
            if (genreId.HasValue)
            {
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

        public async Task<MovieDetailResponse?> UpdateMovieAsync(int id, MovieCreateRequest request)
        {
            var movie = await _db.Movies.FindAsync(id);
            if (movie == null)
                return null;

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

            return await GetMovieByIdAsync(id);
        }
        public async Task<bool> DeleteMovieAsync(int id)
        {
            var movie = await _db.Movies.FindAsync(id);
            if (movie == null)
                return false;

            _db.Movies.Remove(movie);
            await _db.SaveChangesAsync();
            return true;
        }

    }
}
