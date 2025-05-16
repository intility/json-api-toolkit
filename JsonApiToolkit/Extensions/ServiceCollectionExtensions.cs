using System.Text.Json;
using System.Text.Json.Serialization;
using JsonApiToolkit.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace JsonApiToolkit.Extensions
{
    /// <summary>
    /// Provides extension methods for integrating JsonApiToolkit into the ASP.NET Core dependency injection system.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures all necessary services and options for JsonApiToolkit in an ASP.NET Core application.
        /// Also configures OpenAPI/Swagger to use the correct JSON:API content types for controllers tagged with GroupName = "JsonApi".
        /// </summary>
        /// <param name="services">The service collection to add JsonApiToolkit services to.</param>
        /// <returns>The service collection for method chaining.</returns>
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

            return services;
        }
    }
}
