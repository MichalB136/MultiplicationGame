# MultiplicationGame

![.NET Tests](https://github.com/MichalB136/MultiplicationGame/actions/workflows/dotnet-test.yml/badge.svg?branch=main)

MultiplicationGame is an ASP.NET Core Razor Pages app for practicing multiplication and other arithmetic operations. It targets .NET 10 LTS.

## Run locally

Prerequisite: the .NET 10 SDK.

```sh
dotnet run --project MultiplicationGame.csproj --launch-profile http
```

The HTTP launch profile serves the app at <http://localhost:5000>.

## Build and test

The test project is separate from `MultiplicationGame.sln`; target it directly to include the tests:

```sh
dotnet restore tests/MultiplicationGame.Tests/MultiplicationGame.Tests.csproj
dotnet build tests/MultiplicationGame.Tests/MultiplicationGame.Tests.csproj --no-restore
dotnet test tests/MultiplicationGame.Tests/MultiplicationGame.Tests.csproj --no-build
```

## Documentation

See the [application guide](docs/application-guide.md) for the main user flows, architecture, API endpoints, configuration, and Mermaid diagrams.

To enable the repository's pre-commit formatting hook in a clone:

```sh
git config core.hooksPath .githooks
```
