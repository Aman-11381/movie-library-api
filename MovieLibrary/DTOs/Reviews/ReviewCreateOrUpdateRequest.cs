namespace MovieLibrary.DTOs.Reviews
{
    public class ReviewCreateOrUpdateRequest
    {
        public int Rating { get; set; }        // 1 to 5
        public string? Comment { get; set; }   // Optional
    }

}
