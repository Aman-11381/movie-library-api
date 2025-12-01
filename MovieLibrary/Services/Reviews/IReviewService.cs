using MovieLibrary.DTOs.Reviews;
using MovieLibrary.DTOs.Reviews.Response;

namespace MovieLibrary.Services.Reviews
{
    public interface IReviewService
    {
        Task<ReviewResponse> AddOrUpdateReviewAsync(int movieId, string userId, ReviewCreateOrUpdateRequest request);
        Task<bool> DeleteReviewAsync(int reviewId, string userId);
        Task<List<ReviewResponse>> GetReviewsForMovieAsync(int movieId);
    }
}
