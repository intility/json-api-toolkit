[![CI/CD Pipeline](https://github.com/intility/Intility.JsonApiToolkit/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/intility/Intility.JsonApiToolkit/actions/workflows/ci-cd.yml)
[![Build Docs](https://github.com/intility/Intility.JsonApiToolkit/actions/workflows/build-docs.yml/badge.svg)](https://github.com/intility/Intility.JsonApiToolkit/actions/workflows/build-docs.yml)

# Intility.JsonApiToolkit

JsonApiToolkit is a lightweight toolkit for implementing the JSON:API specification in .NET applications. 

## Installation

To install this package from Intility's GitHub Packages, add this to your NuGet.config file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/Intility/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </github>
  </packageSourceCredentials>
</configuration>
```

Then install the package via NuGet:

```bash
dotnet add package Intility.JsonApiToolkit
```

## Documentation
For complete documentation and detailed usage instructions, please visit our 
[documentation page.](https://intility.github.io/Intility.JsonApiToolkit/).
