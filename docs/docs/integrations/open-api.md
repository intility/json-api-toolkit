# OpenAPI Integration

JsonApiToolkit doesn't ship an OpenAPI integration. This guide shows how to configure `Microsoft.AspNetCore.OpenApi` to work correctly with JSON:API.

## Setup

```bash
dotnet add package Microsoft.AspNetCore.OpenApi
```

> [!TIP]
> [Scalar](https://scalar.com) is a great UI for `Microsoft.AspNetCore.OpenApi` and is the recommended choice for .NET 9+.
> ```bash
> dotnet add package Scalar.AspNetCore
> ```

## Content Type

The main thing to get right: JSON:API requires `application/vnd.api+json` as the content type for both requests and responses. Without this, Scalar/OpenAPI clients will send `application/json` and get a 415 back.

Use a document transformer to replace `application/json` with `application/vnd.api+json` across all operations:

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

public class JsonApiContentTypeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem.Operations == null) continue;

            foreach (var operation in pathItem.Operations.Values)
            {
                ReplaceContentType(operation.RequestBody?.Content);

                foreach (var response in operation.Responses.Values)
                    ReplaceContentType(response.Content);
            }
        }

        return Task.CompletedTask;
    }

    private static void ReplaceContentType(IDictionary<string, IOpenApiMediaType>? content)
    {
        if (content == null || !content.TryGetValue("application/json", out var schema))
            return;

        content.Remove("application/json");
        content["application/vnd.api+json"] = schema;
    }
}
```

## Query Parameters

JSON:API's bracket-based query syntax (`filter[field]`, `fields[type]`, `page[number]`) isn't natively modeled in OpenAPI 3.0. Add them as plain string parameters so they're visible and testable in Scalar:

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

public class JsonApiParametersTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "filter[field]",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            Description = "Filter by attribute. Operators: `eq`, `ne`, `gt`, `lt`, `like`, `in`, `isnull`. Example: `filter[title][like]=Hobbit`"
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "fields[type]",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            Description = "Sparse fieldsets. Replace `type` with the resource type name. Example: `fields[books]=title,genre`"
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "include",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            Description = "Include related resources, comma-separated. Example: `include=author,reviews`"
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "sort",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            Description = "Sort fields, comma-separated. Prefix with `-` for descending. Example: `sort=name,-createdAt`"
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "page[number]",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer },
            Description = "Page number (1-based)."
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "page[size]",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer },
            Description = "Items per page. Capped at `JsonApiOptions.MaxPageSize` (default: 100)."
        });

        return Task.CompletedTask;
    }
}
```

## Register

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JsonApiContentTypeTransformer>();
    options.AddOperationTransformer<JsonApiParametersTransformer>();
});

app.MapScalarApiReference();
app.MapOpenApi();
```
