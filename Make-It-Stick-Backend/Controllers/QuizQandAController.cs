using FirebaseAdmin.Auth;
using Google.Api;
using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Models;
using Make_It_Stick_Backend.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static OpenCvSharp.ML.DTrees;

namespace Make_It_Stick_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class QuizQandAController : BaseController
    {
        private readonly HttpClient _httpClient;

        public QuizQandAController(AppDbContext context) : base(context)
        {
            _httpClient = new HttpClient();
        }

      

        // Funcția de curățare a textului
        private static string CleanTextForProcessing(string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;

                // 1. Normalizează spațiile
                // Înlocuiește orice secvență de whitespace cu un singur spațiu
                string cleaned = Regex.Replace(input, @"\s+", " ");

                // 2. Elimină spațiile de la începutul și sfârșitul textului
                cleaned = cleaned.Trim();

                // 3. Elimină caracterele de control (excepție: \n, \r, \t)
                cleaned = Regex.Replace(cleaned, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

                // 4. Elimină caracterele care pot cauza probleme în JSON
                // Păstrează doar caracterele printabile și whitespace basic
                cleaned = Regex.Replace(cleaned, @"[^\u0020-\u007E\u00A0-\uFFFF\n\r\t]", "");

                // 5. Înlocuiește ghilimelele problematice cu variante standard
                cleaned = cleaned.Replace("'", "'").Replace("'", "'");

                // 6. Elimină secvențele de newline excesive
                cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");

                // 7. Truncate dacă textul este prea lung (opțional)
                const int maxLength = 10000; // Ajustează după necesități
                if (cleaned.Length > maxLength)
                {
                    cleaned = cleaned.Substring(0, maxLength) + "...";
                }
                return cleaned;
            }

        [HttpPost("generateQuestion")]
        public async Task<IActionResult> GenerateQuestion([FromBody] GenerateQuestionRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            int userId = user.UserId;
            var baseNote = await _context.Notes
                .Include(n => n.BaseNoteTags)
                .ThenInclude(t => t.Tag)
                .FirstOrDefaultAsync(n => n.NoteId == request.NoteId && n.UserId == userId);

            if (baseNote == null)
                return NotFound("Note not found");

            string rawTextContent = baseNote switch
            {
                SimpleNote simple => $"{simple.Title}\n {simple.Content}",
                QuoteNote quote => $"Quote: {quote.Quote}\n Comment: {quote.comment}",
                FlashcardNote flashcard => $"{flashcard.FlashcardFront}\n {flashcard.FlashcardBack}",
                DefinitionNote def => $"Word/Expression: {def.Word} - Definition: {def.Definition}\n Example: {def.Example}",
                _ => throw new InvalidOperationException("Unsupported note type")
            };

            // Curăță textul înainte de trimitere
            string cleanedTextContent = CleanTextForProcessing(rawTextContent);

            // Verifică dacă textul curățat nu este gol
            if (string.IsNullOrWhiteSpace(cleanedTextContent))
            {
                return BadRequest("Text content is empty after cleaning");
            }

            var payload = new
            {
                text = cleanedTextContent
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }),
                Encoding.UTF8,"application/json");

            try
            {
                var response = await _httpClient.PostAsync("http://localhost:8000/generate_question", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, errorText);
                }

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calling AI service: {ex.Message}");
            }
        }

    [HttpPost("create")]
        public async Task<IActionResult> CreateQuizQandA([FromBody] CreateQuizQuestionRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated.");

            // Verifică că notița aparține userului
            var baseNote = await _context.Notes
                .FirstOrDefaultAsync(n => n.NoteId == request.NoteId && n.UserId == user.UserId);

            if (baseNote == null)
                return NotFound("Note not found or does not belong to the current user.");

            var now = DateTime.UtcNow;

            var quiz = new QuizQandA
            {
                NoteId = request.NoteId,
                Question = request.Question,
                Answer = request.Answer,
                Difficulty = 3.0, // valoare inițială FSRS
                Stability = 0.1,  // va fi actualizată după prima revizuire
                Interval = 0,
                LastReviewDate = now,
                NextReviewDate = now, // prima revizuire poate începe azi
                LastRating = null,
                IsNew = true
            };

            _context.QuizQuestions.Add(quiz);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Question created successfully", id = quiz.Id });
        }

        [HttpGet("getAllQuestions")]
        public async Task<IActionResult> GetAllQuestions()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated.");

            var questions = await _context.QuizQuestions
                .Include(q => q.Note)
                .Where(q => q.Note.UserId == user.UserId)
                .Select(q => new
                {
                    q.Id,
                    q.NoteId,
                    q.Question,
                    q.Answer,
                    q.Difficulty,
                    q.Stability,
                    q.Interval,
                    q.LastReviewDate,
                    q.NextReviewDate,
                    q.LastRating,
                    q.IsNew,
                    NoteSummary = new
                    {
                        q.Note.LibraryItemId,
                        q.Note.LibraryItem.Book.Title,
                        q.Note.LibraryItem.Book.Authors,
                        q.Note.LibraryItem.Book.CoverUrl,
                    }
                })
                .ToListAsync();

            return Ok(questions);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateQuizQandA(int id, [FromBody] CreateQuizQuestionRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated.");

            var quiz = await _context.QuizQuestions
                .Include(q => q.Note)
                .FirstOrDefaultAsync(q => q.Id == id && q.Note.UserId == user.UserId);

            if (quiz == null)
                return NotFound("Question not found or not accessible.");

            if (!string.IsNullOrWhiteSpace(request.Question))
                quiz.Question = request.Question;
            if (!string.IsNullOrWhiteSpace(request.Answer))
                quiz.Answer = request.Answer;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Question updated successfully." });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized("User not authenticated.");

            var q = await _context.QuizQuestions
                .Include(x => x.Note)
                .FirstOrDefaultAsync(x => x.Id == id && x.Note.UserId == user.UserId);

            if (q == null) return NotFound("Question not found or not owned by user.");

            _context.QuizQuestions.Remove(q);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted successfully." });
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetQuestionCount()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized("User not authenticated.");

            var count = await _context.QuizQuestions
                .Include(q => q.Note)
                .CountAsync(q => q.Note.UserId == user.UserId);

            return Ok(new { count });
        }

        // Controller: QuizQandAController.cs

        [HttpGet("getDueToday")]
        public async Task<IActionResult> GetDueToday()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated.");

            var today = DateTime.UtcNow.Date;

            var questions = await _context.QuizQuestions
                .Include(q => q.Note)
                .Where(q => q.Note.UserId == user.UserId && q.NextReviewDate.Date <= today)
                .Select(q => new
                {
                    q.Id,
                    q.NoteId,
                    q.Question,
                    q.Answer,
                    q.Difficulty,
                    q.Stability,
                    q.Interval,
                    q.LastReviewDate,
                    q.NextReviewDate,
                    q.LastRating,
                    q.IsNew,
                    NoteSummary = new
                    {
                        q.Note.LibraryItemId,
                        q.Note.LibraryItem.Book.Title,
                        q.Note.LibraryItem.Book.Authors,
                        q.Note.LibraryItem.Book.CoverUrl,
                    }
                })
                .ToListAsync();

            return Ok(questions);
        }

        [HttpGet("getDueTodayCount")]
        public async Task<IActionResult> GetDueTodayCount()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated.");

            var today = DateTime.UtcNow.Date;
            var count = await _context.QuizQuestions
                .Include(q => q.Note)
                .Where(q => q.Note.UserId == user.UserId && q.NextReviewDate.Date <= today)
                .CountAsync();

            return Ok(count);
        }

        private async Task<FSRSReviewResponse?> CallFsrsServiceAsync(FSRSReviewRequest data)
        {
            var client = new HttpClient();
            var url = "http://localhost:8000/fsrs_review";

            var response = await client.PostAsJsonAsync(url, data);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<FSRSReviewResponse>();
        }



        [HttpPost("submitReview")]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var quiz = await _context.QuizQuestions
                .Include(q => q.Note)
                .FirstOrDefaultAsync(q => q.Id == request.QuestionId && q.Note.UserId == user.UserId);

            if (quiz == null) return NotFound("Question not found");

            var now = DateTime.UtcNow;

            var fsrsRequest = new FSRSReviewRequest
            {
                Difficulty = quiz.Difficulty,
                Stability = quiz.Stability,
                LastReview = now, // <-- Azi, adică momentul review-ului curent
                Rating = (int)request.Rating
            };

            var fsrsResult = await CallFsrsServiceAsync(fsrsRequest);
            if (fsrsResult == null) return StatusCode(500, "FSRS service failed");

            // Actualizează valorile în baza de date
            quiz.LastRating = request.Rating;
            quiz.LastReviewDate = now;
            quiz.Interval = fsrsResult.Interval;
            quiz.Stability = fsrsResult.Stability;
            quiz.Difficulty = fsrsResult.Difficulty;
            quiz.NextReviewDate = fsrsResult.NextReview;
            quiz.IsNew = false;

            _context.QuizReviews.Add(new QuizReview
            {
                QuizQandAId = quiz.Id,
                ReviewedAt = now,
                Rating = request.Rating,
                StabilityAfter = fsrsResult.Stability,
                DifficultyAfter = fsrsResult.Difficulty,
                IntervalAfter = fsrsResult.Interval
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Review submitted successfully" });
        }

        // Adaugă acest endpoint în QuizQandAController.cs

        [HttpGet("{questionId}/reviewHistory")]
        public async Task<IActionResult> GetReviewHistoryForQuestion(int questionId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var quiz = await _context.QuizQuestions
                .Include(q => q.Note)
                .FirstOrDefaultAsync(q => q.Id == questionId && q.Note.UserId == user.UserId);

            if (quiz == null) return NotFound("Question not found.");

            var reviews = await _context.QuizReviews
                .Where(r => r.QuizQandAId == questionId)
                .OrderBy(r => r.ReviewedAt)
                .Select(r => new
                {
                    id = r.Id,
                    reviewedAt = r.ReviewedAt,
                    rating = (int)r.Rating, // Convertește enum-ul la int
                    stabilityAfter = r.StabilityAfter,
                    difficultyAfter = r.DifficultyAfter,
                    intervalAfter = r.IntervalAfter
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // OPȚIONAL: Endpoint combinat care returnează și datele de bază ale întrebării
        [HttpGet("{questionId}/fullReviewData")]
        public async Task<IActionResult> GetFullReviewDataForQuestion(int questionId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var quiz = await _context.QuizQuestions
                .Include(q => q.Note)
                .Include(q => q.Reviews)
                .FirstOrDefaultAsync(q => q.Id == questionId && q.Note.UserId == user.UserId);

            if (quiz == null) return NotFound("Question not found.");

            var reviews = quiz.Reviews
                .OrderBy(r => r.ReviewedAt)
                .Select(r => new
                {
                    id = r.Id,
                    reviewedAt = r.ReviewedAt,
                    rating = (int)r.Rating,
                    stabilityAfter = r.StabilityAfter,
                    difficultyAfter = r.DifficultyAfter,
                    intervalAfter = r.IntervalAfter
                })
                .ToList();

            var result = new
            {
                question = new
                {
                    id = quiz.Id,
                    question = quiz.Question,
                    answer = quiz.Answer,
                    currentStability = quiz.Stability,
                    currentDifficulty = quiz.Difficulty,
                    lastReviewDate = quiz.LastReviewDate,
                    nextReviewDate = quiz.NextReviewDate,
                    lastRating = quiz.LastRating != null ? (int)quiz.LastRating : (int?)null,
                    isNew = quiz.IsNew
                },
                reviews = reviews,
                stats = new
                {
                    totalReviews = reviews.Count,
                    successfulReviews = reviews.Count(r => r.rating >= 3),
                    successRate = reviews.Count > 0 ? Math.Round((reviews.Count(r => r.rating >= 3) * 100.0 / reviews.Count), 1) : 0,
                    averageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.rating), 1) : 0,
                    currentStability = quiz.Stability,
                    daysSinceLastReview = quiz.LastReviewDate != DateTime.MinValue ? (DateTime.UtcNow - quiz.LastReviewDate).Days : 0
                }
            };

            return Ok(result);
        }


    }
}
