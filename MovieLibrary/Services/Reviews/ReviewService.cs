using Microsoft.EntityFrameworkCore;
using MovieLibrary.DTOs.Reviews;
using MovieLibrary.DTOs.Reviews.Response;
using MovieLibrary.Entities;

namespace MovieLibrary.Services.Reviews
{
    public class ReviewService : IReviewService
    {
        private readonly MovieLibraryDbContext _db;

        public ReviewService(MovieLibraryDbContext db)
        {
            _db = db;
        }

        public async Task<ReviewResponse> AddOrUpdateReviewAsync(int movieId, string userId, ReviewCreateOrUpdateRequest request)
        {
            // Check movie exists
            var movie = await _db.Movies.FindAsync(movieId);
            if (movie == null)
                return null;

            // Check if user already reviewed
            var existingReview = await _db.Reviews
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

                return MapToResponse(newReview);
            }
            else
            {
                // Update existing review
                existingReview.Rating = request.Rating;
                existingReview.Comment = request.Comment;
                existingReview.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return MapToResponse(existingReview);
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, string userId)
        {
            var review = await _db.Reviews.FindAsync(reviewId);

            if (review == null || review.UserId != userId)
                return false;

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<List<ReviewResponse>> GetReviewsForMovieAsync(int movieId)
        {
            return await _db.Reviews
                .Where(r => r.MovieId == movieId)
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
                .ToListAsync();
        }

        private ReviewResponse MapToResponse(Review review)
        {
            return new ReviewResponse
            {
                Id = review.Id,
                MovieId = review.MovieId,
                Rating = review.Rating,
                Comment = review.Comment,
                UserId = review.UserId,
                UserEmail = review.User?.Email,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };
        }
    }
}
