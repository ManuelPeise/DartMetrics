# Web.Shared

Server-seitige Bibliothek für das Web (Razor Class Library). Wird nur von `Web.App` verwendet.

## Inhalt

```
DI/WebServiceRegistration    RegisterWebServices(): Sitzungen, Token, API-Clients
Services/                    BlazorAuthenticationService (Login/Logout, Cookie), TokenRefreshService,
                             ApiHttpClientService, AuthorizedApiClient (API-Aufrufe mit Token der Sitzung)
Stores/BlazorSessionStore    Sitzungen (Token) serverseitig, geschützt mit DataProtection
Constants/BlazorClaimTypes   Claim-Namen der Cookie-Identität
Components/Admin/            Admin-Komponenten (Admin…), ApiProblem
wwwroot/css/admin.css        Admin-Design: --adm-*-Tokens auf Basis der --pl-*-Palette
```

## Admin-Komponenten

`AdminCard`, `AdminStatCard`, `AdminTable<TItem>`, `AdminButton`, `AdminInput`, `AdminSelect`, `AdminStatusBadge`,
`AdminPageHeader`, `AdminBreadcrumb`, `AdminIcon`, `AdminEmptyState`, `AdminLoadingState`, `AdminErrorState`,
`AdminAccessDenied`, `AdminSkillBar`. Namespace `Web.Shared.Components.Admin`.

- Stylesheet wird als `_content/Web.Shared/css/admin.css` eingebunden.
- Keine Abhängigkeit zu `Web.App.Client` (z. B. kein `AreaLink`).
- Zustände: Default, Hover, Active, Disabled, Loading, Error.

## Abhängigkeiten

`Microsoft.AspNetCore.App` (Framework-Referenz), `System.IdentityModel.Tokens.Jwt`, `Shared.Interfaces`.
