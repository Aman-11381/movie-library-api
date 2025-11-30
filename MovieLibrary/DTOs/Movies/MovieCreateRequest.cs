namespace MovieLibrary.DTOs.Movies
{
    public class MovieCreateRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int DurationMinutes { get; set; }
        public int LanguageId { get; set; }
        public int CountryId { get; set; }
        public List<int> GenreIds { get; set; }
        public List<MovieActorRequest> Actors { get; set; }
    }

}
