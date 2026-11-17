# Shared.Models

Transportmodelle zwischen API und Web sowie Optionsklassen, nach Bereichen geordnet:
`Administration`, `Authentication`, `Blazor`, `Bot`, `Email`, `Logger`, `Options`, `Shared`, `Training`, `User`.

- Nur Datenklassen, keine Logik. Einzige Ausnahme: `Shared/CheckoutAdvisor` (beste Checkout-Wege für Menschen), weil Server und Browser (Bot-Partie) dieselbe Berechnung brauchen; reine Berechnung ohne Abhängigkeiten, gecacht. Ebenso `Shared/X01VisitPreview`: Vorschau einer Aufnahme im Browser (Bust, Checkout, Double-In) vor dem Senden; rechnet wie `X01Rules`, Abgleich in `Logic.Bot.Tests/Rules/X01VisitPreviewTests`.
- `Options/`: an Konfigurationsabschnitte gebunden (z. B. `DartBotOptions` ↔ `"DartBot"`, `TournamentOptions` ↔ `"Tournament"`, `DataRetentionOptions` ↔ `"DataRetention"`), immer mit sinnvollen Standardwerten.
- Im Web mit `global::Shared.Models…` einbinden.
