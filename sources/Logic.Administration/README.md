# Logic.Administration

Funktionen für Administratoren.

- `LogService`: System-Logs lesen und löschen.
- `UserAdministration`: Benutzer auflisten, Rolle ändern (nur Player ↔ GameMaster), (de)aktivieren, löschen.
  - Eigenes Konto und Admin-Konten sind gesperrt.
  - Löschen = Anonymisieren über `IUserAnonymizer` (Logic.Shared, gemeinsam mit „Konto selbst löschen“): `DeletedAt`, Login-Daten entfernt, Bot-Spiele und Trainings bleiben; Turniere: vor dem Start abgemeldet, sonst „Gelöschter Spieler“ und offene Matches gewinnt der Gegner kampflos.
  - Aktionen werden als Info im System-Log protokolliert.
- `DashboardStatisticsService`: Zählwerte fürs Dashboard (ML-Modelle gesamt/aktiv, Trainingssessions gesamt/letzte 7 Tage). API: `Dashboard/GetDashboardStatistics`, nur Admin.
- DI: `AddAdministrationServices()`.
