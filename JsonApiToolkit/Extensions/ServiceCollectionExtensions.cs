using System.Text.Json;
using System.Text.Json.Serialization;
using JsonApiToolkit.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace JsonApiToolkit.Extensions;

/// <summary>
/// Contains extension methods for configuring JSON:API Toolkit services in the application's dependency injection container.
/// </summary>
/// <remarks>
/// This class provides helper methods to register and configure components required for JSON:API compliance,
/// such as exception filters, JSON serialization settings, and media type support for JSON:API responses.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers JSON:API Toolkit components and configures necessary options for JSON:API compliance.
    /// </summary>
    /// <param name="services">The service collection to which the JSON:API Toolkit services will be added.</param>
    /// <returns>The updated service collection including registered JSON:API Toolkit services.</returns>
    /// <remarks>
    /// This method performs the following actions:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Adds a scoped JSON:API exception filter to handle API-specific exceptions.</description>
    ///   </item>
    ///   <item>
    ///     <description>Configures global JSON serialization options to use camel-case property naming and to ignore null values,
    ///     ensuring consistency with JSON:API formatting requirements.</description>
    ///   </item>
    ///   <item>
    ///     <description>Adds the "application/vnd.api+json" media type to the list of supported media types for JSON output formatters,
    ///     enabling proper content negotiation for JSON:API responses.</description>
    ///   </item>
    /// </list>
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
        });

        services.AddScoped<JsonApiExceptionFilter>();

        return services;
    }
}
