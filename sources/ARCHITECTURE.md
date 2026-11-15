# DartMetrics – Architektur und Service-Verzeichnis

Technische Gesamtübersicht. Was die Anwendung fachlich kann: [README.md](README.md). Regeln und Konventionen: [CLAUDE.md](CLAUDE.md).

## Projekte

| Ordner | Rolle | README |
|---|---|---|
| **Web** | | |
| `Web.App/Web.App` | Blazor-Host (Server): BFF, Cookie-Anmeldung, Admin-Bereich `/admin` | [README](Web.App/Web.App/README.md) |
| `Web.App/Web.App.Client` | Blazor WebAssembly: öffentliche Seiten, Login, Spieler-Bereich | [README](Web.App/Web.App.Client/README.md) |
| `Web.Shared` | Server-Bibliothek fürs Web: Sitzungen, Token, API-Client, Admin-Komponenten | [README](Web.Shared/README.md) |
| **API** | | |
| `Web.Core` | API-Host: DI, Authentifizierung, Middleware, Migrationen, Dockerfile | [README](Web.Core/README.md) |
| `Service.Api` | API-Controller | [README](Service.Api/README.md) |
| **Fachlogik** | | |
| `Logic.Shared` | UnitOfWork, Repositories, Dart-Regeln, Logger, E-Mail, Claims | [README](Logic.Shared/README.md) |
| `Logic.Authentication` | Login, Registrierung, JWT | [README](Logic.Authentication/README.md) |
| `Logic.Profile` | Benutzerprofil | [README](Logic.Profile/README.md) |
| `Logic.Training` | Trainings (Segment, Checkout, X01, Around the Clock) | [README](Logic.Training/README.md) |
| `Logic.Bot` | DartBots: Simulation, Zielwahl, Spiele, Lernen, Verwaltung, ML (Kandidaten, Rollouts, Hybrid-Zielwahl mit Modell, Modellverwaltung für Admins) | [README](Logic.Bot/README.md) |
| `Logic.Statistics` | Spieler-Statistik (nur lesend, über Trainings und Bot-Spiele) | [README](Logic.Statistics/README.md) |
| `Logic.Tournament` | Turniere (Liga, K.-o./WM): Datenmodell, X01-Match Dart für Dart, Liga (Spielplan, Tabelle), K.-o. (Baum, Setzliste, Weiterrücken) | [README](Logic.Tournament/README.md) |
| `Logic.Administration` | System-Logs, Benutzerverwaltung, Dashboard-Kennzahlen | [README](Logic.Administration/README.md) |
| **Werkzeuge** | | |
| `Tools.BotTrainer` | Konsole für ML: Datensätze erzeugen, trainieren, vergleichen, Modell aktivieren | [README](Tools.BotTrainer/README.md) |
| **Tests** | | |
| `Logic.Bot.Tests` | Unit-Tests der Bot-Logik (ohne Datenbank) | [README](Logic.Bot.Tests/README.md) |
| `Logic.Tournament.Tests` | Unit-Tests der Turnier-Logik (ohne Datenbank) | [README](Logic.Tournament.Tests/README.md) |
| `Logic.Integration.Tests` | Integrationstests gegen MySQL 8.4 (Testcontainers) | [README](Logic.Integration.Tests/README.md) |
| **Daten** | | |
| `Data.DartContext` | EF Core (MySQL): Entities, Konfigurationen, Migrationen, Seeds | [README](Data.DartContext/README.md) |
| **Gemeinsam** | | |
| `Shared.Interfaces` | Interfaces aller Services, UnitOfWork, Repositories | [README](Shared.Interfaces/README.md) |
| `Shared.Models` | Transportmodelle (API ↔ Web) und Optionen | [README](Shared.Models/README.md) |
| `Shared.Enums` | Enums | [README](Shared.Enums/README.md) |

## Abhängigkeiten

```
Web.App.Client (WASM)            Web.App (Blazor Server, BFF, /admin)
                                   └─ Web.Shared ─ Shared.Interfaces
Web.Core (API-Host) ─ Service.Api ─ Shared.Interfaces
   └─ Logic.Administration │ Logic.Authentication │ Logic.Bot │ Logic.Profile │ Logic.Statistics │ Logic.Tournament │ Logic.Training
        └─ Logic.Shared ─ Data.DartContext ─ Shared.Models ─ Shared.Enums
```

## Laufzeit

