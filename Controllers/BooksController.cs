using Microsoft.AspNetCore.Mvc;
using LearningDotNet.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LearningDotNet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private static List<Book> Books = new List<Book>();
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        private readonly IHttpClientFactory _httpClientFactory;

        public BooksController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private async Task InitializeBooksAsync()
        {
            if (_isInitialized)
                return;

            lock (_lock)
            {
                if (_isInitialized)
                    return;
            }

            var client = _httpClientFactory.CreateClient();

            // Open Library search API for "programming" books
            var externalApiUrl = "https://openlibrary.org/search.json?q=programming";

            try
            {
                var response = await client.GetAsync(externalApiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var searchResult = JsonSerializer.Deserialize<OpenLibrarySearchResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (searchResult?.Docs != null)
                {
                    var books = searchResult.Docs.Select((doc, index) => new Book
                    {
                        Id = index + 1,
                        Title = doc.Title,
                        Author = doc.Author_name.FirstOrDefault() ?? "Unknown",
                        Year = doc.First_publish_year ?? 0
                    }).ToList();

                    lock (_lock)
                    {
                        Books = books;
                        _isInitialized = true;
                    }
                }
            }
            catch
            {
                lock (_lock)
                {
                    Books = new List<Book>();
                    _isInitialized = true;
                }
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> Get()
        {
            await InitializeBooksAsync();
            return Books;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> Get(int id)
        {
            await InitializeBooksAsync();

            var book = Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
                return NotFound();

            return book;
        }

        [HttpPost]
        public async Task<ActionResult<Book>> Post([FromBody] Book newBook)
        {
            await InitializeBooksAsync();

            if (newBook == null)
                return BadRequest();

            newBook.Id = Books.Any() ? Books.Max(b => b.Id) + 1 : 1;
            Books.Add(newBook);

            return CreatedAtAction(nameof(Get), new { id = newBook.Id }, newBook);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] Book updatedBook)
        {
            await InitializeBooksAsync();

            var book = Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
                return NotFound();

            book.Title = updatedBook.Title;
            book.Author = updatedBook.Author;
            book.Year = updatedBook.Year;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await InitializeBooksAsync();

            var book = Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
                return NotFound();

            Books.Remove(book);
            return NoContent();
        }
    }

   public class OpenLibrarySearchResult
{
    public List<Doc> Docs { get; set; } = new();

    public class Doc
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Author_name { get; set; } = new();
        public int? First_publish_year { get; set; }
    }
}
}