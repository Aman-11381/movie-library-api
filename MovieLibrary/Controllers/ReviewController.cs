using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovieLibrary.DTOs.Reviews;
using MovieLibrary.Entities;
using MovieLibrary.Services.Reviews;

namespace MovieLibrary.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/movies/{movieId}/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(IReviewService reviewService, UserManager<ApplicationUser> userManager, ILogger<ReviewController> logger)
        {
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateReview(int movieId, ReviewCreateOrUpdateRequest request)
        {
            if (movieId <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            if (request == null)
                return BadRequest(new { message = "Request cannot be null." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID is null or empty");
                return Unauthorized(new { message = "User not authenticated." });
            }

            try
            {
                var result = await _reviewService.AddOrUpdateReviewAsync(movieId, userId, request);

                if (result == null)
                    return NotFound(new { message = "Movie not found." });

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating/updating review for movie {MovieId}", movieId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating review for movie {MovieId} by user {UserId}", movieId, userId);
                return StatusCode(500, new { message = "An error occurred while processing the review." });
            }
        }

        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int movieId, int reviewId)
        {
            if (movieId <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            if (reviewId <= 0)
                return BadRequest(new { message = "Invalid review ID." });

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID is null or empty");
                return Unauthorized(new { message = "User not authenticated." });
            }

            try
            {
                var deleted = await _reviewService.DeleteReviewAsync(reviewId, userId);

                if (!deleted)
                    return NotFound(new { message = "Review not found." });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId} by user {UserId}", reviewId, userId);
                return StatusCode(500, new { message = "An error occurred while deleting the review." });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllReviews(int movieId)
        {
            if (movieId <= 0)
                return BadRequest(new { message = "Invalid movie ID." });

            try
            {
                var reviews = await _reviewService.GetReviewsForMovieAsync(movieId);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reviews for movie {MovieId}", movieId);
                return StatusCode(500, new { message = "An error occurred while retrieving reviews." });
            }
        }
    }

}
