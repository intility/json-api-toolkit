# Introduction

JsonApiToolkit is a lightweight toolkit for implementing the JSON:API specification in .NET applications. It streamlines the process of exposing resources in a standardized format by providing built‐in support for:

- **Mapping:** Converts entities into JSON:API resource objects.
- **Querying:** Applies advanced filtering, sorting, pagination, and inclusion of related resources.
- **Error Handling:** Returns consistent and compliant error responses.
- **Content Negotiation:** Supports the `application/vnd.api+json` media type out-of-the-box.

This toolkit is ideal for API developers who want to quickly build RESTful services that conform to JSON:API standards without reinventing the wheel.

**Key Features:**
- Easy configuration and integration via dependency injection.
- Strongly typed LINQ query extensions for filtering and sorting.
- Built-in pagination with self, first, last, prev, and next links.
- Comprehensive support for error response formatting.
- A robust parsing system for all JSON:API query parameters.

Whether you’re building a new API or modernizing an existing one, JsonApiToolkit provides the tools needed to create clean, consistent, and extensible APIs in .NET.
