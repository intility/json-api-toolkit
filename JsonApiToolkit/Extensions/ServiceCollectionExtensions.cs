using System.Text.Json;
using System.Text.Json.Serialization;
using JsonApiToolkit.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace JsonApiToolkit.Extensions
{
    /// <summary>
    /// Provides extension methods for integrating JsonApiToolkit into the ASP.NET Core dependency injection system.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures all necessary services and options for JsonApiToolkit in an ASP.NET Core application.
        /// Also configures OpenAPI/Swagger to use the correct JSON:API content types for controllers inheriting from JsonApiController.
        /// </summary>
        /// <param name="services">The service collection to add JsonApiToolkit services to.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <remarks>
        /// This method:
        /// <list type="number">
        /// <item>
        /// <description>Configures JSON serialization options for JSON:API.</description>
        /// </item>
        /// <item>
        /// <description>Adds support for the JSON:API media type to input/output formatters.</description>
        /// </item>
        /// <item>
        /// <description>Registers JSON:API exception and content-type filters.</description>
        /// </item>
        /// <item>
        /// <description>Configures OpenAPI/Swagger to use "application/vnd.api+json" for all endpoints inheriting from JsonApiController.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public static IServiceCollection AddJsonApiToolkit(this IServiceCollection services)
        {
            // Configure JSON serialization options
            services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            // Configure MVC formatters and filters
            services.Configure<MvcOptions>(options =>
            {
                SystemTextJsonOutputFormatter? jsonOutputFormatter = options
                    .OutputFormatters.OfType<SystemTextJsonOutputFormatter>()
                    .FirstOrDefault();

                if (
                    jsonOutputFormatter?.SupportedMediaTypes.Contains("application/vnd.api+json")
                    == false
                )
                {
                    jsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.api+json");
                }

                SystemTextJsonInputFormatter? jsonInputFormatter = options
                    .InputFormatters.OfType<SystemTextJsonInputFormatter>()
                    .FirstOrDefault();

                if (
                    jsonInputFormatter?.SupportedMediaTypes.Contains("application/vnd.api+json")
                    == false
                )
                {
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.api+json");
                }

                options.Filters.AddService<JsonApiContentTypeFilter>();
            });

            // Register filters
            services.AddScoped<JsonApiExceptionFilter>();
            services.AddScoped<JsonApiContentTypeFilter>();

            // Register OpenAPI document transformer for JSON:API content types
            services.AddOpenApi(o =>
            {
                // OpenAPI document transformer that rewrites request and response content types
                // to "application/vnd.api+json" for all endpoints tagged as "JsonApi".
                o.AddDocumentTransformer(
                    (document, _, _) =>
                    {
                        foreach (var path in document.Paths.Values)
                        {
                            foreach (var operation in path.Operations.Values)
                            {
                                // Only apply to operations tagged as "JsonApi"
                                if (operation.Tags.Any(t => t.Name == "JsonApi"))
                                {
                                    // Rewrite request body content types
                                    if (operation.RequestBody != null)
                                    {
                                        if (
                                            operation.RequestBody.Content.TryGetValue(
                                                "application/json",
                                                out var jsonContent
                                            )
                                        )
                                        {
                                            operation.RequestBody.Content[
                                                "application/vnd.api+json"
                                            ] = jsonContent;
                                            operation.RequestBody.Content.Remove(
                                                "application/json"
                                            );
                                        }
                                        else if (
                                            !operation.RequestBody.Content.ContainsKey(
                                                "application/vnd.api+json"
                                            )
                                        )
                                        {
                                            operation.RequestBody.Content[
                                                "application/vnd.api+json"
                                            ] = new OpenApiMediaType();
                                        }
                                    }

                                    // Rewrite response content types
                                    foreach (var response in operation.Responses.Values)
                                    {
                                        if (
                                            response.Content.TryGetValue(
                                                "application/json",
                                                out var jsonContent
                                            )
                                        )
                                        {
                                            response.Content["application/vnd.api+json"] =
                                                jsonContent;
                                            response.Content.Remove("application/json");
                                        }
                                    }
                                }
                            }
                        }
                        return Task.CompletedTask;
                    }
                );
            });

            return services;
        }
    }
}
