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

    protected async Task<AppUser> GetCurrentUserAsync()
    {
        string firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(firebaseUid))
            return null;

        return await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
    }
}