```
Browser ──► Web.App (Blazor, BFF, Cookie)  ──JWT──►  Web.Core / Service.Api  ──►  MySQL 8.4
                │                                         │
                ├─ /admin: Interactive Server             └─ Mailpit (SMTP, Entwicklung)
                └─ sonst:  WebAssembly (Web.App.Client) ──/bff/api/**──► Proxy in Web.App
```

## Service-Verzeichnis

Alle per DI registrierten Services. Lebensdauer: **S** = Scoped, **Si** = Singleton, **T** = Transient.

### API-Seite (registriert über `Web.Core/Bundles/Services.cs`)

| Interface | Implementierung | Projekt | DI | Aufgabe / Methoden |
|---|---|---|---|---|
| `IUnitOfWork` | `UnitOfWork` | [Logic.Shared](Logic.Shared/README.md) | S | Zugriff auf `UserUnitOfWork`, `DartTrainingUnitOfWork`, `DartBotUnitOfWork`, `TournamentUnitOfWork`; `SaveChangesAsync` (Audit-Felder, Schutz unveränderlicher Entities) |
| `IMessageLog` | `MessageLog` | [Logic.Shared](Logic.Shared/README.md) | S | `LogMessage`, `GetLogMessages`, `DeleteMessages` |
| `IClaimsHelper` | `ClaimsHelper` | [Logic.Shared](Logic.Shared/README.md) | S | `GetCurrentUserFromClaims`, `GetUserClaims`, `GetUserClaimsFromToken` |
| `IEmailClient` | `EmailClient` | [Logic.Shared](Logic.Shared/README.md) | S | `SendMail` |
| `IUserAuthentication` | `UserAuthentication` | [Logic.Authentication](Logic.Authentication/README.md) | S | `AuthenticateUser`, `RefreshToken`, `LogoutUser` |
| `IUserRegistration` | `UserRegistration` | [Logic.Authentication](Logic.Authentication/README.md) | S | `RegisterUser`, `VerifyEmail`, `ResendVerificationMail`, `RequestPasswordReset`, `ResetPassword` |
| `IUserProfileService` | `UserProfileService` | [Logic.Profile](Logic.Profile/README.md) | S | `GetProfileData`, `UpdateUserProfile`, `ChangePassword`, `DeleteOwnAccount`, `UploadAvatar`, `DeleteAvatar`, `GetAvatar` |
| `IUserAnonymizer` | `UserAnonymizer` | [Logic.Shared](Logic.Shared/README.md) | S | `AnonymizeAsync` (Kontolöschung durch Admin und selbst, inkl. Bot-Spiele und Turniere) |
| `ITournamentWithdrawal` | `TournamentWithdrawal` | [Logic.Tournament](Logic.Tournament/README.md) | S | `WithdrawDeletedUserAsync` (abmelden bzw. kampflos, für `UserAnonymizer`) |
| `ISegmentTraining` | `SegmentTraining` | [Logic.Training](Logic.Training/README.md) | S | `GetRunningTrainingAsync`, `StartTrainingAsync`, `AddThrowAsync`, `FinishTrainingAsync`, `GetResultAsync` |
| `ICheckoutTraining` | `CheckoutTraining` | [Logic.Training](Logic.Training/README.md) | S | wie oben |
| `IX01TrainingGame` | `X01TrainingGame` | [Logic.Training](Logic.Training/README.md) | S | wie oben |
| `IAroundTheClockTraining` | `AroundTheClockTraining` | [Logic.Training](Logic.Training/README.md) | S | wie oben |
| `IRandomSource` | `DefaultRandomSource` | [Logic.Bot](Logic.Bot/README.md) | Si | `NextDouble` (austauschbar für Tests) |
| `IThrowSimulator` | `ThrowSimulator` | [Logic.Bot](Logic.Bot/README.md) | Si | `DrawVisitForm`, `Throw` |
| `ITargetSelector` | `RuleBasedTargetSelector` | [Logic.Bot](Logic.Bot/README.md) | Si | `SelectTarget` |
| `IBotVisitPlayer` | `BotVisitPlayer` | [Logic.Bot](Logic.Bot/README.md) | Si | `PlayVisit` |
| `IDartBotAdministration` | `DartBotAdministration` | [Logic.Bot](Logic.Bot/README.md) | S | `CreateBotAsync`, `ActivateBotAsync`, `DeactivateBotAsync`, `GetBotsAsync`, `GetBotAsync` |
| `IDartBotGameService` | `DartBotGameService` | [Logic.Bot](Logic.Bot/README.md) | S | `StartGameAsync`, `AddUserVisitAsync`, `AbortGameAsync`, `GetGameAsync`, `GetRunningGameAsync`, `GetRatingHistoryAsync`, `GetAvailableBotsAsync` |
| `IDartBotLearningService` | `DartBotLearningService` | [Logic.Bot](Logic.Bot/README.md) | S | `ApplyGameResultAsync` (XP, Elo Bot + Spieler, Level), `ApplyTournamentResultAsync` (offizielle Turnier-Matches), `MergeConcurrentBotUpdatesAsync` |
| `IPlayerStatisticsService` | `PlayerStatisticsService` | [Logic.Statistics](Logic.Statistics/README.md) | S | `GetStatisticsAsync(period)` |
| `ILeagueService` | `LeagueService` | [Logic.Tournament](Logic.Tournament/README.md) | S | `CreateLeagueAsync`, `AddParticipantAsync`, `RemoveParticipantAsync`, `StartLeagueAsync`, `ScheduleRoundAsync`, `GetLeagueAsync`, `GetTableAsync`, `FinishLeagueIfCompleteAsync` |
| `IKnockoutService` | `KnockoutService` | [Logic.Tournament](Logic.Tournament/README.md) | S | `CreateKnockoutAsync`, `AddParticipantAsync`, `RemoveParticipantAsync`, `StartKnockoutAsync`, `ScheduleRoundAsync`, `GetKnockoutAsync`, `AdvanceAsync` |
| `ITournamentQueryService` | `TournamentQueryService` | [Logic.Tournament](Logic.Tournament/README.md) | S | `GetTournamentsAsync`, `GetCandidatesAsync` |
| `ITournamentRegistrationService` | `TournamentRegistrationService` | [Logic.Tournament](Logic.Tournament/README.md) | S | `OpenRegistrationAsync`, `RegisterAsync`, `WithdrawAsync`, `ConfirmParticipantAsync` |
| `ITournamentMatchService` | `TournamentMatchService` | [Logic.Tournament](Logic.Tournament/README.md) | S | `StartMatchAsync`, `AddDartAsync`, `UndoLastDartAsync`, `GetMatchStateAsync`, `PlayDueBotMatchesAsync` (Hintergrunddienst `BotMatchWorker`) |
| `ILogService` | `LogService` | [Logic.Administration](Logic.Administration/README.md) | S | `GetLogMessages`, `DeleteMessage`, `DeleteMessages` |
| `IUserAdministration` | `UserAdministration` | [Logic.Administration](Logic.Administration/README.md) | S | `GetUsersAsync`, `ChangeRoleAsync`, `ActivateUserAsync`, `DeactivateUserAsync`, `DeleteUserAsync` |
| `IDashboardStatisticsService` | `DashboardStatisticsService` | [Logic.Administration](Logic.Administration/README.md) | S | `GetStatisticsAsync` (Zählwerte fürs Admin-Dashboard) |

