using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Models;
using System.Security.Claims;
using Google;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly AppDbContext _context;

    protected BaseController(AppDbContext context)
    {
        _context = context;
    }

    protected async Task<AppUser?> GetCurrentUserAsync()
    {
        string? firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

        Console.WriteLine("🔑 Firebase UID: " + firebaseUid);

        if (string.IsNullOrEmpty(firebaseUid))
        {
            Console.WriteLine("⚠️ Firebase UID not found in token");
            return null;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
        Console.WriteLine("👤 Found user: " + user?.Email);

        return user;
    }

}
