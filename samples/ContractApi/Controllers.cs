using JsonApiToolkit.Attributes;
using JsonApiToolkit.Controllers;
using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractApi.Controllers;

public record CreateArticleDto(
    string? Title,
    string? Body,
    bool? Published,
    DateTime? PublishedAt,
    int? AuthorId,
    List<string>? Tags
);

public record UpdateArticleDto(string? Title, string? Body, bool? Published, int? ViewCount);

[ApiController]
[Route("articles")]
public class ArticlesController(ContractDbContext db) : JsonApiController
{
    private const string ResourceType = "articles";

    // No [AllowedIncludes] here: with the attribute present, or/not filter
    // groups are rejected as forbidden filter paths (403). The list action
    // stays open so the contract tests can exercise group filters.
    [HttpGet]
    public Task<IActionResult> GetAllAsync() => JsonApiQueryAsync(db.Articles, ResourceType);

    [HttpGet("{id:int}")]
    [AllowedIncludes("author", "comments", "comments.author")]
    public Task<IActionResult> GetByIdAsync(int id) =>
        JsonApiOkAsync(db.Articles.Where(a => a.Id == id), ResourceType);

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateArticleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw JsonApiErrors.RequiredFieldMissing("title");

        var article = new Article
        {
            Id = (await db.Articles.MaxAsync(a => (int?)a.Id) ?? 0) + 1,
            Title = dto.Title,
            Body = dto.Body,
            Published = dto.Published ?? false,
            PublishedAt = dto.PublishedAt,
            Tags = dto.Tags ?? [],
            AuthorId = dto.AuthorId ?? 1,
        };
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        return JsonApiCreated(article, ResourceType, article.Id.ToString());
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, UpdateArticleDto dto)
    {
        var article =
            await db.Articles.FindAsync(id) ?? throw JsonApiErrors.NotFound(ResourceType, id);

        if (dto.Title != null)
            article.Title = dto.Title;
        if (dto.Body != null)
            article.Body = dto.Body;
        if (dto.Published.HasValue)
            article.Published = dto.Published.Value;
        if (dto.ViewCount.HasValue)
            article.ViewCount = dto.ViewCount.Value;

        await db.SaveChangesAsync();
        return JsonApiOk(article, ResourceType);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var article =
            await db.Articles.FindAsync(id) ?? throw JsonApiErrors.NotFound(ResourceType, id);

        db.Articles.Remove(article);
        await db.SaveChangesAsync();
        return JsonApiNoContent();
    }
}

[ApiController]
[Route("authors")]
public class AuthorsController(ContractDbContext db) : JsonApiController
{
    private const string ResourceType = "authors";

    [HttpGet]
    [AllowedIncludes] // no includes allowed: exercises INCLUDE_NOT_ALLOWED
    public Task<IActionResult> GetAllAsync() => JsonApiQueryAsync(db.Authors, ResourceType);

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetByIdAsync(int id) =>
        JsonApiOkAsync(db.Authors.Where(a => a.Id == id), ResourceType);
}
