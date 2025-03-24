using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Make_It_Stick_Backend.Models
{
    public class LibraryItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LibraryItemId { get; set; }

        // Cheie străină către utilizator
        public int UserId { get; set; }

        // Cheie străină către cartea globală (ID-ul din Book)
        public int BookId { get; set; }

        // Tipul listei (currently_reading, wishlist, finished, abandoned)
        public LibraryShelf LibraryShelf { get; set; }

        // Data la care cartea a fost adăugată în bibliotecă
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Data la care cartea a fost terminată (opțional, doar pentru Finished)
        public DateTime? DateFinished { get; set; }

        public DateTime? DateStarted { get; set; }

        // Rating-ul lăsat de utilizator (1-5, opțional)
        public int? UserRating { get; set; }

        // Navigare către User
        public AppUser User { get; set; }

        // Navigare către Book (cartea globală, cu detalii preluate din Google Books API)
        public Book Book { get; set; }
    }
}
