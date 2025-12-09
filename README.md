# MultiplicationGame

Small ASP.NET Razor Pages app used to practice multiplication. CI and tests are configured in `.github/workflows`.

Local developer notes:

- Enable the repository git hooks included in the repo (recommended):

	- Run `git config core.hooksPath .githooks` once per clone to enable the included pre-commit hooks.
	- The hooks will run `dotnet format --verify-no-changes` and prevent commits when formatting changes are needed.

- To run formatting locally:

	- Install dotnet-format: `dotnet tool install -g dotnet-format --version 6.0.256901`
	- Run: `dotnet format`

CI auto-formatting:

- A scheduled GitHub Action (`.github/workflows/auto-format.yml`) runs weekly and will create a PR with formatting fixes when needed.

# MultiplicationGame

![.NET Tests](https://github.com/MichalB136/MultiplicationGame/actions/workflows/dotnet-test.yml/badge.svg?branch=main)

Simple multiplication practice web app built with ASP.NET Core Razor Pages.

Quick start

```powershell
# restore
dotnet restore MultiplicationGame.sln
# build
dotnet build MultiplicationGame.sln
# run tests
dotnet test MultiplicationGame.sln
```
