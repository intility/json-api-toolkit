# Use cases

JsonApiToolkit was designed to handle a variety of common API scenarios where adherence to the JSON:API specification is desired.

## 1. Single Resource Endpoints

When you need to return only one resource, for example:

- **GET /api/books/123**  
  Returns a JSON:API document with one resource object.  
  
*Toolkit Benefit:* Automatically maps the entity to a resource object and adds self links.

## 2. Collection Endpoints

For endpoints returning lists of resources:

- **GET /api/books**  
  Returns a JSON:API collection document, complete with pagination metadata and navigational links.  

*Toolkit Benefit:* Supports filtering, sorting, and pagination together using a single structured query.

## 3. Advanced Querying

When clients require dynamic querying, such as:

- **Filtering:** Selecting resources based on attribute values (e.g., books where `price > 10`).
- **Sorting:** Ordering resources by multiple criteria (e.g., by `title` and then by `publishedDate`).
- **Including Related Resources:** Allowing deep inclusion of relationships (e.g., including authors along with books).

*Toolkit Benefit:* Use the provided LINQ extensions to apply these parameters directly to your data queries, ensuring the API responds correctly to client-specified parameters.

## 4. Standardized Error Handling

For scenarios where errors need to be communicated in a consistent format:

- **400 Bad Request** for client errors.
- **404 Not Found** when a resource is missing.
- **500 Internal Server Error** for unexpected failures.

*Toolkit Benefit:* The built-in exception filter converts unhandled exceptions into JSON:API compliant error responses, making error handling uniform across your API.

## 5. Enterprise-Level APIs

Integrate JsonApiToolkit in large-scale systems where:

- Consistent API responses are crucial.
- Multiple teams interact with the API.
- Advanced query capabilities are used for reporting and analytics.

*Toolkit Benefit:* By conforming to a well-defined specification, JsonApiToolkit helps to ensure predictable, standardized API responses that scale with your application's needs.
