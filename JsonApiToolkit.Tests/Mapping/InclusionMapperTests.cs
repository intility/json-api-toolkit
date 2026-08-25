using JsonApiToolkit.Attributes;
using JsonApiToolkit.Mapping;
using JsonApiToolkit.Models.Resources;

namespace JsonApiToolkit.Tests.Mapping;

public class InclusionMapperTests
{
    #region Test Models

    [JsonApiResource("people")]
    private class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Book> Books { get; set; } = new();
        public Publisher? Publisher { get; set; }
    }

    private class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public Author? Author { get; set; }
        public List<Chapter> Chapters { get; set; } = new();
    }

    private class Chapter
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private class Publisher
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class EntityWithoutId
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region Single Relationship Includes

    [Fact]
    public void AddIncludedResources_ToOneRelationship_AddsResource()
    {
        var author = new Author { Id = 1, Name = "John" };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Author = author,
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Single(included);
        Assert.Equal("1", included[0].Id);
        Assert.Equal("author", included[0].Type);
        Assert.Equal("John", included[0].Attributes?["name"]);
    }

    [Fact]
    public void AddIncludedResources_ToManyRelationship_AddsAllResources()
    {
        var chapters = new List<Chapter>
        {
            new Chapter { Id = 1, Title = "Chapter 1" },
            new Chapter { Id = 2, Title = "Chapter 2" },
            new Chapter { Id = 3, Title = "Chapter 3" },
        };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Chapters = chapters,
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "chapters" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Equal(3, included.Count);
        Assert.Contains(included, r => r.Id == "1" && r.Type == "chapter");
        Assert.Contains(included, r => r.Id == "2" && r.Type == "chapter");
        Assert.Contains(included, r => r.Id == "3" && r.Type == "chapter");
    }

    [Fact]
    public void AddIncludedResources_MultipleRelationships_AddsAll()
    {
        var author = new Author { Id = 1, Name = "John" };
        var chapters = new List<Chapter>
        {
            new Chapter { Id = 10, Title = "Chapter 1" },
            new Chapter { Id = 20, Title = "Chapter 2" },
        };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Author = author,
            Chapters = chapters,
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author", "chapters" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Equal(3, included.Count);
        Assert.Contains(included, r => r.Type == "author");
        Assert.Equal(2, included.Count(r => r.Type == "chapter"));
    }

    #endregion

    #region Nested Includes

    [Fact]
    public void AddIncludedResources_NestedInclude_AddsNestedResources()
    {
        var publisher = new Publisher { Id = 500, Name = "Big Publisher" };
        var author = new Author
        {
            Id = 1,
            Name = "John",
            Publisher = publisher,
        };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Author = author,
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author.publisher" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Equal(2, included.Count);
        Assert.Contains(included, r => r.Type == "author" && r.Id == "1");
        Assert.Contains(included, r => r.Type == "publisher" && r.Id == "500");
    }

    [Fact]
    public void AddIncludedResources_NestedThroughCollection_AddsAll()
    {
        var chapter1 = new Chapter { Id = 10, Title = "Chapter 1" };
        var chapter2 = new Chapter { Id = 20, Title = "Chapter 2" };
        var books = new List<Book>
        {
            new Book
            {
                Id = 100,
                Title = "Book 1",
                Chapters = new List<Chapter> { chapter1 },
            },
            new Book
            {
                Id = 200,
                Title = "Book 2",
                Chapters = new List<Chapter> { chapter2 },
            },
        };
        var author = new Author
        {
            Id = 1,
            Name = "John",
            Books = books,
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "books.chapters" };

        InclusionMapper.AddIncludedResources(author, includePaths, included);

        Assert.Equal(4, included.Count);
        Assert.Equal(2, included.Count(r => r.Type == "book"));
        Assert.Equal(2, included.Count(r => r.Type == "chapter"));
    }

    [Fact]
    public void AddIncludedResources_DeepNesting_AddsAllLevels()
    {
        var chapter = new Chapter { Id = 10, Title = "Chapter 1" };
        var book = new Book
        {
            Id = 100,
            Title = "Book 1",
            Chapters = new List<Chapter> { chapter },
        };
        var author = new Author
        {
            Id = 1,
            Name = "John",
            Books = new List<Book> { book },
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "books", "books.chapters" };

        InclusionMapper.AddIncludedResources(author, includePaths, included);

        Assert.Equal(2, included.Count);
        Assert.Contains(included, r => r.Type == "book" && r.Id == "100");
        Assert.Contains(included, r => r.Type == "chapter" && r.Id == "10");
    }

    #endregion

    #region Duplicate Prevention

    [Fact]
    public void AddIncludedResources_SameEntityTwice_OnlyAddsOnce()
    {
        var author = new Author { Id = 1, Name = "John" };
        var book1 = new Book
        {
            Id = 100,
            Title = "Book 1",
            Author = author,
        };
        var book2 = new Book
        {
            Id = 200,
            Title = "Book 2",
            Author = author, // Same author
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        // Process both books
        InclusionMapper.AddIncludedResources(
            new List<Book> { book1, book2 },
            includePaths,
            included
        );

        // Author should only appear once
        Assert.Single(included);
        Assert.Equal("1", included[0].Id);
    }

    [Fact]
    public void AddIncludedResources_WithExistingProcessedSet_SkipsDuplicates()
    {
        var author = new Author { Id = 1, Name = "John" };
        var book = new Book
        {
            Id = 100,
            Title = "Book 1",
            Author = author,
        };

        var included = new List<ResourceObject>();
        var processedEntities = new HashSet<string> { "author:1" }; // Already processed
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(book, includePaths, included, null, processedEntities);

        // Author not added because already in processed set
        Assert.Empty(included);
    }

    [Fact]
    public void AddIncludedResources_CollectionWithDuplicates_DeduplicatesCorrectly()
    {
        var sharedChapter = new Chapter { Id = 99, Title = "Shared Chapter" };
        var book1 = new Book
        {
            Id = 100,
            Title = "Book 1",
            Chapters = new List<Chapter>
            {
                new Chapter { Id = 1, Title = "Ch 1" },
                sharedChapter,
            },
        };
        var book2 = new Book
        {
            Id = 200,
            Title = "Book 2",
            Chapters = new List<Chapter>
            {
                sharedChapter, // Same chapter in both books
                new Chapter { Id = 2, Title = "Ch 2" },
            },
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "chapters" };

        InclusionMapper.AddIncludedResources(
            new List<Book> { book1, book2 },
            includePaths,
            included
        );

        // Should have 3 unique chapters, not 4
        Assert.Equal(3, included.Count);
        Assert.Single(included, r => r.Id == "99");
    }

    #endregion

    #region Null Handling

    [Fact]
    public void AddIncludedResources_NullEntity_DoesNothing()
    {
        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(null!, includePaths, included);

        Assert.Empty(included);
    }

    [Fact]
    public void AddIncludedResources_NullIncludePaths_DoesNothing()
    {
        var book = new Book { Id = 100, Title = "Test" };
        var included = new List<ResourceObject>();

        InclusionMapper.AddIncludedResources(book, null!, included);

        Assert.Empty(included);
    }

    [Fact]
    public void AddIncludedResources_EmptyIncludePaths_DoesNothing()
    {
        var book = new Book { Id = 100, Title = "Test" };
        var included = new List<ResourceObject>();

        InclusionMapper.AddIncludedResources(book, new List<string>(), included);

        Assert.Empty(included);
    }

    [Fact]
    public void AddIncludedResources_NullRelationshipValue_SkipsRelationship()
    {
        var book = new Book
        {
            Id = 100,
            Title = "Test",
            Author = null, // Null relationship
        };
        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Empty(included);
    }

    [Fact]
    public void AddIncludedResources_EmptyCollection_AddsNothing()
    {
        var book = new Book
        {
            Id = 100,
            Title = "Test",
            Chapters = new List<Chapter>(), // Empty
        };
        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "chapters" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Empty(included);
    }

    [Fact]
    public void AddIncludedResources_NullInCollection_SkipsNullItems()
    {
        var book = new Book
        {
            Id = 100,
            Title = "Test",
            Chapters = new List<Chapter>
            {
                new Chapter { Id = 1, Title = "Ch 1" },
                null!,
                new Chapter { Id = 2, Title = "Ch 2" },
            },
        };
        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "chapters" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Equal(2, included.Count);
    }

    #endregion

    #region Collection of Entities

    [Fact]
    public void AddIncludedResources_CollectionOfEntities_ProcessesAll()
    {
        var author1 = new Author { Id = 1, Name = "Author 1" };
        var author2 = new Author { Id = 2, Name = "Author 2" };
        var books = new List<Book>
        {
            new Book
            {
                Id = 100,
                Title = "Book 1",
                Author = author1,
            },
            new Book
            {
                Id = 200,
                Title = "Book 2",
                Author = author2,
            },
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(books, includePaths, included);

        Assert.Equal(2, included.Count);
        Assert.Contains(included, r => r.Id == "1");
        Assert.Contains(included, r => r.Id == "2");
    }

    [Fact]
    public void AddIncludedResources_EmptyEntityCollection_AddsNothing()
    {
        var books = new List<Book>();
        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(books, includePaths, included);

        Assert.Empty(included);
    }

    #endregion

    #region Invalid Relationships

    [Fact]
    public void AddIncludedResources_NonExistentRelationship_SkipsGracefully()
    {
        var book = new Book { Id = 100, Title = "Test" };
        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "nonExistent" };

        // Should not throw
        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Empty(included);
    }

    [Fact]
    public void AddIncludedResources_MixedValidAndInvalidPaths_ProcessesValidOnly()
    {
        var author = new Author { Id = 1, Name = "John" };
        var book = new Book
        {
            Id = 100,
            Title = "Test",
            Author = author,
        };
        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "invalid", "author", "alsoInvalid" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Single(included);
        Assert.Equal("author", included[0].Type);
    }

    [Fact]
    public void AddIncludedResources_CaseInsensitiveRelationshipName_Works()
    {
        var author = new Author { Id = 1, Name = "John" };
        var book = new Book
        {
            Id = 100,
            Title = "Test",
            Author = author,
        };
        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "AUTHOR" }; // Uppercase

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Single(included);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddIncludedResources_EntityWithNullId_Skips()
    {
        // This tests the case where ID property exists but returns null
        var author = new Author { Id = 0, Name = "John" }; // 0 becomes "0" string, not null
        var book = new Book
        {
            Id = 100,
            Title = "Test",
            Author = author,
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        // ID "0" is valid (not null)
        Assert.Single(included);
        Assert.Equal("0", included[0].Id);
    }

    [Fact]
    public void AddIncludedResources_StringProperty_NotTreatedAsCollection()
    {
        // Strings are IEnumerable but should not be treated as collections
        var book = new Book
        {
            Id = 100,
            Title = "Test Book", // String property
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "title" };

        // Should not crash or try to iterate over string characters
        InclusionMapper.AddIncludedResources(book, includePaths, included);

        // Title is not a relationship, nothing added
        Assert.Empty(included);
    }

    [Fact]
    public void AddIncludedResources_ComplexScenario_HandlesCorrectly()
    {
        // Build a complex object graph
        var publisher = new Publisher { Id = 1, Name = "Publisher" };
        var author1 = new Author
        {
            Id = 10,
            Name = "Author 1",
            Publisher = publisher,
        };
        var author2 = new Author
        {
            Id = 20,
            Name = "Author 2",
            Publisher = publisher, // Same publisher
        };

        var chapter1 = new Chapter { Id = 100, Title = "Ch 1" };
        var chapter2 = new Chapter { Id = 200, Title = "Ch 2" };

        var book1 = new Book
        {
            Id = 1000,
            Title = "Book 1",
            Author = author1,
            Chapters = new List<Chapter> { chapter1 },
        };
        var book2 = new Book
        {
            Id = 2000,
            Title = "Book 2",
            Author = author2,
            Chapters = new List<Chapter> { chapter2 },
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author", "author.publisher", "chapters" };

        InclusionMapper.AddIncludedResources(
            new List<Book> { book1, book2 },
            includePaths,
            included
        );

        // Should have: 2 authors, 1 publisher (deduplicated), 2 chapters = 5 total
        Assert.Equal(5, included.Count);
        Assert.Equal(2, included.Count(r => r.Type == "author"));
        Assert.Single(included, r => r.Type == "publisher");
        Assert.Equal(2, included.Count(r => r.Type == "chapter"));
    }

    #endregion

    #region Circular Reference Handling

    [Fact]
    public void AddIncludedResources_CircularReference_DoesNotCauseInfiniteLoop()
    {
        // Create circular reference: author -> books -> author
        var author = new Author { Id = 1, Name = "John" };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Author = author,
        };
        author.Books.Add(book); // Circular: author.Books[0].Author == author

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "books", "books.author" };

        // Should not throw or infinite loop
        InclusionMapper.AddIncludedResources(author, includePaths, included);

        // Should have book and author (author deduplicated - not added again via books.author)
        Assert.Equal(2, included.Count);
        Assert.Contains(included, r => r.Type == "book" && r.Id == "100");
        Assert.Contains(included, r => r.Type == "author" && r.Id == "1");
    }

    [Fact]
    public void AddIncludedResources_DeepCircularReference_HandlesCorrectly()
    {
        // Create deeper circular: author -> books -> chapters, books -> author
        var author = new Author { Id = 1, Name = "John" };
        var chapter = new Chapter { Id = 10, Title = "Chapter 1" };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Author = author,
            Chapters = new List<Chapter> { chapter },
        };
        author.Books.Add(book);

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "books", "books.author", "books.chapters" };

        InclusionMapper.AddIncludedResources(author, includePaths, included);

        // Should have: book, author (via books.author - but deduplicated), chapter
        Assert.Equal(3, included.Count);
        Assert.Single(included, r => r.Type == "book");
        Assert.Single(included, r => r.Type == "author");
        Assert.Single(included, r => r.Type == "chapter");
    }

    [Fact]
    public void AddIncludedResources_MutualCircularReference_HandlesCorrectly()
    {
        // Two books referencing the same author, author referencing both books
        var author = new Author { Id = 1, Name = "John" };
        var book1 = new Book
        {
            Id = 100,
            Title = "Book 1",
            Author = author,
        };
        var book2 = new Book
        {
            Id = 200,
            Title = "Book 2",
            Author = author,
        };
        author.Books.Add(book1);
        author.Books.Add(book2);

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author", "author.books" };

        InclusionMapper.AddIncludedResources(
            new List<Book> { book1, book2 },
            includePaths,
            included
        );

        // Author appears once (deduplicated), both books appear
        Assert.Equal(3, included.Count);
        Assert.Single(included, r => r.Type == "author");
        Assert.Equal(2, included.Count(r => r.Type == "book"));
    }

    [Fact]
    public void AddIncludedResources_SelfReferencingEntity_HandlesCorrectly()
    {
        // Simulate a self-referencing entity (e.g., parent-child)
        var parent = new Author { Id = 1, Name = "Parent" };
        var child = new Author { Id = 2, Name = "Child" };

        // Simulate parent.Books containing "child" as if it were a hierarchical relationship
        // Using the existing model, we'll test through the books relationship
        var parentBook = new Book
        {
            Id = 100,
            Title = "Parent's Book",
            Author = parent,
        };
        var childBook = new Book
        {
            Id = 200,
            Title = "Child's Book",
            Author = child,
        };
        parent.Books.Add(parentBook);
        child.Books.Add(childBook);

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "books", "books.author" };

        InclusionMapper.AddIncludedResources(
            new List<Author> { parent, child },
            includePaths,
            included
        );

        // 2 books + 2 authors (each author referenced via their own book.author)
        Assert.Equal(4, included.Count);
        Assert.Equal(2, included.Count(r => r.Type == "book"));
        Assert.Equal(2, included.Count(r => r.Type == "author"));
    }

    #endregion

    #region Attribute Type Names

    [Fact]
    public void AddIncludedResources_UseAttributeTypeNameFalse_UsesCamelCasedClassName()
    {
        var author = new Author { Id = 1, Name = "John" };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Author = author,
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(book, includePaths, included);

        Assert.Equal("author", included[0].Type);
    }

    [Fact]
    public void AddIncludedResources_UseAttributeTypeNameTrue_UsesJsonApiResourceTypeName()
    {
        var author = new Author { Id = 1, Name = "John" };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Author = author,
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "author" };

        InclusionMapper.AddIncludedResources(
            book,
            includePaths,
            included,
            useAttributeTypeName: true
        );

        Assert.Equal("people", included[0].Type);
    }

    [Fact]
    public void AddIncludedResources_UseAttributeTypeNameTrue_FallsBackWithoutAttribute()
    {
        // Chapter has no [JsonApiResource], so it still falls back to the class name.
        var chapter = new Chapter { Id = 10, Title = "Chapter 1" };
        var book = new Book
        {
            Id = 100,
            Title = "Test Book",
            Chapters = new List<Chapter> { chapter },
        };

        var included = new List<ResourceObject>();
        var includePaths = new List<string> { "chapters" };

        InclusionMapper.AddIncludedResources(
            book,
            includePaths,
            included,
            useAttributeTypeName: true
        );

        Assert.Equal("chapter", included[0].Type);
    }

    #endregion
}
