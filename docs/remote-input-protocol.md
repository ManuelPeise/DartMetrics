# Handy als Eingabegerät – Protokoll

Ein Handy wird an die geöffnete Spielseite im Browser gekoppelt und dient als Eingabegerät und
Spielstandsanzeige. Auf dem Handy läuft keine eigene App, sondern die Seite **`/remote`** der Web-App
(`Web.App.Client/Pages/Remote/RemoteControl.razor`, Layout `RemoteLayout`, gleiches Eingabefeld `DartInputPad`). Die Spielseite bleibt das „Gehirn“: Sie wertet die Tasten aus wie Eingaben auf ihrem
eigenen Eingabefeld und schickt dem Handy einen fertig aufbereiteten Anzeigezustand. Das Handy kennt
**keine Spielregeln**.

```
Handy ──SendCommand (Dart / Undo / Submit)──▶ Hub /hubs/remote (Web.App) ──Command──▶ Spielseite (Browser)
      ◀─────────────── State (RemoteDisplayState) ◀── PublishState ────────────────────┘
```

Modelle: `Shared.Models/Remote/RemoteInputModels.cs`, Enum `Shared.Enums/Remote/RemoteCommandTypeEnum.cs`.

## 1. Kopplung (pro Anmeldung, ohne Datenbank)

1. Spielseite → „Handy verbinden“ → `POST /bff/remote/pairing` (Cookie + `X-CSRF`).
   Antwort `RemotePairingInfo`: einmaliger Code (Standard 120 s gültig) und QR-Code mit der Adresse
   `https://<host>/remote?code=<code>`. Der QR-Code enthält **keinen Token und keine Sitzungsdaten**.
2. Die Kamera des Handys öffnet die Adresse; die Seite `/remote` entfernt den Code aus der Adresszeile und ruft auf:
   ```http
   POST <server>/remote/pair
   Content-Type: application/json

   { "code": "<code>", "deviceName": "Pixel 8" }
   ```
   Antwort 200 `RemotePairResponse` (Token nur im `sessionStorage` dieses Tabs, `js/remote.js`):
   ```json
   { "deviceToken": "…", "expiresAtUtc": "2026-11-18T02:00:00Z", "hubPath": "/hubs/remote", "userName": "max@example.org" }
   ```
   400 bei unbekanntem, abgelaufenem oder schon benutztem Code; 429 bei zu vielen Versuchen (Rate Limit wie Login).
3. Der Geräte-Token gilt höchstens `RemoteInput:DeviceTokenHours` (Standard 12 h) **und nur, solange die
   Browser-Anmeldung besteht**. Abmelden im Browser trennt das Handy. Danach neu scannen.

## 2. Hub-Verbindung (SignalR, JSON-Protokoll)

- URL: `<server>/hubs/remote`, Token als `Authorization: Bearer <deviceToken>` oder Query `access_token=<deviceToken>`
  (.NET-Client: `options.AccessTokenProvider`).
- Nach dem Verbinden: `RegisterController()` aufrufen (auch nach jedem Reconnect).
- Die Seite hält den Bildschirm wach (Screen Wake Lock, Chrome / Safari ab 16.4) und vibriert kurz beim Senden (nur Android).

### Handy → Hub

| Methode | Argument | Bedeutung |
|---|---|---|
| `RegisterController` | – | Als Eingabegerät anmelden; die Spielseite schickt daraufhin sofort `State` |
| `SendCommand` | `RemoteCommand` | `{ "type": 1, "segment": 20, "multiplier": 3 }` = T20 |

`type`: `1` Dart, `2` Undo (letzten noch nicht gesendeten Dart zurücknehmen), `3` Submit (Aufnahme senden).
Kodierung der Darts: Miss `0 × 0`, Segmente `1–20 × 1–3`, `25 × 1` (25), `25 × 2` (Bull). Ungültige Befehle → Fehler.

### Hub → Handy

| Ereignis | Argument | Bedeutung |
|---|---|---|
| `State` | `RemoteDisplayState` | Anzeigen, wie er ist (s. u.) |
| `DisplayGone` | – | Keine Spielseite offen bzw. Seite verlassen: Hinweis „Spiel im Browser öffnen“ |

### `RemoteDisplayState`

| Feld | Anzeige |
|---|---|
| `title`, `subtitle` | Kopfzeile |
| `players[]` (`name`, `value`, `detail`, `isActive`) | Spielstand; `value` = Rest bzw. Ziel, aktiver Spieler hervorgehoben |
| `darts[]`, `visitTotal` | laufende Aufnahme (gewertete und noch nicht gesendete Darts) |
| `checkout` | Checkout-Weg, z. B. „T20 · T20 · Bull“ |
| `status`, `statusTone` | Statuszeile (`success` / `danger`) |
| `hint`, `hintTone` | Vorschau neben „Senden“ („Bust“, „Checkout!“) |
| `canInput` | Eingabe überhaupt möglich (eigener Wurf, kein Server-Aufruf läuft) |
| `inputLocked` | keine weiteren Darts (Aufnahme voll oder beendet) – Undo/Senden bleiben |
| `canUndo`, `canSubmit` | Tasten aktiv |
| `isFinished` | Spiel beendet |

Die Spielseite prüft dieselben Sperren noch einmal; ein gesperrter Befehl wird verworfen und der aktuelle Stand erneut geschickt.

## 3. Regeln im Hub

- Jeder Benutzer sieht nur seine eigenen Verbindungen.
- Pro Benutzer genau **eine aktive Spielseite** (die zuletzt geöffnete); Eingaben gehen nur dorthin.
- Cookie-Verbindungen dürfen nur Anzeige-Methoden aufrufen (mit Origin-Prüfung), Geräte-Token nur Eingabe-Methoden.
- Zustand nur im Speicher des Web-Containers (eine Instanz). Nach einem Neustart verbinden sich beide Seiten neu.

## Konfiguration (`RemoteInput`)

| Schlüssel | Standard | Bedeutung |
|---|---|---|
| `PairingCodeSeconds` | 120 | Gültigkeit des QR-Codes |
| `DeviceTokenHours` | 12 | Höchstdauer des Geräte-Tokens |
| `PublicBaseUrl` | leer | Adresse im QR-Code; leer = aus der Anfrage (hinter Proxy `ForwardedHeaders:Enabled`) |
