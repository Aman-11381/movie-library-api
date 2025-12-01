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

        public ReviewController(IReviewService reviewService, UserManager<ApplicationUser> userManager)
        {
            _reviewService = reviewService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateReview(int movieId, ReviewCreateOrUpdateRequest request)
        {
            var userId = _userManager.GetUserId(User);
            var result = await _reviewService.AddOrUpdateReviewAsync(movieId, userId, request);

            if (result == null)
                return NotFound("Movie not found");

            return Ok(result);
        }

        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int movieId, int reviewId)
        {
            var userId = _userManager.GetUserId(User);
            var deleted = await _reviewService.DeleteReviewAsync(reviewId, userId);

            if (!deleted)
                return Unauthorized("You cannot delete this review");

            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllReviews(int movieId)
        {
            var reviews = await _reviewService.GetReviewsForMovieAsync(movieId);
            return Ok(reviews);
        }
    }

}
