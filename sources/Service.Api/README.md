# Service.Api

API-Controller. Enthalten keine Logik, nur den Aufruf des passenden Services.

## Controller

| Bereich | Controller | Zugriff |
|---|---|---|
| Authentication | `UserAuthenticationController`, `RegistrationController` | anonym / angemeldet |
| Account | `ProfileController` | angemeldet |
| Training | `SegmentTraining…`, `CheckoutTraining…`, `X01TrainingGame…`, `AroundTheClockTraining…` | angemeldet |
| Bot | `DartBotGameController` (Spiele gegen Bots, Bot-Auswahl, Elo-Verlauf) | Player |
| Statistics | `PlayerStatisticsController` (eigene Statistik, `period`) | Player |
| Tournament | `TournamentController` (Übersicht, Anmeldung, Liga, K.o., Match-Eingabe) | GameMaster, Player (Rechte je Turnier im Service) |
| Administration | `DartBotAdministrationController`, `UserAdministrationController`, `MessageLogController`, `DashboardController` | `[Authorize(Roles = Admin)]` |
| Health | `HealthController` | anonym |

## Regeln

- Basisklasse `ApiControllerBase`, Route `api/[controller]/[action]`.
- `[HttpGet(Name = "…")]` / `[HttpPost(Name = "…")]`; Bodies mit `[FromBody]`, einfache Werte mit `[FromQuery]`.
- Admin-Controller: `[Authorize(Roles = nameof(UserRoleEnum.Admin))]` auf Klassenebene.
- Keine try/catch-Blöcke: Fehler behandelt die `GlobalExceptionHandlerMiddleware` in Web.Core.
- Die API ist nicht für den direkten Zugriff aus dem Browser gedacht: Die Web-App (BFF) ist der einzige Client; in der Produktion ist Port 8080 nur im Docker-Netz erreichbar. Rate Limiting für Login/Registrierung sitzt deshalb im BFF.
