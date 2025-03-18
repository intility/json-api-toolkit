using System.Text.Json;
using System.Text.Json.Serialization;
using JsonApiToolkit.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace JsonApiToolkit.Extensions;

/// <summary>
/// Provides extension methods for integrating JsonApiToolkit into the ASP.NET Core dependency injection system.
/// </summary>
/// <remarks>
/// Contains the core setup method for registering and configuring all JsonApiToolkit components
/// in an ASP.NET Core application.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures all necessary services and options for JsonApiToolkit in an ASP.NET Core application.
    /// </summary>
    /// <param name="services">The service collection to add JsonApiToolkit services to</param>
    /// <returns>The service collection for method chaining</returns>
    /// <remarks>
    /// This method performs the following configuration steps:
    /// <list type="number">
    /// <item>
    /// <description>Configures JSON serialization options:
    /// <list type="bullet">
    /// <item>
    /// <description>Sets camelCase property naming to comply with JSON:API naming conventions</description>
    /// </item>
    /// <item>
    /// <description>Ignores null values to reduce response size</description>
    /// </item>
    /// <item>
    /// <description>Configures reference handling to prevent circular references</description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// <item>
    /// <description>Adds support for the JSON:API media type:
    /// <list type="bullet">
    /// <item>
    /// <description>Registers "application/vnd.api+json" as a supported media type for JSON formatters</description>
    /// </item>
    /// <item>
    /// <description>Ensures proper content negotiation for JSON:API responses</description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// <item>
    /// <description>Registers the JSON:API exception filter:
    /// <list type="bullet">
    /// <item>
    /// <description>Provides standardized error handling for unhandled exceptions</description>
    /// </item>
    /// <item>
    /// <description>Formats errors according to the JSON:API specification</description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// </list>
    /// Call this method in your Startup.ConfigureServices or Program.cs to fully configure JsonApiToolkit.
    /// <para>
    /// Example:
    /// <code>
    /// builder.Services.AddJsonApiToolkit();
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddJsonApiToolkit(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

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
        });

        services.AddScoped<JsonApiExceptionFilter>();

        return services;
    }
}
