set dotenv-load := false

sample := "samples/ContractApi"
ts := "clients/typescript"

# List available recipes
default:
    @just --list

# ---------------------------------------------------------------------------
# Setup
# ---------------------------------------------------------------------------

# Full bootstrap: dotnet tools/packages, git hooks
[group('setup')]
setup: tools restore hooks
    @echo "Setup complete. Run 'just test' to verify everything works."

# Restore local dotnet tools (csharpier)
[group('setup')]
tools:
    dotnet tool restore

# Restore NuGet packages
[group('setup')]
restore:
    dotnet restore --locked-mode

# Install lefthook git hooks
[group('setup')]
hooks:
    lefthook install

# ---------------------------------------------------------------------------
# Quality
# ---------------------------------------------------------------------------

# Format all code (.NET + TypeScript) in parallel
[group('quality')]
format:
    #!/usr/bin/env bash
    set -uo pipefail
    pids=()
    dotnet csharpier format . & pids+=($!)
    (cd {{ts}} && deno task fmt:fix) & pids+=($!)
    code=0
    for pid in "${pids[@]}"; do wait "$pid" || code=1; done
    exit $code

# Format .NET only (CSharpier)
[group('quality')]
format-dotnet:
    dotnet csharpier format .

# Format the TypeScript client only (deno fmt)
[group('quality')]
format-ts:
    cd {{ts}} && deno task fmt:fix

# Lint the TypeScript client (deno lint)
[group('quality')]
lint:
    cd {{ts}} && deno task lint

# Type-check and format-check everything, no fixes (.NET + TypeScript) in parallel
[group('quality')]
check:
    #!/usr/bin/env bash
    set -uo pipefail
    pids=()
    dotnet csharpier check . & pids+=($!)
    (cd {{ts}} && deno task check && deno task lint && deno task fmt) & pids+=($!)
    code=0
    for pid in "${pids[@]}"; do wait "$pid" || code=1; done
    exit $code

# Run .NET + TypeScript unit tests in parallel (excludes the contract suite; see test-contract)
[group('quality')]
test:
    #!/usr/bin/env bash
    set -uo pipefail
    pids=()
    dotnet test --configuration Release & pids+=($!)
    (cd {{ts}} && deno task test) & pids+=($!)
    code=0
    for pid in "${pids[@]}"; do wait "$pid" || code=1; done
    exit $code

# .NET tests only (JsonApiToolkit.Tests)
[group('quality')]
test-dotnet:
    dotnet test --configuration Release

# TypeScript client unit tests only
[group('quality')]
test-ts:
    cd {{ts}} && deno task test

# Build & run ContractApi (default + all-strict-opt-ins), run the Deno contract suite, then stop the servers
[group('quality')]
test-contract:
    #!/usr/bin/env bash
    set -euo pipefail
    dotnet build {{sample}} --configuration Release
    ASPNETCORE_URLS=http://localhost:5198 \
        dotnet run --project {{sample}} --configuration Release --no-build &
    default_pid=$!
    JSONAPI_STRICT=true ASPNETCORE_URLS=http://localhost:5199 \
        dotnet run --project {{sample}} --configuration Release --no-build &
    strict_pid=$!
    trap 'kill $default_pid $strict_pid 2>/dev/null || true' EXIT

    for url in http://localhost:5198/articles http://localhost:5199/articles; do
        timeout 60 bash -c "until curl -sf -o /dev/null $url; do sleep 1; done"
    done

    cd {{ts}} && deno task test:contract

# Regenerate api-types.gen.ts for the ContractApi sample from its [JsonApiResource] types.
# Pass "--check" to verify instead of regenerating (what CI runs).
[group('quality')]
typegen *args:
    #!/usr/bin/env bash
    set -euo pipefail
    dotnet build JsonApiToolkit.TypeGen --configuration Release
    dotnet build {{sample}} --configuration Release
    dotnet run --project JsonApiToolkit.TypeGen --configuration Release --no-build -- \
        --assembly {{sample}}/bin/Release/net10.0/ContractApi.dll \
        --out {{sample}}/api-types.gen.ts {{args}}

# Everything CI runs: format check, unit tests, the contract suite, and typegen drift
[group('quality')]
test-all: check test test-contract (typegen "--check")

# ---------------------------------------------------------------------------
# Clean
# ---------------------------------------------------------------------------

# Remove build artifacts (.NET bin/obj)
[group('clean')]
clean:
    #!/usr/bin/env bash
    set -uo pipefail
    dotnet clean JsonApiToolkit.sln > /dev/null 2>&1 || true
    find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
    echo "Cleaned build artifacts."
