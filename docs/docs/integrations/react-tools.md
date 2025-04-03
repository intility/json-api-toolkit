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

JsonApiToolkit responses separate primary resource data from related resources, which are returned in an `included` array. The primary resource’s relationships contain only resource identifiers. To work with full resource objects and ensure type safety, you must first define your resource attributes and create a central type registry that maps resource type strings to these attribute interfaces.

#### Define Resource Attributes & Create a Type Registry

First, define interfaces for each resource's attributes:

```ts
export interface CompanyAttributes {
  companyName: string;
  companyCode: string;
  establishedAt: string;
}

export interface LocationAttributes {
  address: string;
  city: string;
  country: string;
}

export interface EmployeeAttributes {
  firstName: string;
  lastName: string;
  email: string;
}
```

Then, create a registry that maps the resource type strings (as they are returned by your API) to your interfaces:

```ts
import { JsonApiTypeRegistry } from "@intility/jsonapi-react-tools";
import { CompanyAttributes } from "./Company";
import { LocationAttributes } from "./Location";
import { EmployeeAttributes } from "./Employee";

export interface AppTypeRegistry extends JsonApiTypeRegistry {
  companies: CompanyAttributes;
  locations: LocationAttributes;
  employees: EmployeeAttributes;
}
```

This registry is used throughout your application to ensure that all JSON:API responses are strongly typed.

#### 2. Extracting Related Resources

There are two cases for extracting related resources from a JSON:API response:

**A. Single Resource Responses:**  
When fetching a single resource (using a `JsonApiDocument`), all related resources are still available in the `included` array. In this case, you can use the built-in `getIncludedOfType` helper to filter the `included` array by resource type. For example:

```tsx
import { getIncludedOfType } from "@intility/jsonapi-react-tools";

export function CompanyDetail({ companyId }: { companyId: string }) {
  const { data: companyDocument, isLoading } = useCompany(companyId);

  if (isLoading || !companyDocument || !companyDocument.data) {
    return <div>Loading company details...</div>;
  }

  // Use getIncludedOfType to extract related locations and employees
  const locations = getIncludedOfType(companyDocument, "locations");
  const employees = getIncludedOfType(companyDocument, "employees");

  return (
    <div>
      <h2>{companyDocument.data.attributes.companyName}</h2>
      <p>Code: {companyDocument.data.attributes.companyCode}</p>
      <p>
        Established:{" "}
        <FormatDate date={companyDocument.data.attributes.establishedAt} />
      </p>
      <section>
        <h3>Locations</h3>
        <ul>
          {locations.map((loc) => (
            <li key={loc.id}>
              {loc.attributes.address}, {loc.attributes.city},{" "}
              {loc.attributes.country}
            </li>
          ))}
        </ul>
      </section>
      <section>
        <h3>Employees</h3>
        <ul>
          {employees.map((emp) => (
            <li key={emp.id}>
              {emp.attributes.firstName} {emp.attributes.lastName} -{" "}
              {emp.attributes.email}
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
```

In this example, the company’s related locations and employees are retrieved directly from the `included` array using `getIncludedOfType`.

**B. Collection Responses:**  
When dealing with collection responses (using a `JsonApiCollectionDocument`), each primary resource defines its relationships with only resource identifiers. To resolve these identifiers into full resource objects, use the `resolveRelationship` helper. For example, in a component listing companies:

```tsx
import { useCompanies } from "~/hooks/useCompanies";
import { Table } from "@intility/bifrost-react";
import { resolveRelationship } from "@intility/jsonapi-react-tools";

export function CompanyList() {
  const { data: companiesDocument, isLoading } = useCompanies();

  if (isLoading || !companiesDocument) {
    return <div>Loading companies...</div>;
  }

  return (
    <Table>
        <Table.Header>
            <Table.Row>
                <Table.HeaderCell>Company Name</Table.HeaderCell>
                <Table.HeaderCell>Locations</Table.HeaderCell>
                <Table.HeaderCell>Employees</Table.HeaderCell>
            </Table.Row>
        </Table.Header>
        <Table.Body>
            {companiesDocument.data.map((company) => {
            // Resolve the 'locations' and 'employees' relationships for each company.
            // Note: The relationship names here (e.g., "locations") must match those
            // returned by your API.
            const companyLocations = company.relationships?.locations?.data
                ? resolveRelationship(
                    company.relationships.locations.data,
                    companiesDocument,
                    "locations"
                )
                : [];
            const companyEmployees = company.relationships?.employees?.data
                ? resolveRelationship(
                    company.relationships.employees.data,
                    companiesDocument,
                    "employees"
                )
                : [];

            return (
                <Table.Row key={company.id}>
                    <Table.Cell>{company.attributes.companyName}</Table.Cell>
                    <Table.Cell>
                        {companyLocations.map((loc) => loc.attributes.address).join(
                        ", "
                        )}
                    </Table.Cell>
                    <Table.Cell>
                        {companyEmployees
                        .map(
                            (emp) =>
                            `${emp.attributes.firstName} ${emp.attributes.lastName}`
                        )
                        .join(", ")}
                    </Table.Cell>
                </Table.Row>
            );
            })}
        </Table.Body>
    </Table>
  );
}
```

Here, the `resolveRelationship` helper maps the relationship identifiers (from the primary company resource) to the full location and employee objects from the collection’s `included` array.

### Further Information

For more details on the package itself, visit the repository:

*   **GitHub:** [jsonapi-react-tools](https://github.com/intility/jsonapi-react-tools)
  
