using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Models;
using Make_It_Stick_Backend.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Make_It_Stick_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : BaseController
    {
        public NotesController(AppDbContext context) : base(context) { }

        [HttpPost("simple")]
        public async Task<IActionResult> CreateSimpleNote([FromBody] SimpleNoteDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(item =>
                    item.Book.GoogleBookId == dto.GoogleBookId &&
                    item.UserId == user.UserId);

            if (libraryItem == null)
                return BadRequest("Invalid book: no LibraryItem found for the given GoogleBookId and user.");

            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.TagId) && t.UserId == user.UserId)
                .ToListAsync();

            var note = new SimpleNote
            {
                UserId = user.UserId,
                LibraryItemId = libraryItem.LibraryItemId,
                Title = dto.Title,
                Content = dto.Content,
                IsFavorite = dto.IsFavorite,
                BaseNoteTags = tags.Select(tag => new BaseNoteTag { TagId = tag.TagId }).ToList()
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Note created successfully", noteId = note.NoteId });
        }

        [HttpPost("quote")]
        public async Task<IActionResult> CreateQuoteNote([FromBody] QuoteNoteDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(item =>
                    item.Book.GoogleBookId == dto.GoogleBookId &&
                    item.UserId == user.UserId);

            if (libraryItem == null)
                return BadRequest("Invalid book: no LibraryItem found for the given GoogleBookId and user.");

            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.TagId) && t.UserId == user.UserId)
                .ToListAsync();

            var note = new QuoteNote
            {
                UserId = user.UserId,
                LibraryItemId = libraryItem.LibraryItemId,
                Quote = dto.Quote,
                comment = dto.Comment,
                IsFavorite = dto.IsFavorite,
                BaseNoteTags = tags.Select(tag => new BaseNoteTag { TagId = tag.TagId }).ToList()
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quote note created successfully", noteId = note.NoteId });
        }

        [HttpPost("definition")]
        public async Task<IActionResult> CreateDefinitionNote([FromBody] DefinitionNoteDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(item =>
                    item.Book.GoogleBookId == dto.GoogleBookId &&
                    item.UserId == user.UserId);

            if (libraryItem == null)
                return BadRequest("Invalid book: no LibraryItem found for the given GoogleBookId and user.");

            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.TagId) && t.UserId == user.UserId)
                .ToListAsync();

            var note = new DefinitionNote
            {
                UserId = user.UserId,
                LibraryItemId = libraryItem.LibraryItemId,
                Word = dto.Word,
                Definition = dto.Definition,
                Example = dto.Example,
                IsFavorite = dto.IsFavorite,
                BaseNoteTags = tags.Select(tag => new BaseNoteTag { TagId = tag.TagId }).ToList()
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Definition note created successfully", noteId = note.NoteId });
        }

        [HttpPost("flashcard")]
        public async Task<IActionResult> CreateFlashcardNote([FromBody] FlashcardNoteDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(item =>
                    item.Book.GoogleBookId == dto.GoogleBookId &&
                    item.UserId == user.UserId);

            if (libraryItem == null)
                return BadRequest("Invalid book: no LibraryItem found for the given GoogleBookId and user.");

            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.TagId) && t.UserId == user.UserId)
                .ToListAsync();

            var note = new FlashcardNote
            {
                UserId = user.UserId,
                LibraryItemId = libraryItem.LibraryItemId,
                FlashcardFront = dto.FlashcardFront,
                FlashcardBack = dto.FlashcardBack,
                IsFavorite = dto.IsFavorite,
                BaseNoteTags = tags.Select(tag => new BaseNoteTag { TagId = tag.TagId }).ToList()
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Flashcard note created successfully", noteId = note.NoteId });
        }

        [HttpGet("books-with-notes")]
        public async Task<IActionResult> GetBooksWithNotes()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            var booksWithNotes = await _context.Notes
                .Where(n => n.UserId == user.UserId)
                .Include(n => n.LibraryItem)
                    .ThenInclude(li => li.Book)
                .GroupBy(n => new {
                    n.LibraryItem.Book.GoogleBookId,
                    n.LibraryItem.Book.Title,
                    n.LibraryItem.Book.CoverUrl
                })
                .Select(g => new {
                    googleBookId = g.Key.GoogleBookId,
                    title = g.Key.Title,
                    cover = g.Key.CoverUrl,
                    noteCount = g.Count()
                })
                .ToListAsync();

            return Ok(booksWithNotes);
        }


        [HttpGet("counts")]
        public async Task<IActionResult> GetNotesCounts()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var allNotes = await _context.Notes
                .Include(n => n.LibraryItem)
                .Where(n => n.UserId == user.UserId)
                .ToListAsync();

            var counts = allNotes
                .GroupBy(n => n.GetType().Name.ToLower())
                .ToDictionary(g => g.Key, g => g.Count());

            var favoriteCount = allNotes.Count(n => n.IsFavorite);

            var result = new List<object>
            {
                new { type = "simple", count = counts.GetValueOrDefault("simplenote", 0) },
                new { type = "flashcard", count = counts.GetValueOrDefault("flashcardnote", 0) },
                new { type = "quote", count = counts.GetValueOrDefault("quotenote", 0) },
                new { type = "definition", count = counts.GetValueOrDefault("definitionnote", 0) },
                new { type = "favorites", count = favoriteCount }
            };

            return Ok(result);
        }


        [HttpGet("filtered")]
        public async Task<IActionResult> GetFilteredNotes([FromQuery] string? type, [FromQuery] string? bookId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var notesQuery = _context.Notes
                .Include(n => n.LibraryItem)
                    .ThenInclude(li => li.Book)
                .Include(n => n.BaseNoteTags)
                    .ThenInclude(t => t.Tag)
                .Where(n => n.UserId == user.UserId);

            if (!string.IsNullOrEmpty(bookId))
                notesQuery = notesQuery.Where(n => n.LibraryItem.Book.GoogleBookId == bookId);

            if (type == "favorites")
                notesQuery = notesQuery.Where(n => n.IsFavorite);

            var notes = await notesQuery.ToListAsync();

            var filteredNotes = notes
                .Where(n =>
                    type == "all" || type == null || type == "favorites" ||
                    (type == "simple" && n is SimpleNote) ||
                    (type == "quote" && n is QuoteNote) ||
                    (type == "definition" && n is DefinitionNote) ||
                    (type == "flashcard" && n is FlashcardNote)
                )
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    id = n.NoteId,
                    type = n switch
                    {
                        SimpleNote => "simple",
                        QuoteNote => "quote",
                        DefinitionNote => "definition",
                        FlashcardNote => "flashcard",
                        _ => "unknown"
                    },
                    isFavorite = n.IsFavorite,
                    createdAt = n.CreatedAt,
                    bookTitle = n.LibraryItem.Book.Title,
                    googleBookId = n.LibraryItem.Book.GoogleBookId,
                    cover = n.LibraryItem.Book.CoverUrl,
                    tags = n.BaseNoteTags.Select(t => t.Tag.Name).ToList(),

                    // content-specific
                    title = n switch
                    {
                        SimpleNote s => s.Title,
                        QuoteNote q => q.Quote,
                        DefinitionNote d => d.Word,
                        FlashcardNote f => f.FlashcardFront,
                        _ => ""
                    },
                    content = n switch
                    {
                        SimpleNote s => s.Content,
                        QuoteNote q => q.comment,
                        DefinitionNote d => d.Definition,
                        FlashcardNote f => f.FlashcardBack,
                        _ => ""
                    }
                })
                .ToList();

            return Ok(filteredNotes);
        }

        // NotesController.cs

        [HttpGet("{noteId}/details")]
        public async Task<IActionResult> GetNoteDetails(int noteId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated.");

            var note = await _context.Notes
                .Include(n => n.BaseNoteTags)
                    .ThenInclude(nt => nt.Tag)
                .Include(n => n.LibraryItem)
                    .ThenInclude(li => li.Book)
                .FirstOrDefaultAsync(n => n.NoteId == noteId && n.UserId == user.UserId);

            if (note == null)
                return NotFound("Note not found");

            var noteType = note.GetType().Name.Replace("Note", "").ToLower();

            var baseDto = new
            {
                id = note.NoteId,
                type = noteType,
                isFavorite = note.IsFavorite,
                createdAt = note.CreatedAt.ToString("yyyy-MM-dd"),
                bookTitle = note.LibraryItem.Book.Title,
                cover = note.LibraryItem.Book.CoverUrl,
                googleBookId = note.LibraryItem.Book.GoogleBookId,
                tags = note.BaseNoteTags
                    .Select(t => new {
                        id = t.Tag.TagId,
                        name = t.Tag.Name
                    }).ToList(),
            };

            object fullDto = note switch
            {
                SimpleNote sn => new
                {
                    baseDto.id,
                    baseDto.type,
                    baseDto.isFavorite,
                    baseDto.createdAt,
                    baseDto.bookTitle,
                    baseDto.cover,
                    baseDto.googleBookId,
                    baseDto.tags,
                    title = sn.Title,
                    content = sn.Content
                },
                QuoteNote qn => new
                {
                    baseDto.id,
                    baseDto.type,
                    baseDto.isFavorite,
                    baseDto.createdAt,
                    baseDto.bookTitle,
                    baseDto.cover,
                    baseDto.googleBookId,
                    baseDto.tags,
                    quote = qn.Quote,
                    comment = qn.comment
                },
                DefinitionNote dn => new
                {
                    baseDto.id,
                    baseDto.type,
                    baseDto.isFavorite,
                    baseDto.createdAt,
                    baseDto.bookTitle,
                    baseDto.cover,
                    baseDto.googleBookId,
                    baseDto.tags,
                    word = dn.Word,
                    definition = dn.Definition,
                    example = dn.Example
                },
                FlashcardNote fn => new
                {
                    baseDto.id,
                    baseDto.type,
                    baseDto.isFavorite,
                    baseDto.createdAt,
                    baseDto.bookTitle,
                    baseDto.cover,
                    baseDto.googleBookId,
                    baseDto.tags,
                    front = fn.FlashcardFront,
                    back = fn.FlashcardBack
                },
                _ => baseDto
            };

            return Ok(fullDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            var note = await _context.Notes
                .Include(n => n.BaseNoteTags)
                .FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == user.UserId);

            if (note == null)
                return NotFound();

            _context.RemoveRange(note.BaseNoteTags);
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/toggle-favorite")]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated.");

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == user.UserId);
            if (note == null)
                return NotFound("Note not found");

            note.IsFavorite = !note.IsFavorite;
            await _context.SaveChangesAsync();

            return Ok(new { isFavorite = note.IsFavorite });
        }

        [HttpPut("simple/{id}")]
        public async Task<IActionResult> UpdateSimpleNote(int id, [FromBody] SimpleNoteDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var note = await _context.SimpleNotes
                .Include(n => n.BaseNoteTags)
                .FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == user.UserId);

            if (note == null)
                return NotFound("Note not found.");

            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(item =>
                    item.Book.GoogleBookId == dto.GoogleBookId &&
                    item.UserId == user.UserId);

            if (libraryItem == null)
                return BadRequest("Invalid book: no LibraryItem found for the given GoogleBookId and user.");

            // Update fields
            note.Title = dto.Title;
            note.Content = dto.Content;
            note.IsFavorite = dto.IsFavorite;
            note.UpdatedAt = DateTime.UtcNow;
            note.LibraryItemId = libraryItem.LibraryItemId;

            // Update tags
            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.TagId) && t.UserId == user.UserId)
                .ToListAsync();

            note.BaseNoteTags.Clear();
            note.BaseNoteTags = tags.Select(tag => new BaseNoteTag { TagId = tag.TagId }).ToList();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Note updated successfully", noteId = note.NoteId });
        }

        [HttpPut("quote/{id}")]
        public async Task<IActionResult> UpdateQuoteNote(int id, [FromBody] QuoteNoteDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var note = await _context.QuoteNotes
                .Include(n => n.BaseNoteTags)
                .FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == user.UserId);

            if (note == null)
                return NotFound("Note not found.");

            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(item =>
                    item.Book.GoogleBookId == dto.GoogleBookId &&
                    item.UserId == user.UserId);

            if (libraryItem == null)
                return BadRequest("Invalid book: no LibraryItem found for the given GoogleBookId and user.");

            note.Quote = dto.Quote;
            note.comment = dto.Comment;
            note.IsFavorite = dto.IsFavorite;
            note.UpdatedAt = DateTime.UtcNow;
            note.LibraryItemId = libraryItem.LibraryItemId;

            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.TagId) && t.UserId == user.UserId)
                .ToListAsync();

            note.BaseNoteTags.Clear();
            note.BaseNoteTags = tags.Select(tag => new BaseNoteTag { TagId = tag.TagId }).ToList();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Note updated successfully", noteId = note.NoteId });
        }


        [HttpPut("definition/{id}")]
        public async Task<IActionResult> UpdateDefinitionNote(int id, [FromBody] DefinitionNoteDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var note = await _context.DefinitionNotes
                .Include(n => n.BaseNoteTags)
                .FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == user.UserId);

            if (note == null)
                return NotFound("Note not found.");

            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(item =>
                    item.Book.GoogleBookId == dto.GoogleBookId &&
                    item.UserId == user.UserId);

            if (libraryItem == null)
                return BadRequest("Invalid book: no LibraryItem found for the given GoogleBookId and user.");

            note.Word = dto.Word;
            note.Definition = dto.Definition;
            note.Example = dto.Example;
            note.IsFavorite = dto.IsFavorite;
            note.UpdatedAt = DateTime.UtcNow;
            note.LibraryItemId = libraryItem.LibraryItemId;

            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.TagId) && t.UserId == user.UserId)
                .ToListAsync();

            note.BaseNoteTags.Clear();
            note.BaseNoteTags = tags.Select(tag => new BaseNoteTag { TagId = tag.TagId }).ToList();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Note updated successfully", noteId = note.NoteId });
        }

        [HttpPut("flashcard/{id}")]
        public async Task<IActionResult> UpdateFlashcardNote(int id, [FromBody] FlashcardNoteDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var note = await _context.FlashcardNotes
                .Include(n => n.BaseNoteTags)
                .FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == user.UserId);

            if (note == null)
                return NotFound("Note not found.");

            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(item =>
                    item.Book.GoogleBookId == dto.GoogleBookId &&
                    item.UserId == user.UserId);

            if (libraryItem == null)
                return BadRequest("Invalid book: no LibraryItem found for the given GoogleBookId and user.");

            note.FlashcardFront = dto.FlashcardFront;
            note.FlashcardBack = dto.FlashcardBack;
            note.IsFavorite = dto.IsFavorite;
            note.UpdatedAt = DateTime.UtcNow;
            note.LibraryItemId = libraryItem.LibraryItemId;

            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.TagId) && t.UserId == user.UserId)
                .ToListAsync();

            note.BaseNoteTags.Clear();
            note.BaseNoteTags = tags.Select(tag => new BaseNoteTag { TagId = tag.TagId }).ToList();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Note updated successfully", noteId = note.NoteId });
        }


        [HttpPost("DiscussNotesWithAI")]
        public async Task<IActionResult> DiscussNotesWithAI([FromBody] DiscussRequestDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            // 1. Fetch all notes for the user
            var notes = await _context.Notes
                .Include(n => n.LibraryItem)
                    .ThenInclude(li => li.Book)
                .Include(n => n.BaseNoteTags)
                    .ThenInclude(t => t.Tag)
                .Where(n => n.UserId == user.UserId)
                .ToListAsync();

            // 2. Transform into expected format
            var noteDtos = notes.Select(n =>
            {
                string content = "";

                switch (n)
                {
                    case SimpleNote simple:
                        content = $"{simple.Title}: {simple.Content}";
                        break;
                    case QuoteNote quote:
                        content = $"{quote.Quote}{(string.IsNullOrWhiteSpace(quote.comment) ? "" : $" — {quote.comment}")}";
                        break;
                    case FlashcardNote flash:
                        content = $"{flash.FlashcardFront} → {flash.FlashcardBack}";
                        break;
                    case DefinitionNote def:
                        content = $"{def.Word}: {def.Definition}{(string.IsNullOrWhiteSpace(def.Example) ? "" : $". Example: {def.Example}")}";
                        break;
                    default:
                        content = "Unknown note content.";
                        break;
                }

                return new
                {
                    note_id = n.NoteId.ToString(),
                    book_title = n.LibraryItem.Book.Title,
                    author = n.LibraryItem.Book.Authors ?? "Unknown",
                    content = content,
                };
            }).ToList();


            // 3. Prepare payload
            var payload = new
            {
                user_id = user.UserId.ToString(),
                question = dto.Question,
                notes = noteDtos
            };

            // 4. Send to FastAPI server
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync("http://localhost:8000/discuss_notes", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errText = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"AI server error: {errText}");
            }

            var result = await response.Content.ReadFromJsonAsync<object>(); // or define proper DTO
            return Ok(result);
        }

    }

}