Repositories sind keine eigenen DI-Services; sie werden von den UnitOfWorks erzeugt (`RepositoryBase<TEntity>`).

### Web-Seite – Server (registriert über `Web.Shared/DI/WebServiceRegistration.cs`)

| Interface | Implementierung | Projekt | DI | Aufgabe / Methoden |
|---|---|---|---|---|
| `IBlazorAuthenticationService` | `BlazorAuthenticationService` | [Web.Shared](Web.Shared/README.md) | S | `LoginAsync`, `RefreshAsync`, `LogoutAsync` (Cookie + Sitzung) |
| `IApiHttpClientService` | `ApiHttpClientService` | [Web.Shared](Web.Shared/README.md) | HttpClient | `GetAsync`, `PostAsync` gegen die API |
| `ISessionStore` | `BlazorSessionStore` | [Web.Shared](Web.Shared/README.md) | S | `CreateAsync`, `GetAsync`, `UpdateAsync`, `DeleteAsync` (Token je Sitzung) |
| `ITokenRefreshService` | `TokenRefreshService` | [Web.Shared](Web.Shared/README.md) | S | `RefreshIfNeededAsync`, `ForceRefreshAsync` |
| `IAuthorizedApiClient` | `AuthorizedApiClient` | [Web.Shared](Web.Shared/README.md) | S | API-Aufrufe mit Token der Sitzung (`GetAsync`, `PostAsync`, `GetFromJsonAsync`, `PostAsJsonAsync`) |

