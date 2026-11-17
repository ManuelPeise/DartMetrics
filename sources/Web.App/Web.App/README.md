# Web.App

Blazor-Host (Server). Liefert die WebAssembly-App aus, betreibt das BFF und den Admin-Bereich.

## Aufgaben

- **Cookie-Anmeldung** (`Bundles/ServiceRegistration.cs`): Cookie `__Host-DartMetrics.Auth`, Token bleiben serverseitig in der Sitzung.
- **BFF** (`Bundles/BffEndpoints.cs`): `/bff/user`, `/bff/login`, `/bff/logout`, `/bff/register`, `/bff/verify-email`, `/bff/resend-verification`, `/bff/forgot-password`, `/bff/reset-password` und Proxy `/bff/api/**` → API. CSRF-Header-Pflicht für alle `/bff`-Aufrufe.
- **Handy als Eingabegerät** (`Remote/`): `POST /bff/remote/pairing` (QR-Code), `POST /remote/pair` (Code gegen Geräte-Token, Rate Limit),
  SignalR-Hub `/hubs/remote` (Spielseite per Cookie, Handy per Geräte-Token, Schema `RemoteDevice`). Keine Spiellogik, keine Datenbank;
  Token per Data Protection, an die Browser-Sitzung gebunden. Protokoll: [docs/remote-input-protocol.md](../../../docs/remote-input-protocol.md).
- **Rate Limiting** (`Bundles/ServiceRegistration.cs`): anonyme Auth-Endpunkte 10 Anfragen/Minute je IP (`RateLimiting:Auth:*`), Antwort 429.
- **Render-Modi** (`Components/App.razor`): `/admin` Interactive Server, `/Error`, `/impressum`, `/datenschutz` statisch (static SSR), alles andere Interactive WebAssembly (ohne Prerendering).
- **Impressum / Datenschutz** (`Components/Pages/Legal`): Angaben aus `LegalOptions` (Abschnitt `Legal`, in Docker `LEGAL_*` aus der `.env`), nie aus dem Repository.
- **Reverse Proxy:** `ForwardedHeaders:Enabled=true` übernimmt Schema und Client-IP vom Proxy (für `__Host-`-Cookie und Rate Limiting).
- **Admin-Bereich** unter `/admin`.
- **Docker:** `Dockerfile` (HTTPS mit Zertifikat aus `./certs`, Data-Protection-Schlüssel im Volume `web_dataprotection`).

## Struktur

```
Components/
  App.razor                 Render-Modus je Pfad, Stylesheets
  Layout/Admin/             AdminLayout, AdminSidebar, AdminHeader, AdminBottomNav, AdminNavigation (Menü-Definition)
  Pages/Admin/              Dashboard, Benutzer, Bots, Logs, Passwort; Statistiken/Einstellungen sind
                            Platzhalter und für V1 aus der Navigation ausgeblendet
    _Imports.razor          @layout AdminLayout + [Authorize(Roles = "Admin")] für alle Admin-Seiten
  Pages/Legal/              Impressum, Datenschutz (static SSR, PublicLayout)
Bundles/                    ServiceRegistration (Cookie, Rate Limiting, Legal), BffEndpoints, LegalOptions
wwwroot/css/public.css      Farbpalette (--pl-*), Typografie, Impressum/Datenschutz
wwwroot/css/fonts.css       Schriften lokal (@font-face, kein Google Fonts); Dateien in wwwroot/fonts (SIL OFL)
wwwroot/img/                Bilder (Pixabay-Lizenz, siehe THIRD-PARTY-NOTICES.md)
```

Wiederverwendbare Admin-Komponenten und `admin.css` liegen in [Web.Shared](../../Web.Shared/README.md).

## Regeln

- Neue Admin-Seite: Datei in `Components/Pages/Admin`, Menüpunkt in `Layout/Admin/AdminNavigation.cs`.
- Daten über `IAuthorizedApiClient` laden, in `OnAfterRenderAsync(firstRender)`.
- `@using global::Shared.…` statt `@using Shared.…` (sonst `Web.Shared.…`).
- Nur `--adm-*`/`--pl-*`-Farben verwenden.

## Abhängigkeiten

`Web.App.Client`, `Web.Shared`, `Shared.Interfaces`, `Shared.Models`.
