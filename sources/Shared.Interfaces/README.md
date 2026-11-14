# Shared.Interfaces

Interfaces aller Services und der Datenzugriffs-Schicht, nach Bereichen geordnet:
`Administration`, `Authentication`, `Blazor`, `Bot`, `Email`, `Logger`, `Repositories`, `Training`, `UnitsOfWork`, `User`.

- Implementierungen liegen in `Logic.*` bzw. `Web.Shared`.
- Referenziert `Data.DartContext` (Repository- und UnitOfWork-Interfaces arbeiten mit Entities).
