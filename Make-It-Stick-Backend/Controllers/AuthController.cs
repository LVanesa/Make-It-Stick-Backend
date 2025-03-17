using FirebaseAdmin.Auth;
using Make_It_Stick_Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Make_It_Stick_Backend.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Make_It_Stick_Backend.Models.Requests;

namespace MakeItStickBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string firebaseApiKey;

        private readonly AppDbContext _context;
        public AuthController(IConfiguration configuration, AppDbContext context)
        {
            // Obținerea cheii API Firebase din fișierul secrets.json
            firebaseApiKey = configuration["Firebase:ApiKey"];
            _context = context;
        }

        [HttpPost("createUser")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Crearea utilizatorului în Firebase
                var userArgs = new UserRecordArgs()
                {
                    Email = request.Email,
                    Password = request.Password,
                    DisplayName = request.Name,
                };

                UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

                // Obținem idToken-ul pentru utilizator prin apelarea endpoint-ului de signInWithPassword
                var signInUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}";
                var loginPayload = new
                {
                    email = request.Email,
                    password = request.Password,
                    returnSecureToken = true
                };

                var jsonLoginPayload = JsonConvert.SerializeObject(loginPayload);
                var loginContent = new StringContent(jsonLoginPayload, Encoding.UTF8, "application/json");

                using (var httpClient = new HttpClient())
                {
                    var loginResponse = await httpClient.PostAsync(signInUrl, loginContent);
                    if (!loginResponse.IsSuccessStatusCode)
                    {
                        var loginErrorResponse = await loginResponse.Content.ReadAsStringAsync();
                        return BadRequest(new { error = "Utilizator creat, dar autentificarea pentru trimiterea emailului de verificare a eșuat: " + loginErrorResponse });
                    }
                    var loginJson = await loginResponse.Content.ReadAsStringAsync();
                    // Deserializăm răspunsul într-un model tipizat
                    var firebaseLoginResponse = JsonConvert.DeserializeObject<FirebaseLoginResponse>(loginJson);
                    string idToken = firebaseLoginResponse?.IdToken;
                    if (string.IsNullOrEmpty(idToken))
                    {
                        return BadRequest(new { error = "Utilizatorul a fost creat, dar nu s-a obținut un token de autentificare." });
                    }

                    // Trimiterea emailului de verificare
                    bool emailSent = await SendVerificationEmail(idToken);

                    var appUser = new AppUser
                    {
                        FirebaseUid = userRecord.Uid,
                        Email = request.Email,
                        Username = request.Username,
                        Name = request.Name
                    };

                    _context.Users.Add(appUser);
                    await _context.SaveChangesAsync();

                    if (!emailSent)
                    {
                        return Ok(new { uid = userRecord.Uid, message = "Utilizator creat, dar trimiterea emailului de verificare a eșuat." });
                    }
                    return Ok(new { uid = userRecord.Uid, message = "Utilizator creat cu succes. Emailul de verificare a fost trimis." });
                }
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Construiesc URL-ul pentru endpoint-ul de login Firebase
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}";

            // Construiește payload-ul conform documentației Firebase
            var payload = new
            {
                email = request.Email,
                password = request.Password,
                returnSecureToken = true
            };

            Console.WriteLine("Payload trimis către Firebase: " + JsonConvert.SerializeObject(payload));

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, httpContent);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Răspuns brut de la Firebase: " + jsonResponse);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Deserializăm într-un model tipizat
                        var firebaseResponse = JsonConvert.DeserializeObject<FirebaseLoginResponse>(jsonResponse);

                        // Verificăm dacă emailul a fost validat
                        var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(firebaseResponse.LocalId);
                        if (!userRecord.EmailVerified)
                        {
                            return BadRequest(new { error = "Emailul nu a fost validat. Vă rugăm să verificați inbox-ul." });
                        }

                        return Ok(firebaseResponse);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Eroare la deserializare: " + ex.Message);
                        return Ok(jsonResponse);
                    }
                }
                else
                {
                    return BadRequest(jsonResponse);
                }
            }
        }

        private async Task<bool> SendVerificationEmail(string idToken)
        {
            var sendOobUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={firebaseApiKey}";
            var payload = new
            {
                requestType = "VERIFY_EMAIL",
                idToken = idToken
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(sendOobUrl, httpContent);
                return response.IsSuccessStatusCode;
            }
        }
        [HttpPost("emailExists")]
        public async Task<IActionResult> EmailExists([FromBody] EmailExistsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.email))
            {
                return BadRequest(new { error = "Email is required." });
            }
            string email = request.email;
            try
            {
                var user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
                return Ok(new { exists = true });
            }
            catch (FirebaseAuthException ex)
            {
                //if (ex.Message.Contains("USER_NOT_FOUND"))
                //{
                //    return Ok(new { exists = false });
                //}
                //return StatusCode(500, new { error = ex.Message });
                return Ok(new { exists = false });

            }
        }

        private async Task<bool> SendPasswordResetEmail(string email)
        {
            var resetUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={firebaseApiKey}";
            var payload = new
            {
                requestType = "PASSWORD_RESET",
                email = email
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(resetUrl, httpContent);
                return response.IsSuccessStatusCode;
            }
        }



        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.email))
            {
                return BadRequest(new { error = "Email is required." });
            }

            try
            {
                bool emailSent = await SendPasswordResetEmail(request.email);
                if (emailSent)
                {
                    return Ok(new { message = "Password reset email sent successfully." });
                }
                else
                {
                    return BadRequest(new { error = "Failed to send reset email." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("usernameExists")]
        public async Task<IActionResult> UsernameExists([FromBody] UsernameExistsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.username))
            {
                return BadRequest(new { error = "Username is required." });
            }

            try
            {
                bool exists = await _context.Users
                    .AnyAsync(u => u.Username.ToLower() == request.username.ToLower());
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }






    }
}
