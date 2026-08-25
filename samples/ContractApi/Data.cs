using JsonApiToolkit.Attributes;
using Microsoft.EntityFrameworkCore;

namespace ContractApi;

[JsonApiResource("authors")]
public class Author
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
}

[JsonApiResource("articles")]
public class Article
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public bool Published { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public List<string> Tags { get; set; } = [];
    public int AuthorId { get; set; }
    public Author? Author { get; set; }
    public List<Comment> Comments { get; set; } = [];
}

[JsonApiResource("comments")]
public class Comment
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ArticleId { get; set; }
    public Article? Article { get; set; }
    public int AuthorId { get; set; }
    public Author? Author { get; set; }
}

public class ContractDbContext(DbContextOptions<ContractDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();
}

/// <summary>
/// Deterministic fabricated seed data. The contract test suite in
/// clients/typescript/contract/ asserts against these exact rules;
/// change them and the suite must change with them.
/// </summary>
public static class Seed
{
    public static void Run(ContractDbContext db)
    {
        if (db.Authors.Any())
            return;

        db.Authors.AddRange(
            new Author
            {
                Id = 1,
                Name = "Astrid Berg",
                Email = "astrid@example.com",
            },
            new Author { Id = 2, Name = "Bjarne Moen" },
            new Author
            {
                Id = 3,
                Name = "Carmen Diaz",
                Email = "carmen@example.com",
            }
        );

        int commentId = 1;
        for (int i = 1; i <= 25; i++)
        {
            db.Articles.Add(
                new Article
                {
                    Id = i,
                    Title = $"Article {i:00}",
                    Body = i % 5 == 0 ? null : $"Body of article {i}",
                    Published = i % 2 == 1,
                    PublishedAt =
                        i % 2 == 1 ? new DateTime(2025, 1, i, 12, 0, 0, DateTimeKind.Utc) : null,
                    ViewCount = i * 10,
                    Tags = (i % 3) switch
                    {
                        0 => ["tech", "news"],
                        1 => ["tech"],
                        _ => [],
                    },
                    AuthorId = (i - 1) % 3 + 1,
                }
            );

            // Two comments each on the first ten articles
            if (i <= 10)
            {
                for (int j = 1; j <= 2; j++)
                {
                    db.Comments.Add(
                        new Comment
                        {
                            Id = commentId++,
                            Text = $"Comment {j} on article {i}",
                            CreatedAt = new DateTime(2025, 2, i, 8 + j, 0, 0, DateTimeKind.Utc),
                            ArticleId = i,
                            AuthorId = (i + j) % 3 + 1,
                        }
                    );
                }
            }
        }

        db.SaveChanges();
    }
}
