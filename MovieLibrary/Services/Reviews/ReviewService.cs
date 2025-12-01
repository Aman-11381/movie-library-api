using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieLibrary.DTOs.Reviews;
using MovieLibrary.DTOs.Reviews.Response;
using MovieLibrary.Entities;

namespace MovieLibrary.Services.Reviews
{
    public class ReviewService : IReviewService
    {
        private readonly MovieLibraryDbContext _db;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(MovieLibraryDbContext db, ILogger<ReviewService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReviewResponse> AddOrUpdateReviewAsync(int movieId, string userId, ReviewCreateOrUpdateRequest request)
        {
            if (movieId <= 0)
            {
                _logger.LogWarning("Invalid movie ID: {MovieId}", movieId);
                return null;
            }

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate rating is between 1 and 5
            if (request.Rating < 1 || request.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.", nameof(request.Rating));

            try
            {
                // Check movie exists
                var movie = await _db.Movies.FindAsync(movieId);
                if (movie == null)
                    return null;

                // Check if user already reviewed
                var existingReview = await _db.Reviews
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == userId);

                if (existingReview == null)
                {
                    // Create new review
                    var newReview = new Review
                    {
                        MovieId = movieId,
                        UserId = userId,
                        Rating = request.Rating,
                        Comment = request.Comment,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.Reviews.Add(newReview);
                    await _db.SaveChangesAsync();

                    // Reload with User to get email
                    var savedReview = await _db.Reviews
                        .Include(r => r.User)
                        .FirstOrDefaultAsync(r => r.Id == newReview.Id);

                    if (savedReview == null)
                        throw new InvalidOperationException("Review not found after creation");

                    _logger.LogInformation("Review created for movie {MovieId} by user {UserId}", movieId, userId);
                    return MapToResponse(savedReview);
                }
                else
                {
                    // Update existing review
                    existingReview.Rating = request.Rating;
                    existingReview.Comment = request.Comment;
                    existingReview.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                    
                    _logger.LogInformation("Review updated for movie {MovieId} by user {UserId}", movieId, userId);
                    return MapToResponse(existingReview);
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating review for movie {MovieId} by user {UserId}", movieId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, string userId)
        {
            if (reviewId <= 0)
            {
                _logger.LogWarning("Invalid review ID for deletion: {ReviewId}", reviewId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            try
            {
                var review = await _db.Reviews.FindAsync(reviewId);

                if (review == null || review.UserId != userId)
                    return false;

                _db.Reviews.Remove(review);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Review {ReviewId} deleted by user {UserId}", reviewId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId} by user {UserId}", reviewId, userId);
                throw;
            }
        }

        public async Task<List<ReviewResponse>> GetReviewsForMovieAsync(int movieId)
        {
            if (movieId <= 0)
            {
                _logger.LogWarning("Invalid movie ID: {MovieId}", movieId);
                return new List<ReviewResponse>();
            }

            try
            {
                return await _db.Reviews
                    .Include(r => r.User)
                    .Where(r => r.MovieId == movieId)
                    .Select(r => new ReviewResponse
                    {
                        Id = r.Id,
                        MovieId = r.MovieId,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        UserId = r.UserId,
                        UserEmail = r.User != null ? r.User.Email ?? string.Empty : string.Empty,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reviews for movie {MovieId}", movieId);
                throw;
            }
        }

        private ReviewResponse MapToResponse(Review review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));

            if (review.User == null)
            {
                _logger.LogWarning("Review {ReviewId} has null User", review.Id);
                throw new InvalidOperationException($"Review {review.Id} has invalid User reference");
            }

            return new ReviewResponse
            {
                Id = review.Id,
                MovieId = review.MovieId,
                Rating = review.Rating,
                Comment = review.Comment,
                UserId = review.UserId,
                UserEmail = review.User.Email ?? string.Empty,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };
        }
    }
}
