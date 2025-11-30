using Microsoft.AspNetCore.Mvc;
using MovieLibrary.DTOs.Movies;
using MovieLibrary.Services.Movies;

namespace MovieLibrary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMovie(MovieCreateRequest request)
        {
            var movie = await _movieService.CreateMovieAsync(request);
            return CreatedAtAction(nameof(GetMovieById), new { id = movie.Id }, movie);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMovieById(int id)
        {
            var movie = await _movieService.GetMovieByIdAsync(id);

            if (movie == null)
                return NotFound();

            return Ok(movie);
        }

        [HttpGet]
        public async Task<IActionResult> GetMovies(
            [FromQuery] int? languageId,
            [FromQuery] int? genreId,
            [FromQuery] string? sortOrder = "desc")
        {
            var movies = await _movieService.GetMoviesAsync(languageId, genreId, sortOrder);
            return Ok(movies);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateMovie(int id, MovieCreateRequest request)
        {
            var updated = await _movieService.UpdateMovieAsync(id, request);

            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            var success = await _movieService.DeleteMovieAsync(id);

            if (!success)
                return NotFound();

            return NoContent();
        }

    }
}
