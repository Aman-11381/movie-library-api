namespace MovieLibrary.Entities
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int DurationMinutes { get; set; }

        // Foreign keys
        public int LanguageId { get; set; }
        public int CountryId { get; set; }

        // Navigation properties
        public Language Language { get; set; }
        public Country Country { get; set; }
        public List<MovieGenre> MovieGenres { get; set; }
        public List<MovieActor> MovieActors { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
