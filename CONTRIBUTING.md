# Contributing to DartMetrics

Thanks for your interest! Bug reports, ideas and pull requests are all welcome.

## Ways to contribute

- **Report a bug** – use the *Bug report* issue template. Steps to reproduce help most.
- **Suggest a feature** – use the *Feature request* template and describe the problem you want to solve.
- **Improve the docs** – typos, unclear steps, missing explanations.
- **Write code** – pick an open issue (look for `good first issue`) or open one first for larger changes, so we can agree on the approach before you invest time.
- **Security issues** – please do **not** open a public issue, see [SECURITY.md](SECURITY.md).

## Development setup

Requirements: .NET SDK 10, Docker.

```bash
cd sources
cp .env.example .env                 # set the placeholders
./deploy/deploy.ps1                 # or ./deploy/deploy.sh
dotnet build DartMetrics.slnx
dotnet test DartMetrics.slnx         # integration tests need Docker (Testcontainers)
```

Run the web app outside Docker for debugging: `dotnet run --project Web.App/Web.App` (the API and database keep running in Docker).

Read these before changing code:

- [sources/ARCHITECTURE.md](sources/ARCHITECTURE.md) – projects, dependencies, services
- [sources/CLAUDE.md](sources/CLAUDE.md) – coding rules and conventions
- the `README.md` of the project you are changing

## Workflow

1. Fork the repository and create a branch from `Master`:
   - `feature/#<issue>-short-description` for features
   - `bug/#<issue>-short-description` for fixes
2. Keep the change focused: one topic per pull request.
3. Add or update tests:
   - business logic without database → unit tests (`Logic.Bot.Tests`, `Logic.Tournament.Tests`)
   - anything touching the database or several requests → `Logic.Integration.Tests`
4. Update the docs when behavior changes (project `README.md`, `ARCHITECTURE.md`, [user guide](docs/user-guide.md)).
5. Make sure `dotnet build` has no new warnings and `dotnet test` passes.
6. Open a pull request using the template. The GitHub Action builds, tests and builds the Docker images.

## Conventions

- **Language:** the UI is German. Code identifiers are English; existing comments and docs are mostly German – either language is fine in new comments, but stay consistent within a file.
- **Commits:** short summary line (imperative, max. ~72 characters), optional body explaining *why*; reference the issue (`#42`).
- **Database:** schema changes only via EF Core migrations (`dotnet ef migrations add <Name> --project Data.DartContext --startup-project Web.Core`).
- **Packages:** versions live in `sources/Directory.Packages.props` only.
- **No secrets or personal data** in the repository: configuration goes into `.env` (see `.env.example`).
- **No third-party requests** from the web app (fonts, scripts, analytics): DartMetrics is privacy by design.

## Licensing of contributions

By submitting a pull request you agree that your contribution is licensed under the [MIT License](LICENSE) of this project. Only add images, fonts or other assets you are allowed to redistribute, and list them in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Code of conduct

Be respectful and constructive. Harassment or personal attacks are not tolerated; maintainers may remove such content and block participants.
