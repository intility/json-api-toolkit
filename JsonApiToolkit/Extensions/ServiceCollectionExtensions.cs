using System.Text.Json;
using System.Text.Json.Serialization;
using JsonApiToolkit.Configuration;
using JsonApiToolkit.Filters;
using JsonApiToolkit.Services;
using JsonApiToolkit.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JsonApiToolkit.Extensions;

/// <summary>
/// Extension methods for registering JsonApiToolkit services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers JsonApiToolkit services with default options.
    /// </summary>
    public static IServiceCollection AddJsonApiToolkit(this IServiceCollection services) =>
        AddJsonApiToolkit(services, _ => { });

    /// <summary>
    /// Registers JsonApiToolkit services with custom options configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure JsonApiOptions.</param>
    public static IServiceCollection AddJsonApiToolkit(
        this IServiceCollection services,
        Action<JsonApiOptions> configure
    )
    {
        // Register options
        services.Configure(configure);
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

        // Register query parser service
        services.AddScoped<IJsonApiQueryParser, JsonApiQueryParserService>();

        // Register include pattern validator for startup validation
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IApplicationModelProvider, IncludePatternValidator>()
        );

        return services;
    }
}