### Web-Seite – WebAssembly (registriert in `Web.App/Web.App.Client/Program.cs`)

| Interface | Implementierung | Projekt | DI | Aufgabe |
|---|---|---|---|---|
| `IAccountService` | `AccountService` | [Web.App.Client](Web.App/Web.App.Client/README.md) | S | `LoginAsync`, `RegisterAsync`, `VerifyEmailAsync`, `LogoutAsync` über `/bff` |
| – | `ApiClient` | [Web.App.Client](Web.App/Web.App.Client/README.md) | S | API-Aufrufe über den BFF-Proxy `/bff/api/**` |
| `AuthenticationStateProvider` | `BffAuthenticationStateProvider` | [Web.App.Client](Web.App/Web.App.Client/README.md) | S | Anmeldestatus aus `/bff/user` |
| – | `CsrfHeaderHandler` | [Web.App.Client](Web.App/Web.App.Client/README.md) | T | Setzt den CSRF-Header für BFF-Aufrufe |
| `IProfileApi` | `ProfileApi` | [Web.App.Client](Web.App/Web.App.Client/README.md) | S | Profil laden/speichern, Passwort, Profilbild hoch-/entfernen |
| – | `PlayerProfileState` | [Web.App.Client](Web.App/Web.App.Client/README.md) | S | Profil für Kopfzeile und Profilseite, `Changed`-Ereignis |
| `IStatisticsApi` | `StatisticsApi` | [Web.App.Client](Web.App/Web.App.Client/README.md) | S | `GetStatisticsAsync(period)` (PlayerStatistics) |
| `ITrainingApi` | `TrainingApi` | [Web.App.Client](Web.App/Web.App.Client/README.md) | S | Offenes Training, Start, Dart, Beenden, Ergebnis für X01, Around the Clock, Checkout, Segment |
| `IBotGameApi` | `BotGameApi` | [Web.App.Client](Web.App/Web.App.Client/README.md) | S | Bots, laufendes Spiel, Start, Aufnahme, Abbruch, Spielstand, Elo-Verlauf (DartBotGameController) |

## Web-Bereiche

| Pfad | Bereich | Render-Modus | Zugriff | Layout |
|---|---|---|---|---|
| `/`, `/login`, `/register`, … | Öffentlich | WebAssembly | alle | `PublicLayout` |
| `/player/**` | Spieler-Bereich | WebAssembly | Player, GameMaster | `PlayerLayout` |
| `/admin/**` | Administration | Interactive Server | Admin | `AdminLayout` |

## API-Endpunkte (Überblick)

Details: [Service.Api](Service.Api/README.md). Route jeweils `api/<Controller>/<Action>`.

| Controller | Service | Zugriff |
|---|---|---|
| `UserAuthentication`, `Registration` | `IUserAuthentication`, `IUserRegistration` | anonym / angemeldet |
| `Profile` | `IUserProfileService` | angemeldet |
| `SegmentTraining`, `CheckoutTraining`, `X01TrainingGame`, `AroundTheClockTraining` | Trainings-Services | angemeldet |
| `DartBotGame` | `IDartBotGameService` | Player |
| `Tournament` | `ITournamentQueryService`, `ITournamentRegistrationService`, `ILeagueService`, `IKnockoutService`, `ITournamentMatchService` | GameMaster, Player (Rechte je Turnier im Service) |
| `PlayerStatistics` | `IPlayerStatisticsService` | Player |
| `DartBotAdministration` | `IDartBotAdministration` | Admin |
| `UserAdministration` | `IUserAdministration` | Admin |
| `MessageLog` | `ILogService` | Admin |
| `Dashboard` | `IDashboardStatisticsService` | Admin |
| `Health` | – | anonym |

## Weitere Dokumente

- DartBot-Architektur und Datenmodell: Claude-Doc „DartBot – Finaler Architektur- und Datenmodellplan“.
- Deployment: `docker-compose.yml`, `Web.Core/Dockerfile`, `scripts/update-containers.ps1` / `.sh`.

## Optimistische Nebenläufigkeit

Entitäten mit `IConcurrencyStamped` (aktuell `DartBotEntity`) erhalten bei jedem Speichern einen neuen `ConcurrencyStamp`.
Konflikte werden als `DbUpdateConcurrencyException` auf HTTP 409 abgebildet. Für Bots gibt es einen Delta-Merge mit einem Retry (siehe `Logic.Bot/README.md`).
