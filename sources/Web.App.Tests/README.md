# Web.App.Tests

xUnit-Tests für die Handy-Eingabe in `Web.App/Remote` (ohne Datenbank, ohne laufenden Server).
Protokoll: [docs/remote-input-protocol.md](../../docs/remote-input-protocol.md).

| Test | Geprüft |
|---|---|
| `RemotePairingServiceTests` | Code einlösbar, nur einmal, läuft ab, unbekannt/leer/zu lang abgelehnt, Codes zufällig und eindeutig; Geräte-Token an Benutzer und Sitzung gebunden, Gerätename begrenzt, manipulierte oder fremde Token abgelehnt, Ablaufzeit |
| `RemoteDeviceAuthenticationHandlerTests` | Bearer-Token mit bestehender Sitzung → angemeldet; Sitzung beendet → abgelehnt; `access_token` nur auf `/hubs/remote`; ohne Token keine Anmeldung |
| `RemoteConnectionRegistryTests` | eine aktive Spielseite je Benutzer (neuer Tab ersetzt alten), Handys je Benutzer, Benutzer getrennt |
| `RemoteCommandValidationTests` | Hub nimmt nur gültige Darts an (Miss 0×0, 1–20 × 1–3, 25 × 1–2), Undo/Submit |

Interne Methoden (`RemoteInputHub.IsValid`) sind über `InternalsVisibleTo` in `Web.App.csproj` sichtbar.

Ausführen: `dotnet test Web.App.Tests`
