namespace MovieLibrary.DTOs.Movies.Responses
{
    public class MovieListResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int DurationMinutes { get; set; }
        public int LanguageId { get; set; }
        public int CountryId { get; set; }
        public List<int> GenreIds { get; set; }
    }

}
