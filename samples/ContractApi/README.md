# ContractApi

A minimal JsonApiToolkit-backed API used by the TypeScript client's contract
tests (`clients/typescript/contract/`). It exists to pin the actual wire
behavior of the toolkit; it is not a usage showcase.

In-memory EF Core, deterministic fabricated seed data (see `Data.cs`). The
contract test suite asserts against the exact seed rules.

```bash
# default instance (StrictPagination off)
dotnet run --project samples/ContractApi --urls http://localhost:5198

# strict-pagination instance
STRICT_PAGINATION=true dotnet run --project samples/ContractApi --urls http://localhost:5199
```
