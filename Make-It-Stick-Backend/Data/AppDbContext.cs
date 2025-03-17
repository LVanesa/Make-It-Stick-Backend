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


        public DbSet<AppUser> Users { get; set; }
    }
}
