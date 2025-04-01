# Integrations / Frontend Consumption

## jsonapi-react-tools: Consuming APIs in React/TypeScript

While JsonApiToolkit focuses on building compliant backend APIs, consuming these APIs effectively on the frontend is crucial. The `@intility/jsonapi-react-tools` package provides TypeScript types and utilities specifically designed to make working with JsonApiToolkit responses seamless in React applications.

It ensures type safety by mirroring the JSON:API structure produced by JsonApiToolkit, allowing you to focus only on defining your resource attributes on the frontend.

### Features

*   **Strong JSON:API Type Definitions:** Provides generic types like `JsonApiDocument<T>`, `JsonApiCollectionDocument<T>`, `ResourceObject<T>`, etc., where `T` is your specific attribute type.
*   **Seamless Integration:** Types are tailored for JsonApiToolkit's output, including support for `data`, `links`, `meta` (with `pagination`), `included`, and `errors`.
*   **Advanced Query Builder:** A type-safe utility to generate complex JSON:API query strings for filtering (including operators and logical groups), sorting, pagination, and inclusion, matching JsonApiToolkit's parsing capabilities.
*   **Framework Agnostic:** Primarily types and helpers, with no hard dependency on specific data-fetching libraries (though examples often use `react-query`).

### Installation

Install the package into your **frontend** React project:

```bash
npm install @intility/jsonapi-react-tools
```

> [!NOTE]
> This package needs to be fetched via the `npm.intility.com` proxy.

### Getting Started: Typing Responses

1.  **Define Your Attribute Types:** In your frontend project, define a type for the `attributes` of your resource.

    ```ts
    export type CompanyAttributes = {
       companyName: string;
       companyCode: string;
       companyTenantId: string;
       protected: boolean;
       lastSyncedAt: string; 
    };
    ```

2.  **Use Types with Data Fetching:** Use the types from `@intility/jsonapi-react-tools` when fetching data. The package provides types for the full JSON:API envelope. In this example we'll use `react-query`, but you can adapt it to any data-fetching library.

    ```tsx
    import { useQuery } from "@tanstack/react-query";
    // Import the document type and your attribute type
    import { JsonApiCollectionDocument } from "@intility/jsonapi-react-tools";
    import { CompanyAttributes } from "~/types/Company.ts"; // Your local type

    // Assuming you have a default query function configured for 'api'
    // that fetches data and returns the parsed JSON response.

    export const CompanyList = () => {
      // Provide the specific document type wrapping your attributes type
      const { data: companyResponse, error, isLoading } = useQuery<
        JsonApiCollectionDocument<CompanyAttributes>
      >({ queryKey: ["api", "companies"] });

      if (isLoading)
        return <div>Loading companies...</div>;
      
      if (error || !companyResponse)
        return <div>Error loading companies</div>;

      return (
        <ul>
          {companyResponse.data.map((company) => (
            <li key={company.id}>
              {company.attributes.companyName} ({company.attributes.companyCode})
            </li>
          ))}
        </ul>
      );
    };
    ```

    This approach keeps the original JSON:API structure intact while providing full type safety based on JsonApiToolkit's output.

### Using the Query Builder

JsonApiToolkit supports rich querying via URL parameters. `jsonapi-react-tools` includes a type-safe query builder to generate these strings easily.

1.  **Import the Builder:**

    ```ts
    import { buildJsonApiQueryString, JsonApiQueryOptions } from "@intility/jsonapi-react-tools";
    import { CompanyAttributes } from "../types/Company.ts"; // Your attribute type
    ```

2.  **Define Query Options:** Create an options object. Field names for `filter` and `sort` are type-checked against your `CompanyAttributes`. Filter operators (`eq`, `ne`, `like`, etc.) have autocompletion.

    ```ts
    const queryOptions: JsonApiQueryOptions<CompanyAttributes> = {
      filter: {
        // Field 'companyName' must exist in CompanyAttributes
        companyName: { like: "Intility" },
        or: [
          // Field 'protected' must exist
          { protected: { eq: true } },
          // Field 'companyCode' must exist
          { companyCode: { in: ["AA", "ZZ"] } }
        ]
      },
      sort: [
        // Field 'companyName' must exist, '-' prefix is allowed
        "-companyName",
        // Field 'lastSyncedAt' must exist
        "lastSyncedAt" 
      ],
      page: {
        number: 1,
        size: 20
      },
      include: ["locations", "employees"] // Relationship names (string array)
    };
    ```

