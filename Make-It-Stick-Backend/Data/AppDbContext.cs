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
        public DbSet<ReadingSession> ReadingSessions { get; set; }
        public DbSet<DailyReadingGoal> DailyReadingGoals { get; set; }
        public DbSet<DailyProgress> DailyProgresses { get; set; }
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

            // Relația între AppUser și YearlyReadingGoal
            modelBuilder.Entity<YearlyReadingGoal>()
                .HasOne(y => y.User)
                .WithMany(u => u.YearlyReadingGoals)
                .HasForeignKey(y => y.UserId);

            // Relația între LibraryItem și ReadingSession
            modelBuilder.Entity<ReadingSession>()
                .HasOne(rs => rs.LibraryItem)
                .WithMany(li => li.ReadingSessions)
                .HasForeignKey(rs => rs.LibraryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relația între AppUser și DailyReadingGoal
            modelBuilder.Entity<AppUser>()
                 .HasOne(u => u.DailyReadingGoal)
                 .WithOne(g => g.User)
                 .HasForeignKey<DailyReadingGoal>(g => g.UserId)
                 .OnDelete(DeleteBehavior.Cascade);


            // Relația între AppUser și DailyProgress
            modelBuilder.Entity<DailyProgress>()
                .HasOne(dp => dp.User)
                .WithMany(u => u.DailyProgresses)
                .HasForeignKey(dp => dp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Constrângere unică pe (UserId, Date) în DailyProgress
            // (un utilizator poate avea un singur DailyProgress pentru fiecare zi)
            modelBuilder.Entity<DailyProgress>()
                .HasIndex(dp => new { dp.UserId, dp.Date })
                .IsUnique();
        }

    }
}
