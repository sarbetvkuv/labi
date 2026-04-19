using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lab3.CrudApi;

public static class Lab3CrudApiProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        bool hasExplicitEndpoints =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS"));

        if (!hasExplicitEndpoints)
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
        }

        builder.Services.AddDbContext<LibraryDbContext>(options =>
            options.UseSqlite("Data Source=lab3_library.db"));

        var app = builder.Build();

        using (IServiceScope scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
            db.Database.EnsureCreated();
            Seed(db);
        }

        app.MapGet("/", () => Results.Ok(new
        {
            service = "Lab3 CRUD API",
            endpoints = new[]
            {
                "/authors",
                "/books",
                "/categories"
            }
        }));

        MapAuthorEndpoints(app);
        MapBookEndpoints(app);
        MapCategoryEndpoints(app);

        app.Run();
    }

    private static void MapAuthorEndpoints(WebApplication app)
    {
        app.MapGet("/authors", async (LibraryDbContext db) =>
        {
            List<AuthorResponse> authors = await db.Authors
                .Include(a => a.Books)
                .Select(a => new AuthorResponse(
                    a.Id,
                    a.Name,
                    a.Books.Select(b => new BookShortResponse(b.Id, b.Title)).ToList()))
                .ToListAsync();

            return Results.Ok(authors);
        });

        app.MapGet("/authors/{id:int}", async (int id, LibraryDbContext db) =>
        {
            Author? author = await db.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author is null)
            {
                return Results.NotFound($"Author with id={id} not found.");
            }

            var response = new AuthorResponse(
                author.Id,
                author.Name,
                author.Books.Select(b => new BookShortResponse(b.Id, b.Title)).ToList());

            return Results.Ok(response);
        });

        app.MapPost("/authors", async (CreateAuthorRequest request, LibraryDbContext db) =>
        {
            var author = new Author { Name = request.Name.Trim() };
            db.Authors.Add(author);
            await db.SaveChangesAsync();

            return Results.Created($"/authors/{author.Id}", new AuthorResponse(author.Id, author.Name, new List<BookShortResponse>()));
        });

        app.MapPut("/authors/{id:int}", async (int id, UpdateAuthorRequest request, LibraryDbContext db) =>
        {
            Author? author = await db.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author is null)
            {
                return Results.NotFound($"Author with id={id} not found.");
            }

            author.Name = request.Name.Trim();
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/authors/{id:int}", async (int id, LibraryDbContext db) =>
        {
            Author? author = await db.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author is null)
            {
                return Results.NotFound($"Author with id={id} not found.");
            }

            db.Authors.Remove(author);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static void MapBookEndpoints(WebApplication app)
    {
        app.MapGet("/books", async (LibraryDbContext db) =>
        {
            List<Book> books = await db.Books
                .Include(b => b.Author)
                .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
                .ToListAsync();

            var response = books.Select(ToBookResponse).ToList();
            return Results.Ok(response);
        });

        app.MapGet("/books/{id:int}", async (int id, LibraryDbContext db) =>
        {
            Book? book = await db.Books
                .Include(b => b.Author)
                .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book is null)
            {
                return Results.NotFound($"Book with id={id} not found.");
            }

            return Results.Ok(ToBookResponse(book));
        });

        app.MapPost("/books", async (CreateBookRequest request, LibraryDbContext db) =>
        {
            if (!await db.Authors.AnyAsync(a => a.Id == request.AuthorId))
            {
                return Results.BadRequest($"Author with id={request.AuthorId} does not exist.");
            }

            List<int> distinctCategoryIds = request.CategoryIds.Distinct().ToList();
            List<Category> categories = await db.Categories
                .Where(c => distinctCategoryIds.Contains(c.Id))
                .ToListAsync();

            if (categories.Count != distinctCategoryIds.Count)
            {
                return Results.BadRequest("One or more category ids do not exist.");
            }

            var book = new Book
            {
                Title = request.Title.Trim(),
                AuthorId = request.AuthorId,
                BookCategories = categories
                    .Select(c => new BookCategory { CategoryId = c.Id })
                    .ToList()
            };

            db.Books.Add(book);
            await db.SaveChangesAsync();

            return Results.Created($"/books/{book.Id}", new { book.Id, book.Title, book.AuthorId, CategoryIds = distinctCategoryIds });
        });

        app.MapPut("/books/{id:int}", async (int id, UpdateBookRequest request, LibraryDbContext db) =>
        {
            Book? book = await db.Books
                .Include(b => b.BookCategories)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book is null)
            {
                return Results.NotFound($"Book with id={id} not found.");
            }

            if (!await db.Authors.AnyAsync(a => a.Id == request.AuthorId))
            {
                return Results.BadRequest($"Author with id={request.AuthorId} does not exist.");
            }

            List<int> distinctCategoryIds = request.CategoryIds.Distinct().ToList();
            List<Category> categories = await db.Categories
                .Where(c => distinctCategoryIds.Contains(c.Id))
                .ToListAsync();

            if (categories.Count != distinctCategoryIds.Count)
            {
                return Results.BadRequest("One or more category ids do not exist.");
            }

            book.Title = request.Title.Trim();
            book.AuthorId = request.AuthorId;

            db.BookCategories.RemoveRange(book.BookCategories);
            book.BookCategories = categories
                .Select(c => new BookCategory { BookId = id, CategoryId = c.Id })
                .ToList();

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/books/{id:int}", async (int id, LibraryDbContext db) =>
        {
            Book? book = await db.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book is null)
            {
                return Results.NotFound($"Book with id={id} not found.");
            }

            db.Books.Remove(book);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static void MapCategoryEndpoints(WebApplication app)
    {
        app.MapGet("/categories", async (LibraryDbContext db) =>
        {
            List<Category> categories = await db.Categories
                .Include(c => c.BookCategories)
                .ThenInclude(bc => bc.Book)
                .ToListAsync();

            var response = categories.Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.BookCategories.Select(bc => new BookShortResponse(bc.BookId, bc.Book!.Title)).ToList()))
                .ToList();

            return Results.Ok(response);
        });

        app.MapGet("/categories/{id:int}", async (int id, LibraryDbContext db) =>
        {
            Category? category = await db.Categories
                .Include(c => c.BookCategories)
                .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category is null)
            {
                return Results.NotFound($"Category with id={id} not found.");
            }

            var response = new CategoryResponse(
                category.Id,
                category.Name,
                category.BookCategories.Select(bc => new BookShortResponse(bc.BookId, bc.Book!.Title)).ToList());

            return Results.Ok(response);
        });

        app.MapPost("/categories", async (CreateCategoryRequest request, LibraryDbContext db) =>
        {
            var category = new Category { Name = request.Name.Trim() };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            return Results.Created($"/categories/{category.Id}", new CategoryResponse(category.Id, category.Name, new List<BookShortResponse>()));
        });

        app.MapPut("/categories/{id:int}", async (int id, UpdateCategoryRequest request, LibraryDbContext db) =>
        {
            Category? category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is null)
            {
                return Results.NotFound($"Category with id={id} not found.");
            }

            category.Name = request.Name.Trim();
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/categories/{id:int}", async (int id, LibraryDbContext db) =>
        {
            Category? category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is null)
            {
                return Results.NotFound($"Category with id={id} not found.");
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static BookResponse ToBookResponse(Book book)
    {
        return new BookResponse(
            book.Id,
            book.Title,
            book.AuthorId,
            book.Author?.Name ?? string.Empty,
            book.BookCategories.Select(bc => new CategoryShortResponse(bc.CategoryId, bc.Category?.Name ?? string.Empty)).ToList());
    }

    private static void Seed(LibraryDbContext db)
    {
        if (db.Authors.Any() || db.Books.Any() || db.Categories.Any())
        {
            return;
        }

        var author1 = new Author { Name = "George Orwell" };
        var author2 = new Author { Name = "Aldous Huxley" };

        var category1 = new Category { Name = "Dystopia" };
        var category2 = new Category { Name = "Classic" };
        var category3 = new Category { Name = "Political" };

        db.Authors.AddRange(author1, author2);
        db.Categories.AddRange(category1, category2, category3);
        db.SaveChanges();

        var book1 = new Book { Title = "1984", AuthorId = author1.Id };
        var book2 = new Book { Title = "Animal Farm", AuthorId = author1.Id };
        var book3 = new Book { Title = "Brave New World", AuthorId = author2.Id };

        db.Books.AddRange(book1, book2, book3);
        db.SaveChanges();

        db.BookCategories.AddRange(
            new BookCategory { BookId = book1.Id, CategoryId = category1.Id },
            new BookCategory { BookId = book1.Id, CategoryId = category2.Id },
            new BookCategory { BookId = book1.Id, CategoryId = category3.Id },
            new BookCategory { BookId = book2.Id, CategoryId = category2.Id },
            new BookCategory { BookId = book2.Id, CategoryId = category3.Id },
            new BookCategory { BookId = book3.Id, CategoryId = category1.Id },
            new BookCategory { BookId = book3.Id, CategoryId = category2.Id }
        );

        db.SaveChanges();
    }
}

public sealed class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>()
            .HasMany(a => a.Books)
            .WithOne(b => b.Author)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookCategory>()
            .HasKey(bc => new { bc.BookId, bc.CategoryId });

        modelBuilder.Entity<BookCategory>()
            .HasOne(bc => bc.Book)
            .WithMany(b => b.BookCategories)
            .HasForeignKey(bc => bc.BookId);

        modelBuilder.Entity<BookCategory>()
            .HasOne(bc => bc.Category)
            .WithMany(c => c.BookCategories)
            .HasForeignKey(bc => bc.CategoryId);
    }
}

public sealed class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Book> Books { get; set; } = new();
}

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public int AuthorId { get; set; }
    public Author? Author { get; set; }

    public List<BookCategory> BookCategories { get; set; } = new();
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<BookCategory> BookCategories { get; set; } = new();
}

public sealed class BookCategory
{
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}

public sealed record CreateAuthorRequest(string Name);
public sealed record UpdateAuthorRequest(string Name);

public sealed record CreateCategoryRequest(string Name);
public sealed record UpdateCategoryRequest(string Name);

public sealed record CreateBookRequest(string Title, int AuthorId, List<int> CategoryIds);
public sealed record UpdateBookRequest(string Title, int AuthorId, List<int> CategoryIds);

public sealed record BookShortResponse(int Id, string Title);
public sealed record CategoryShortResponse(int Id, string Name);

public sealed record AuthorResponse(int Id, string Name, List<BookShortResponse> Books);
public sealed record CategoryResponse(int Id, string Name, List<BookShortResponse> Books);
public sealed record BookResponse(int Id, string Title, int AuthorId, string AuthorName, List<CategoryShortResponse> Categories);
