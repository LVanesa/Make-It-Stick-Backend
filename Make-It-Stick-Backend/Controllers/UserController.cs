using Make_It_Stick_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Make_It_Stick_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        public UserController(AppDbContext context) : base(context) { }

        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                user.UserId,
                user.Name,
                user.Email,
                user.Username
            });
        }
    }
}
