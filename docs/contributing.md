# Contributing Guide

## Prerequisites

- .NET 10 SDK (pinned in `global.json` to `10.0.201`)
- `uv` for the docs site (`brew install uv` or `mise use uv`)

## Setup

```bash
dotnet tool restore   # csharpier + dotnet-api-docs
dotnet restore
```

> **Intility contributors:** `NuGet.config` currently resolves dependencies from `nuget.pkg.github.com/Intility`. Until the package moves to public NuGet, restore needs `NUGET_AUTH_TOKEN` set to a GitHub PAT with `read:packages`.

## Daily Commands

```bash
dotnet build --configuration Release
dotnet test  --configuration Release
dotnet csharpier format .   # CI fails on unformatted code
```

## Docs

The site is built with mkdocs (Material) and served from GitHub Pages.

```bash
uv venv
uv pip install -r docs/requirements.txt
uv run mkdocs serve         # http://127.0.0.1:8000, live reload
uv run mkdocs build --strict  # what CI runs; do this before pushing
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
3. Open a PR with a descriptive title and bullet summary.
4. CI must pass `build-and-test` and `Docs: Build` (required status checks).
5. Squash merge (the only merge method allowed on `main`).

## Releases

Handled by [Release Please](https://github.com/googleapis/release-please). Merging to `main` updates a release PR that accumulates changes. Merging the release PR cuts a GitHub Release, publishes to NuGet, and bumps the version in `JsonApiToolkit.csproj` and `mkdocs.yaml`.

## AI-Assisted Contributions

Project-specific guidance for AI tools lives in `AGENTS.md` at the repo root (with `CLAUDE.md` as a symlink). Update it there if you add conventions other contributors' tools should follow.

## Questions

Open an issue: <https://github.com/intility/Intility.JsonApiToolkit/issues>
