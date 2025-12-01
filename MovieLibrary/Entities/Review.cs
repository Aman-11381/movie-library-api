namespace MovieLibrary.Entities
{
    public class Review
    {
        public int Id { get; set; }

        public int MovieId { get; set; }
        public Movie Movie { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int Rating { get; set; }  // 1 to 5
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
