# Web.App.Client

Blazor WebAssembly: öffentliche Seiten und Spieler-Bereich (`/player`).

## Struktur

```
Authentication/       BffAuthenticationStateProvider (Anmeldestatus über /bff/user), Formular-Modelle
Layout/               MainLayout (Fallback), PublicLayout (öffentliche Seiten), LoginDisplay, AreaLink
Layout/Player/        PlayerLayout, PlayerTopBar, PlayerBottomNav, PlayerNavigation (Menü + Rollen)
Components/Player/    Bausteine des Spieler-Bereichs: PlayerCard, PlayerPageHeader, PlayerIcon,
                      PlayerEmptyState, PlayerLoadingState, PlayerErrorState,
                      DartInputPad + DartThrowInput (Wurfeingabe für Training und Bot-Spiel)
Pages/                Home, Login, Logout, Register, VerifyEmail, AccessDenied, NotFound
Pages/Player/         Start, Training, Gegen Bot spielen, Statistiken, Profil
  _Imports.razor      @layout PlayerLayout + [Authorize(Roles = "Player,GameMaster")]
Routing/AppAreas      Grenze zwischen WASM und Server-Bereich (/admin)
Services/             AccountService (Login, Registrierung, E-Mail-Bestätigung, Logout über /bff),
                      ApiClient (API über den BFF-Proxy /bff/api/**), BffHttpClient + CsrfHeaderHandler
Services/Api/         Typisierte API-Clients je Fachbereich (IProfileApi, IBotGameApi), ApiResult<T>
```

Styles: `public.css` (Palette `--pl-*`, Buttons, Formulare) und `player.css` (Spieler-Bereich, Präfix `pa-`), beide in `Web.App/wwwroot/css`.

## Regeln

- Keine Server-Abhängigkeiten (läuft im Browser). `Shared.Models` ist referenziert (reine Datenklassen), `Shared.Interfaces` nicht.
- Seiten rufen die API **nur über typisierte Clients** in `Services/Api` auf; URLs und Fehlerauswertung stehen dort. Ergebnis ist immer `ApiResult<T>` (Wert oder Fehlertext).
- Alle Aufrufe laufen über das BFF (`/bff/api/**`), nie direkt an die API.
- `@using global::Shared.…` statt `@using Shared.…` (sonst `Web.App.Client.Pages.Shared`).
- Neue Spieler-Seite: Datei in `Pages/Player`, Menüpunkt in `Layout/Player/PlayerNavigation.cs`.
- Nach dem Login: Admins → `/admin`, Player/GameMaster → `/player` (sofern keine andere Seite angefordert war).
- Links in den Admin-Bereich mit `AreaLink` (voller Seitenwechsel).
- Responsiv: Desktop Top-Leiste mit Text, Tablet (< 1024 px) nur Icons, Smartphone (< 768 px) Bottom Navigation.

## Wurfeingabe (`DartInputPad`)

- Gibt jeden Dart einzeln über `OnDart` (`DartThrowInput`: Segment, Multiplier) aus; Kodierung wie im Backend.
- Double/Triple gilt nur für den nächsten Dart. 25 ist Single, Bull = 25 × 2, kein Triple auf 25.
- Kennt **keine Spielregeln**: Bust, Checkout und Aufnahmeende wertet die Seite aus.
- Parameter: `CurrentDarts` (Anzeige der laufenden Aufnahme), `OnUndo`/`CanUndo` (optionale Rückgängig-Taste), `Disabled`, `ShowVisit`.
