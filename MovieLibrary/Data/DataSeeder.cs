using MovieLibrary.Entities;

namespace MovieLibrary.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(MovieLibraryDbContext db)
        {
            // Prevent duplicate seeding
            if (!db.Genres.Any())
            {
                db.Genres.AddRange(
                    new Genre { Name = "Action" },
                    new Genre { Name = "Comedy" },
                    new Genre { Name = "Drama" },
                    new Genre { Name = "Sci-Fi" }
                );
            }

            if (!db.Languages.Any())
            {
                db.Languages.AddRange(
                    new Language { Name = "English" },
                    new Language { Name = "Hindi" },
                    new Language { Name = "Spanish" }
                );
            }

            if (!db.Countries.Any())
            {
                db.Countries.AddRange(
                    new Country { Name = "USA" },
                    new Country { Name = "India" },
                    new Country { Name = "UK" }
                );
            }

            if (!db.Actors.Any())
            {
                db.Actors.AddRange(
                    new Actor { Name = "Leonardo DiCaprio" },
                    new Actor { Name = "Tom Hardy" },
                    new Actor { Name = "Christian Bale" }
                );
            }

            await db.SaveChangesAsync();
        }
    }

}
