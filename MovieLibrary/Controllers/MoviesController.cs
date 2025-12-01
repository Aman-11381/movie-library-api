using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieLibrary.DTOs.Movies;
using MovieLibrary.Services.Movies;

namespace MovieLibrary.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(IMovieService movieService, ILogger<MoviesController> logger)
        {
            _movieService = movieService ?? throw new ArgumentNullException(nameof(movieService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateMovie(MovieCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var movie = await _movieService.CreateMovieAsync(request);
                return CreatedAtAction(nameof(GetMovieById), new { id = movie.Id }, movie);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating movie");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating movie");
                return StatusCode(500, new { message = "An error occurred while creating the movie." });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMovieById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            try
            {
                var movie = await _movieService.GetMovieByIdAsync(id);

                if (movie == null)
                    return NotFound(new { message = "Movie not found." });

                return Ok(movie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movie with ID: {MovieId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the movie." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMovies(
            [FromQuery] int? languageId,
            [FromQuery] int? genreId,
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var movies = await _movieService.GetMoviesAsync(languageId, genreId, sortOrder);
                return Ok(movies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving movies");
                return StatusCode(500, new { message = "An error occurred while retrieving movies." });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateMovie(int id, MovieCreateRequest request)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            if (request == null)
                return BadRequest(new { message = "Request cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _movieService.UpdateMovieAsync(id, request);

                if (updated == null)
                    return NotFound(new { message = "Movie not found." });

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating movie {MovieId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie {MovieId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the movie." });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            try
            {
                var success = await _movieService.DeleteMovieAsync(id);

                if (!success)
                    return NotFound(new { message = "Movie not found." });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie {MovieId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the movie." });
            }
        }

    }
}
