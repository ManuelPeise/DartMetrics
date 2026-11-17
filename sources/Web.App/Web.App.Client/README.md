# Web.App.Client

Blazor WebAssembly: öffentliche Seiten und Spieler-Bereich (`/player`).

## Struktur

```
Authentication/       BffAuthenticationStateProvider (Anmeldestatus über /bff/user), Formular-Modelle
Layout/               MainLayout (Fallback), PublicLayout (öffentliche Seiten), LoginDisplay, AreaLink
Layout/Player/        PlayerLayout, PlayerTopBar, PlayerBottomNav, PlayerNavigation (Menü + Rollen)
Components/Player/    Bausteine des Spieler-Bereichs: PlayerCard, PlayerPageHeader, PlayerIcon,
                      PlayerEmptyState, PlayerLoadingState, PlayerErrorState,
                      DartInputPad + DartThrowInput (Wurfeingabe für Training und Bot-Spiel),
                      PlayerChoiceGroup, PlayerScoreboard, PlayerStatGrid, CheckoutHint
Pages/                Home, Login (mit „Passwort vergessen“ und „Bestätigungsmail erneut senden“), Logout, Register, VerifyEmail, ForgotPassword (/password/forgot), ResetPassword (/password/reset), AccessDenied, NotFound
Pages/Player/         Start, Training (Auswahl), Gegen Bot spielen (Gegnerwahl → Spiel → Ergebnis), Statistiken (Zeitraum, Kennzahlen je Trainingsart, Verlauf), Profil (Elo + Verlauf, Daten, Passwort, Konto löschen), Turniere (Liste, Detail mit Teilnehmern/Spielplan/Tabelle, Match-Eingabe)
  Training/           X01, Around the Clock, Checkout, Segment (Einstellung → Spiel → Ergebnis; offenes Training wird beim Öffnen fortgesetzt)
  _Imports.razor      @layout PlayerLayout + [Authorize(Roles = "Player,GameMaster")]
Routing/AppAreas      Grenze zwischen WASM und Server: /admin (Interactive Server), /impressum und /datenschutz
                      (static SSR); RequiresFullPageLoad für Links dorthin
Services/             AccountService (Login, Registrierung, E-Mail-Bestätigung, Logout über /bff),
                      ApiClient (API über den BFF-Proxy /bff/api/**), BffHttpClient + CsrfHeaderHandler
Services/Api/         Typisierte API-Clients je Fachbereich (IProfileApi, IBotGameApi, ITrainingApi, IStatisticsApi,
                      ITournamentApi), ApiResult<T>
```

Styles: `public.css` (Palette `--pl-*`, Buttons, Formulare) und `player.css` (Spieler-Bereich, Präfix `pa-`), beide in `Web.App/wwwroot/css`.

## Regeln

- Keine Server-Abhängigkeiten (läuft im Browser). `Shared.Models` ist referenziert (reine Datenklassen), `Shared.Interfaces` nicht.
- Seiten rufen die API **nur über typisierte Clients** in `Services/Api` auf; URLs und Fehlerauswertung stehen dort. Ergebnis ist immer `ApiResult<T>` (Wert oder Fehlertext).
- Alle Aufrufe laufen über das BFF (`/bff/api/**`), nie direkt an die API.
- `@using global::Shared.…` statt `@using Shared.…` (sonst `Web.App.Client.Pages.Shared`).
- Neue Spieler-Seite: Datei in `Pages/Player`, Menüpunkt in `Layout/Player/PlayerNavigation.cs`.
- Nach dem Login: Admins → `/admin`, Player/GameMaster → `/player` (sofern keine andere Seite angefordert war).
- Links in den Admin-Bereich und zu Impressum/Datenschutz mit `AreaLink` (voller Seitenwechsel; der WASM-Router kennt diese Seiten nicht).
- Login/Registrierung/Passwort-Seiten zeigen bei HTTP 429 (Rate Limiting) „Zu viele Versuche“.
- Responsiv: Desktop Top-Leiste mit Text, Tablet (< 1024 px) nur Icons, Smartphone (< 768 px) Bottom Navigation.

## Wurfeingabe (`DartInputPad`)