3.  **Generate the Query String:**

    ```typescript
    const queryString = buildJsonApiQueryString<CompanyAttributes>(queryOptions);
    ```
    This will produce a query string like:

    `?filter[companyName][like]=Intility&filter[or][0][protected][eq]=true&filter[or][1][companyCode][in]=AA,ZZ&sort=-companyName,lastSyncedAt&page[number]=1&page[size]=20&include=locations,employees`

    >*This query would fetch companies with names like "Intility", either protected or with specific company codes, sorted by name and last synced date, paginated to the first 20 results, and including related locations and employees.*

4.  **Use with Data Fetching:** Append the `queryString` to your API endpoint URL.

    ```tsx
    const { data: companyResponse, error, isLoading } = useQuery<
        JsonApiCollectionDocument<CompanyAttributes>
      >({ queryKey: ["api", "companies", queryString] });
    ```

    The query builder supports all standard JSON:API operators (`eq`, `ne`, `gt`, `ge`, `lt`, `le`, `like`, `in`, `nin`, `isnull`, `isnotnull`) and logical groupings (`and`, `or`, `not`).

### Working with Relationships (Included Resources)

When your API response includes related resources (using the `include` query parameter), `jsonapi-react-tools` provides type safety for the `included` array. This requires defining a central registry of all your resource types.

1.  **Define Attribute Types for All Resources:** Ensure you have attribute types defined not only for your primary resource (e.g., `CompanyAttributes`) but also for any resources that might appear in the `included` array (e.g., `LocationAttributes`).

    ```ts
    // types/Company.ts
    export type CompanyAttributes = {
      companyName: string;
      companyCode: string;
      // ... other fields
    };

    // types/Location.ts
    export type LocationAttributes = {
      address: string;
      city: string;
      // ... other fields
    };
    ```

2.  **Create Your Application's Type Registry:** Implement the `JsonApiTypeRegistry` interface, mapping the exact `type` string returned by your API to the corresponding attribute interface.

    ```ts
    // types/Registry.ts
    import { JsonApiTypeRegistry } from "@intility/jsonapi-react-tools";
    import { CompanyAttributes } from "./Company";
    import { LocationAttributes } from "./Location";

    // Add all resource types that can appear in 'data' or 'included'
    export interface AppTypeRegistry extends JsonApiTypeRegistry {
      companies: CompanyAttributes; // 'companies' is the type string from the API
      locations: LocationAttributes; // 'locations' is the type string from the API
    }
    ```

3.  **Use the Registry in Your Data Fetching:** Pass your `AppTypeRegistry` as the second generic argument to `JsonApiCollectionDocument` or `JsonApiDocument`.

    ```tsx
    import { useQuery } from "@tanstack/react-query";
    import {
      JsonApiCollectionDocument,
      isResourceType, // Import the type predicate
      buildJsonApiQueryString,
      JsonApiQueryOptions,
    } from "@intility/jsonapi-react-tools";
    import { CompanyAttributes } from "~/types/Company.ts";
    import { AppTypeRegistry } from "~/types/Registry.ts"; // Import your registry

    const queryOptions: JsonApiQueryOptions<CompanyAttributes> = {
      include: ["locations"], // Request related locations
    };
    const queryString = buildJsonApiQueryString(queryOptions);

    export const CompanyListWithLocations = () => {
      // Use the registry in the useQuery type definition
      const { data: response, error, isLoading } = useQuery<
        JsonApiCollectionDocument<CompanyAttributes, AppTypeRegistry> // <--- Pass registry here
      >({ queryKey: ["api", "companies", queryString] });

      if (isLoading) return <div>Loading...</div>;
      if (error || !response) return <div>Error</div>;

      // Process included data safely
      response.included?.forEach((resource) => {
        // Use the type predicate to check the type and narrow it down
        if (isResourceType(resource, "locations")) {
          // TypeScript now knows resource.attributes is LocationAttributes
          console.log("Included Location City:", resource.attributes.city);
        }
        // Add checks for other included types if necessary
      });

      return (
        <ul>
          {response.data.map((company) => (
            <li key={company.id}>
              {company.attributes.companyName}
              {/* You can now safely look up and display related data */}
            </li>
          ))}
        </ul>
      );
    };
    ```

By defining the `AppTypeRegistry` and using the `isResourceType` type predicate, you gain full type safety when working with related resources included in your API responses.


### Further Information

For more details on the package itself, visit the repository:

*   **GitHub:** [jsonapi-react-tools](https://github.com/intility/jsonapi-react-tools)
  
