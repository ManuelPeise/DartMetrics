# CLAUDE.md – DartMetrics

Arbeitsregeln für Claude (und alle anderen) in dieser Lösung.

- Was die Anwendung fachlich kann: [README.md](README.md)
- Projekte, Abhängigkeiten, **Service-Verzeichnis** und Endpunkte: [ARCHITECTURE.md](ARCHITECTURE.md)
- Vor Arbeiten an einem Projekt dessen `README.md` lesen.
- Neue Services, Controller oder Projekte immer im Projekt-README **und** in `ARCHITECTURE.md` eintragen.

## Grundsätze

- **Erst verstehen, dann ändern.** Vorhandene Konventionen, Basisklassen und Komponenten verwenden; keine parallelen Architekturen.
- **Bestehende Geschäftslogik nur nach Rücksprache ändern.** Auffällige Fehler melden, nicht stillschweigend „mitreparieren“.
- **Keine erfundenen Datenquellen oder APIs.** Fehlt ein Endpunkt, als Platzhalter kennzeichnen und nachfragen.
- **Rollenmodell ist fest:** `Admin`, `Player`, `GameMaster` (`UserRoleEnum`). Jeder Benutzer hat genau eine Rolle. Keine neuen Rollen, keine parallelen Berechtigungssysteme.
- **Sicherheit immer serverseitig.** Frontend-Prüfungen (`AuthorizeView`) ersetzen nie `[Authorize]` in der API.

## Sprache

- Code-Kommentare, UI-Texte, Dokumentation: **Deutsch**.
- Bezeichner (Klassen, Methoden, Variablen): **Englisch**.
- Commit-Nachrichten: **Englisch**, Conventional Commits (`feat(bot): …`, `fix(admin): …`, `refactor(…): …`).
- Fehlermeldungen aus Services (Exceptions) aktuell Englisch.

## Architektur und Abhängigkeitsrichtung

```
Web.App.Client (WASM)            Web.App (Blazor Server, BFF, /admin)
                                   └─ Web.Shared (Cookie-Auth, Sessions, API-Client, Admin-Komponenten)
Web.Core (API-Host) ─ Service.Api (Controller)
   └─ Logic.* (Fachlogik) ─ Logic.Shared (UnitOfWork, Repositories, Regeln, Logger)
        └─ Data.DartContext (EF Core, Entities, Migrationen)
Shared.Interfaces / Shared.Models / Shared.Enums (von allen genutzt)
```

- Controller enthalten keine Logik, nur Aufruf des Services.
- Logic-Projekte greifen nur über `IUnitOfWork` auf die Datenbank zu, nie direkt auf den `DbContext` (Ausnahme: `MessageLog`).
- Interfaces liegen in `Shared.Interfaces`, Transportmodelle in `Shared.Models`, Enums in `Shared.Enums` – jeweils im fachlichen Unterordner (`Bot`, `Training`, `Administration`, …).
- Das Web (Web.App/Web.Shared) spricht die Datenbank nie direkt an, immer über die API.

## Namenskonventionen

| Art | Muster | Beispiel |
|---|---|---|
| Entity | `…Entity : AEntityBase` | `DartBotGameEntity` |
| EF-Konfiguration | `…Configuration : AEntityBaseConfiguration<T>` | `DartBotGameConfiguration` |
| Enum | `…Enum` | `DartBotGameStatusEnum` |
| Service | Interface `I…` in `Shared.Interfaces`, Klasse in `Logic.*` | `IUserAdministration` / `UserAdministration` |
| DI-Registrierung | `DI/…ServiceRegistration.cs` mit `Add…Services()` | `AddBotServices()` |
| Controller | `…Controller : ApiControllerBase` | `DartBotGameController` |
| Admin-Komponente | `Admin…` | `AdminStatCard` |
| CSS (Admin) | Präfix `adm-`, Tokens `--adm-*` | `.adm-card` |

## Datenbank (Data.DartContext)

- Alle Entities erben von `AEntityBase` (`Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`). Audit-Felder setzt `UnitOfWork.SaveChangesAsync` – nie manuell.
- Konfiguration nur per Fluent API in `Configurations/<Bereich>/`; `ToTable("Plural")`.
- CHECK Constraints mit MySQL-Backticks; Wurf-Regeln aus `DartThrowConstraints` wiederverwenden.
- **Wurfkodierung:** Miss = `0 × 0`, Segmente `1–20 × 1–3`, Bull `25 × 1` (25) bzw. `25 × 2` (50). `Score = Segment × Multiplier`.
- Historische Daten: `DeleteBehavior.Restrict`; Bots werden deaktiviert, Benutzer anonymisiert (`DeletedAt`), nicht gelöscht.
- **Zeit:** immer UTC (`DateTime.UtcNow`). Bot-Entities nutzen `UseUtcDateTimes()`.
- Provider: **`MySql.EntityFrameworkCore` (Oracle)**, MySQL 8.4 – nicht Pomelo.
- Migrationen:
  ```
  dotnet ef migrations add <Name> --project Data.DartContext --startup-project Web.Core
  ```
  Die API wendet sie beim Start an (`Database.Migrate()`). Generierte Migration vor dem Commit prüfen.
- Ids bleiben `int`.

## Fachlogik (Logic.*)

