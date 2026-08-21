# Contributing Guide

## Prerequisites

- .NET 10 SDK, [Deno](https://deno.com) 2.x, [just](https://github.com/casey/just), [lefthook](https://lefthook.dev)
  (all pinned in `mise.toml`; run `mise install` if you use [mise](https://mise.jdx.dev))
- `uv` for the docs site (`brew install uv` or `mise use uv`)

## Setup

```bash
just setup   # dotnet tool restore, dotnet restore, lefthook install
```

This installs git hooks (lefthook) that format-check on commit and type-check
on push; see `.lefthook.yml`. CI (`ci-cd.yml`, `typescript-ci.yml`,
`contract-tests.yml`) is still the source of truth and runs the full suites.

## Daily Commands

Run `just` (no arguments) to list all recipes, or `just --list`.

```bash
just format          # format .NET + TypeScript
just check           # format-check + type-check everything, no fixes
just test            # .NET tests + TypeScript unit tests, in parallel
just test-contract   # build & run samples/ContractApi, run the Deno contract suite against it
just test-all        # check + test + test-contract (what CI runs)
```

Narrower recipes (`format-dotnet`, `format-ts`, `test-dotnet`, `test-ts`,
`lint`, `clean`) are also available; see the `justfile`.

Equivalent plain commands, if you don't have `just`:

```bash
dotnet build --configuration Release
dotnet test  --configuration Release
dotnet csharpier format .
cd clients/typescript && deno task test
```

## Docs

The site is built with mkdocs (Material) and served from GitHub Pages.

```bash
uv venv
uv pip install -r docs/requirements.txt
uv run mkdocs serve         
```

If you change C# XML doc comments, regenerate the API reference first:

```bash
dotnet build JsonApiToolkit/JsonApiToolkit.csproj -c Release -o JsonApiToolkit/bin/docs
dotnet tool run dotnet-api-docs -- --input JsonApiToolkit/bin/docs --output docs/api --strict
```

## Tests

Tests live in `JsonApiToolkit.Tests/` and use xUnit + EF Core in-memory databases. Integration tests spin up a `TestServer` via `HostBuilder`; see existing tests in `Integration/` for the pattern.

Naming: `MethodName_Scenario_ExpectedBehavior` (e.g. `ApplyPagination_WithPageSizeZero_ClampsToOne`).

When adding behavior, add tests covering the happy path, boundaries, and error conditions.

## Commits

Conventional Commits, enforced indirectly by Release Please:

| Prefix | Bump | Notes |
|---|---|---|
| `fix:` | patch | |
| `feat:` | minor | |
| `feat!:` / `fix!:` | major | breaking change |
| `perf:`, `refactor:`, `docs:`, `build:`, `ci:`, `test:`, `style:` | none | shows in changelog |
| `chore:` | none | hidden from changelog |

Branch names: `feat/`, `fix/`, `refactor/`, `docs/`, `test/`, `chore/`, etc.

## Pull Requests

1. Branch from `main`.
2. Format (`dotnet csharpier format .`) and run tests locally.
3. Open a PR with a descriptive title and summary.
4. CI must pass.
5. At least one approving review is required.

## Releases

Handled by [Release Please](https://github.com/googleapis/release-please). Merging to `main` updates a release PR that accumulates changes. Merging the release PR cuts a GitHub Release, publishes to NuGet, and bumps the version in `JsonApiToolkit.csproj` and `mkdocs.yaml`.

## Questions

Open an issue: <https://github.com/intility/json-api-toolkit/issues>