- Gibt jeden Dart einzeln über `OnDart` (`DartThrowInput`: Segment, Multiplier) aus; Kodierung wie im Backend.
- Double/Triple gilt nur für den nächsten Dart. 25 ist Single, Bull = 25 × 2, kein Triple auf 25.
- Kennt **keine Spielregeln**: Bust, Checkout und Aufnahmeende wertet die Seite aus.
- Parameter: `CurrentDarts` (Anzeige der laufenden Aufnahme), `OnUndo`/`CanUndo` (optionale Rückgängig-Taste), `Disabled`, `ShowVisit`.
- **Senden-Taste** mit `OnSubmit`/`CanSubmit`: Alle Spielseiten sammeln die Darts einer Aufnahme im Browser und schicken sie erst
  mit „Senden“ ab, nie automatisch. `InputLocked` sperrt weitere Darts (3 Darts oder Aufnahme laut Vorschau zu Ende),
  `SubmitHint`/`SubmitHintTone` zeigen das Ergebnis der Vorschau („Bust“, „Checkout!“).
- Vorschau für X01-Seiten (Training, Checkout-Training, Bot-Spiel, Turnier): `X01VisitPreview` aus `Shared.Models` – erkennt
  Bust/Checkout sofort, gewertet wird weiter auf dem Server. Trainings und Turnier senden die gesammelten Darts nacheinander an die
  bestehenden Dart-Endpunkte; beendet der Server die Aufnahme früher, werden übrige Darts verworfen und gemeldet.
- Noch nicht gesendete Darts gehen beim Neuladen der Seite verloren (der Server kennt sie nicht).

## Handy als Eingabegerät (`RemoteInputBridge`)

- Steht auf jeder Spielseite unter dem `DartInputPad`: „Handy verbinden“ zeigt den QR-Code (`RemoteInputConnection.CreatePairingAsync`).
- Tasten vom Handy rufen dieselben Callbacks wie das Eingabefeld (`OnDart`, `OnUndo`, `OnSubmit`); Sperren kommen aus `State`.
- `State` (`RemoteDisplayState`) baut jede Seite in `RemoteDisplay` aus ihren eigenen Feldern; geschickt wird nur bei Änderung.
- Neue Spielseite: Bridge einbauen und `RemoteDisplay` füllen – die Handy-Seite braucht dafür keine Änderung.
- Handy-Seite `/remote` (`Pages/Remote/RemoteControl`, `RemoteLayout`, ohne Anmeldung): löst den Code aus dem QR-Code ein,
  verbindet sich mit dem Geräte-Token, zeigt `State` und nutzt dasselbe `DartInputPad`; Wake Lock und Vibration über `js/remote.js`.
  Protokoll: [docs/remote-input-protocol.md](../../../docs/remote-input-protocol.md).

## Bot-Spiel (`/player/bots`)

- Beim Öffnen wird ein laufendes Spiel fortgesetzt (`GetRunningDartBotGame`), sonst Gegnerwahl und Einstellungen.
- Die API erwartet die **ganze Aufnahme**: Darts werden im Browser gesammelt (lokal rückgängig machbar) und mit „Senden“ abgeschickt.
- Der Rest während der Eingabe ist eine Vorschau (`X01VisitPreview`, inkl. Double-In, Bust, Checkout); gewertet wird auf dem Server.
- Die Antwort enthält bereits die Bot-Aufnahme(n); sie werden Dart für Dart mit Zielfeld angezeigt.
- Checkout-Hinweis nach jedem eingegebenen Dart, im Browser berechnet (`CheckoutAdvisor` aus `Shared.Models`), ab dem Eröffnen.

## Checkout-Hinweis (`CheckoutHint`)

Zeigt den besten Weg aus `CheckoutSuggestions` (z. B. „T20 · T20 · Bull“). In Trainings und Turnier-Matches liefert der Server die Wege nach jedem Dart, in der Bot-Partie rechnet die Seite selbst.

## Turniere (`/player/tournaments`)

- `Tournaments/PlayerTournaments`: Liste (offizielle + eigene private) und Anlegen; GameMaster legt offizielle, Spieler private Turniere an (Liga oder K.o., 301/501, In/Out, K.o. optional mit Setzliste aus einer beendeten Liga).
- `Tournaments/PlayerTournamentDetail` (`/player/tournaments/{league|knockout}/{id}`): Teilnehmer hinzufügen/entfernen, Start (Satzlänge bzw. je Runde), Termine je Runde (Ortszeit → UTC), Spielplan bzw. Baum, Liga-Tabelle, „Match leiten“ für Matches mit Menschen (Bot gegen Bot startet der Server zum Termin).
- `Tournaments/PlayerTournamentMatch`: Eingabe-Screen – links Tafel (Handschrift `--pl-font-chalk`), rechts oben Spielstand und Finish-Weg, rechts unten `DartInputPad` mit Korrektur; jeder Aufruf mit `ConcurrencyStamp`, ohne Eingaberecht wird alle 3 s nachgeladen.
- API: `ITournamentApi` / `TournamentApi`. Menüpunkt „Turniere“ (Bottom Navigation jetzt 6 Spalten).
