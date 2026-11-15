# Web.Core

Host der REST-API (Startprojekt für API und Migrationen).

## Struktur

```
Program.cs                              AddServices() / ConfigureApp()
Bundles/Services.cs                     Optionen, Datenbank, JWT, Controller, OpenAPI, DI aller Logic-Projekte
Bundles/AppConfiguration.cs             Middleware-Pipeline, Swagger (Entwicklung), Datenbank-Initialisierung
Bundles/DatabaseInitialization.cs       Database.Migrate() + Admin-Seed beim Start
Bundles/BearerSecuritySchemeTransformer Swagger "Authorize"-Button
MiddleWare/GlobalExceptionHandler…      Exceptions → ProblemDetails (400/403/404/409/500)
MiddleWare/MustChangePasswordMiddleware Einmal-Passwort: bis zur Änderung nur Profil/Passwort/An-/Abmelden
Dockerfile                              Image der API (kopiert Directory.Build.props / Directory.Packages.props vor dem Restore)
```

## Konfiguration

Abschnitte: `ConnectionStrings:DartContext`, `Jwt`, `AdminUser`, `Email`, `Server`; optional mit Standardwerten: `DartBot` (`DartBotOptions`), `Tournament` (`TournamentOptions`), `DataRetention` (`DataRetentionOptions`: Löschfristen).
In Docker per Umgebungsvariablen aus `docker-compose.yml` / `.env` (alle Variablen: `.env.example`). Standard-Umgebung im Container ist `Production` (Swagger aus); `docker-compose.override.yml` setzt für die Entwicklung `Development`.

## Regeln

- Neues Logic-Projekt: Referenz hier, `Add…Services()` in `AddDiServices`, **`COPY`-Zeile im `Dockerfile`** ergänzen.
- Paketversionen nur in `Directory.Packages.props` (Central Package Management); EF Core auf dem Stand von `MySql.EntityFrameworkCore` halten.
- Neue Optionen in `ApplyOptions` binden (`ValidateDataAnnotations`, `ValidateOnStart`).
- Migrationen: `dotnet ef migrations add <Name> --project Data.DartContext --startup-project Web.Core`.
