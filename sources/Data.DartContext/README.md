# Data.DartContext

EF-Core-Datenzugriff (Provider `MySql.EntityFrameworkCore`, MySQL 8.4).

## Struktur

```
DartDatabaseContext            DbSets, ApplyConfigurationsFromAssembly
Entities/<Bereich>/            User, Training, Bot, Tournament, Log – alle erben von AEntityBase
Configurations/<Bereich>/      Fluent-Konfiguration je Entity (AEntityBaseConfiguration<T>)
  Training/DartThrowConstraints  Gemeinsame CHECK-Regeln für Würfe
  Bot/BotUtcDateTimeExtensions   UTC-Converter für Bot- und Turnier-Entities
Migrations/                    EF-Migrationen (beim API-Start angewendet)
Seeds/                         Rollen (HasData), Standard-Admin
```

## Regeln

- Neue Entity: Klasse in `Entities/<Bereich>`, Konfiguration in `Configurations/<Bereich>`, DbSet im Context, Migration.
- Beziehungen und Constraints nur per Fluent API; Tabellenname im Plural.
- Historische Daten mit `DeleteBehavior.Restrict`.
- Wurfkodierung: Miss `0×0`, `1–20 × 1–3`, Bull `25×1`/`25×2`.
- Zeitstempel in UTC.
- Migration anlegen: `dotnet ef migrations add <Name> --project Data.DartContext --startup-project Web.Core`.
