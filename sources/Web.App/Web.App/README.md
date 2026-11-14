# Web.App

Blazor-Host (Server). Liefert die WebAssembly-App aus, betreibt das BFF und den Admin-Bereich.

## Aufgaben

- **Cookie-Anmeldung** (`Bundles/ServiceRegistration.cs`): Cookie `__Host-DartMetrics.Auth`, Token bleiben serverseitig in der Sitzung.
- **BFF** (`Bundles/BffEndpoints.cs`): `/bff/login`, `/bff/logout`, `/bff/register`, `/bff/user` und Proxy `/bff/api/**` → API.
- **Render-Modi** (`Components/App.razor`): `/admin` Interactive Server, alles andere Interactive WebAssembly (ohne Prerendering), `/Error` statisch.
- **Admin-Bereich** unter `/admin`.

## Struktur

```
Components/
  App.razor                 Render-Modus je Pfad, Stylesheets
  Layout/Admin/             AdminLayout, AdminSidebar, AdminHeader, AdminBottomNav, AdminNavigation (Menü-Definition)
  Pages/Admin/              Dashboard, Bots, Benutzer, Logs, Platzhalter-Seiten
    _Imports.razor          @layout AdminLayout + [Authorize(Roles = "Admin")] für alle Admin-Seiten
wwwroot/css/public.css      Farbpalette (--pl-*), Typografie
```

Wiederverwendbare Admin-Komponenten und `admin.css` liegen in [Web.Shared](../../Web.Shared/README.md).

## Regeln

- Neue Admin-Seite: Datei in `Components/Pages/Admin`, Menüpunkt in `Layout/Admin/AdminNavigation.cs`.
- Daten über `IAuthorizedApiClient` laden, in `OnAfterRenderAsync(firstRender)`.
- `@using global::Shared.…` statt `@using Shared.…` (sonst `Web.Shared.…`).
- Nur `--adm-*`/`--pl-*`-Farben verwenden.

## Abhängigkeiten

`Web.App.Client`, `Web.Shared`, `Shared.Interfaces`, `Shared.Models`.