- Services erben von `ALogicBase` (stellt `UnitOfWork`, `MessageLog`).
- Muster je öffentlicher Methode:
  ```csharp
  try { … await UnitOfWork.SaveChangesAsync(); }
  catch (Exception exception)
  {
      await MessageLog.LogMessage("Error …", nameof(MyService), exception);
      throw;
  }
  ```
- **Ein `SaveChangesAsync()` pro fachlichem Vorgang** (ist eine Transaktion).
- Getrackte Entities direkt ändern; **kein `repository.Update(graph)`** – markiert alle geladenen Navigationen als geändert.
- Unveränderlich: `DartBotProfileVersionEntity`, `DartBotModelEntity` (UnitOfWork wirft beim Ändern/Löschen).
- Exceptions bestimmen den HTTP-Status (`GlobalExceptionHandlerMiddleware`):
  `ArgumentException` → 400, `KeyNotFoundException` → 404, `UnauthorizedAccessException` → 403, sonst 500.
- Aktueller Benutzer über `IClaimsHelper.GetCurrentUserFromClaims()`.
- Dart-Regeln aus `Logic.Shared/Rules` (`X01Rules`, `DartRulesBase`) verwenden, nicht neu implementieren.

## API (Service.Api / Web.Core)

- Route: `api/[controller]/[action]`; Aktionen mit `[HttpGet(Name = "…")]` / `[HttpPost(Name = "…")]`.
- Admin-Endpunkte: `[Authorize(Roles = nameof(UserRoleEnum.Admin))]` auf Klassenebene.
- Rollen-Claim heißt `UserRoles` (`ClaimsHelper.UserRolesClaimType`), ist in der JWT-Konfiguration als `RoleClaimType` gesetzt.
- Neue Optionen: Klasse in `Shared.Models/Options`, Bindung in `Web.Core/Bundles/Services.cs` (`ApplyOptions`), mit Standardwerten.
- **Neues Projekt, das Web.Core referenziert → `COPY`-Zeile im Restore-Abschnitt von `Web.Core/Dockerfile` ergänzen**, sonst bricht der Docker-Build.

## Web (Web.App, Web.App.Client, Web.Shared)

- `/admin` läuft **Interactive Server** in `Web.App`; alles andere **Interactive WebAssembly** in `Web.App.Client`.
- Links über die Bereichsgrenze: `AreaLink` (Client) bzw. `<a data-enhance-nav="false">` – erzwingt vollen Seitenwechsel.
- API-Aufrufe vom Server: `IAuthorizedApiClient` (hängt das Token der Sitzung an). WASM ruft über den BFF-Proxy `/bff/api/**`.
- Daten in Admin-Seiten in `OnAfterRenderAsync(firstRender)` laden (sonst doppelt durch Prerendering).
- **Namespace-Falle:** In `Web.App`/`Web.Shared` immer `@using global::Shared.…` schreiben – `Shared.…` würde sonst als `Web.Shared.…` aufgelöst.
- Admin-UI:
  - Wiederverwendbare Komponenten und `admin.css` in `Web.Shared` (`Components/Admin`, `wwwroot/css`), Layout und Seiten in `Web.App`.
  - **Keine neuen Farben oder Schriften.** Nur `--adm-*`-Tokens, die auf die `--pl-*`-Palette aus `public.css` zeigen.
  - Seiten unter `Components/Pages/Admin` erben Layout und `[Authorize(Roles = "Admin")]` aus der dortigen `_Imports.razor`.
  - Zustände immer abdecken: Laden, Fehler (mit „Erneut versuchen“), leer. Fehlertexte der API über `ApiProblem.ReadMessageAsync`.
  - Responsiv: Desktop ≥ 1200 px, Tablet 768–1199 px, Smartphone < 768 px (Bottom Navigation, Tabellen als Karten über `data-label`).
- Keine ASP.NET-Core-2.x-Pakete; serverseitige Projekte nutzen `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

## DartBot – feste Entscheidungen

- Profil = unveränderliche Versionen; aktuell ist die höchste `VersionNumber`.
- Gemeinsame ML-Modelle für alle Bots; Profilwerte sind Eingabemerkmale.
- Modell lernt aus allen Spielen; **Profil, XP, Level und Elo ändern sich nur in abgeschlossenen Spielen gegen Menschen**.
- Lernschritt genau einmal (`LearningProcessedAt`), in derselben Transaktion wie die letzte Aufnahme.
- Ohne aktives Modell wählt der Bot Ziele regelbasiert (`RuleBasedTargetSelector`).
- Einstellungen: Abschnitt `"DartBot"` (`DartBotOptions`).
- Ausführlicher Plan: Claude-Doc „DartBot – Finaler Architektur- und Datenmodellplan“.

## Arbeitsablauf

- Branches: `feature/#<Nr>-<kurzbeschreibung>` (z. B. `feature/#07-implement-bot-ai`), Merge nach `master`.
- Vor dem Commit: Solution bauen (`dotnet build DartMetrics.slnx`), bei Datenmodelländerungen Migration anlegen und prüfen.
- Deployment der API: `scripts/update-containers.ps1` bzw. `.sh` (baut nur die API neu, Daten bleiben).

## Bekannte offene Punkte

Keine bekannten Fehler. Fachlich noch offen:

- Benutzer haben noch kein eigenes Elo; Bots rechnen Menschen mit festem Wert (`HumanOpponentRating`).
- Dashboard: ML-Modelle und Trainingssessions ohne API (Platzhalter).
