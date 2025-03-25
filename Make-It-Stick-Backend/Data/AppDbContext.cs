using Make_It_Stick_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Make_It_Stick_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }


        public DbSet<Book> Books { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<LibraryItem> LibraryItems { get; set; }
        public DbSet<YearlyReadingGoal> YearlyReadingGoals { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurări suplimentare, dacă este necesar
            base.OnModelCreating(modelBuilder);

            // Relația între AppUser și LibraryItem (un utilizator are multe LibraryItem-uri)
            modelBuilder.Entity<LibraryItem>()
                .HasOne(li => li.User)
                .WithMany(u => u.LibraryItems)
                .HasForeignKey(li => li.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relația între Book și LibraryItem (o carte poate fi în multe LibraryItem-uri)
            modelBuilder.Entity<LibraryItem>()
                .HasOne(li => li.Book)
                .WithMany()
                .HasForeignKey(li => li.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<YearlyReadingGoal>()
            .HasOne(y => y.User)
            .WithMany(u => u.YearlyReadingGoals)
            .HasForeignKey(y => y.UserId);

        }

    }
}
