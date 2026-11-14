# Web.Core

Host der REST-API (Startprojekt für API und Migrationen).

## Struktur

```
Program.cs                              AddServices() / ConfigureApp()
Bundles/Services.cs                     Optionen, Datenbank, JWT, Controller, OpenAPI, DI aller Logic-Projekte
Bundles/AppConfiguration.cs             Middleware-Pipeline, Swagger (Entwicklung), Datenbank-Initialisierung
Bundles/DatabaseInitialization.cs       Database.Migrate() + Admin-Seed beim Start
Bundles/BearerSecuritySchemeTransformer Swagger "Authorize"-Button
MiddleWare/GlobalExceptionHandler…      Exceptions → ProblemDetails (400/403/404/500)
Dockerfile                              Image der API
```

## Konfiguration

Abschnitte: `ConnectionStrings:DartContext`, `Jwt`, `AdminUser`, `Email`, `Server`, `DartBot` (optional, Standardwerte in `DartBotOptions`).
In Docker per Umgebungsvariablen aus `docker-compose.yml` / `.env`.

## Regeln

- Neues Logic-Projekt: Referenz hier, `Add…Services()` in `AddDiServices`, **`COPY`-Zeile im `Dockerfile`** ergänzen.
- Neue Optionen in `ApplyOptions` binden (`ValidateDataAnnotations`, `ValidateOnStart`).
- Migrationen: `dotnet ef migrations add <Name> --project Data.DartContext --startup-project Web.Core`.
