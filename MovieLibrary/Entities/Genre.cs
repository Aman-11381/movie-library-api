namespace MovieLibrary.Entities
{
    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<MovieGenre> MovieGenres { get; set; }
    }
}
