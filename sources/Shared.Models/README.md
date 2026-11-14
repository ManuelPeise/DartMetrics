# Shared.Models

Transportmodelle zwischen API und Web sowie Optionsklassen, nach Bereichen geordnet:
`Administration`, `Authentication`, `Blazor`, `Bot`, `Email`, `Logger`, `Options`, `Shared`, `Training`, `User`.

- Nur Datenklassen, keine Logik.
- `Options/`: an Konfigurationsabschnitte gebunden (z. B. `DartBotOptions` ↔ `"DartBot"`), immer mit sinnvollen Standardwerten.
- Im Web mit `global::Shared.Models…` einbinden.
