# MultiplicationGame

![.NET Tests](https://github.com/MichalB136/MultiplicationGame/actions/workflows/dotnet-test.yml/badge.svg?branch=main)

MultiplicationGame is an ASP.NET Core Razor Pages app for practicing multiplication and other arithmetic operations. It targets .NET 10 LTS.

## Run locally

Prerequisite: the .NET 10 SDK.

```powershell
dotnet restore .\MultiplicationGame.sln
dotnet build .\MultiplicationGame.sln
dotnet run --project .\MultiplicationGame.csproj --launch-profile http
```

The HTTP launch profile serves the app at <http://localhost:5000>. On Windows, run `dotnet run` from an external PowerShell terminal rather than Visual Studio's integrated terminal.

## Game modes

- **Home (`/`)** — mixed multiplication practice with a progress board, streaks, and remaining lives.
- **Training (`/Training`)** — continue mixed practice or choose one or more multiplication tables for a 10-question round.
- **Equations (`/Equations`)** — practice arithmetic equations with selectable difficulty and a progress board.

Training rounds can use adaptive question selection and optional hints. The round summary includes the score, elapsed time, and answers; mistakes can be repeated.

See [process and flow diagrams](docs/flows.md) for Mermaid diagrams of the game modes, training rounds, and progress storage.

## Answer validation

Answer fields validate required values and accepted number formats in the browser, using the browser's native validation prompt. The table-selection form also requires at least one table. The server validates submitted values independently and displays an error message if a request bypasses browser validation.

## Progress and privacy

Training statistics are stored in `localStorage` in the current browser. They are not synchronized across browsers or devices and can be cleared on the training selection screen. Adaptive training sends the saved statistics to the app to select questions; the app does not persist them on the server. The active round is held temporarily in the ASP.NET session.

## Build and test

The test project is separate from `MultiplicationGame.sln`; target it directly to include the tests:

```powershell
dotnet restore .\tests\MultiplicationGame.Tests\MultiplicationGame.Tests.csproj
dotnet build .\tests\MultiplicationGame.Tests\MultiplicationGame.Tests.csproj --no-restore
dotnet test .\tests\MultiplicationGame.Tests\MultiplicationGame.Tests.csproj --no-build
```

See the [application guide](docs/application-guide.md) for the main user flows, architecture, API endpoints, configuration, and Mermaid diagrams.

## Developer notes

- Enable the repository git hooks once per clone with `git config core.hooksPath .githooks`. The pre-commit hook runs `dotnet format --verify-no-changes`.
- Run formatting locally with `dotnet format`.
- The scheduled workflow `.github/workflows/auto-format.yml` opens a pull request for weekly formatting fixes.
