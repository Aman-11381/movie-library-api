using MovieLibrary.DTOs.Common;

namespace MovieLibrary.DTOs.Movies.Responses
{
    public class MovieDetailResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int DurationMinutes { get; set; }

        public LanguageDto Language { get; set; }
        public CountryDto Country { get; set; }

        public List<GenreDto> Genres { get; set; }
        public List<ActorWithCharacterDto> Actors { get; set; }
    }

}
