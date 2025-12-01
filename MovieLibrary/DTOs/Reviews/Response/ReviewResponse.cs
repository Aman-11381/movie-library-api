namespace MovieLibrary.DTOs.Reviews.Response
{
    public class ReviewResponse
    {
        public int Id { get; set; }
        public int MovieId { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public string UserId { get; set; }
        public string UserEmail { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
