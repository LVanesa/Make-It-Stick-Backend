using Make_It_Stick_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Make_It_Stick_Backend.Data
{
    public class BooksDataSeederInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            // Obține contextul bazei de date din container
            using (var context = serviceProvider.GetRequiredService<AppDbContext>())
            {
                // Dacă există deja cărți în BD, seeding-ul nu mai e necesar
                if (await context.Books.AnyAsync())
                {
                    Console.WriteLine("Books already seeded.");
                    return;
                }

                // Obține serviciul GoogleBooksService din container
                var googleBooksService = serviceProvider.GetRequiredService<GoogleBooksService>();

                // Creează o instanță a seeder-ului (definit în BooksDataSeeder)
                var seeder = new BooksDataSeeder(context, googleBooksService);

                // Apelează metoda asincronă de seeding
                await seeder.SeedBooksAsync();
            }
        }
    }
}
