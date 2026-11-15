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
| Administration | `DartBotAdministrationController`, `UserAdministrationController`, `MessageLogController`, `DashboardController` | `[Authorize(Roles = Admin)]` |
| Health | `HealthController` | anonym |

## Regeln

- Basisklasse `ApiControllerBase`, Route `api/[controller]/[action]`.
- `[HttpGet(Name = "…")]` / `[HttpPost(Name = "…")]`; Bodies mit `[FromBody]`, einfache Werte mit `[FromQuery]`.
- Admin-Controller: `[Authorize(Roles = nameof(UserRoleEnum.Admin))]` auf Klassenebene.
- Keine try/catch-Blöcke: Fehler behandelt die `GlobalExceptionHandlerMiddleware` in Web.Core.
