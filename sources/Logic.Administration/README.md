# Logic.Administration

Funktionen für Administratoren.

- `LogService`: System-Logs lesen und löschen.
- `UserAdministration`: Benutzer auflisten, Rolle ändern (nur Player ↔ GameMaster; die Seite fragt vorher nach und erklärt die Folgen: eine Rolle pro Benutzer, ein GameMaster spielt nicht selbst), (de)aktivieren, löschen.
  - Eigenes Konto und Admin-Konten sind gesperrt.
  - Löschen = Anonymisieren über `IUserAnonymizer` (Logic.Shared, gemeinsam mit „Konto selbst löschen“): `DeletedAt`, Login-Daten entfernt, Bot-Spiele und Trainings bleiben; Turniere: vor dem Start abgemeldet, sonst „Gelöschter Spieler“ und offene Matches gewinnt der Gegner kampflos.
  - Aktionen werden als Info im System-Log protokolliert.
- `DashboardStatisticsService`: Zählwerte fürs Dashboard (Trainingssessions gesamt/letzte 7 Tage). API: `Dashboard/GetDashboardStatistics`, nur Admin.
- `DataRetention/DataRetentionWorker` (Hintergrunddienst): Löschfristen nach `DataRetentionOptions` – unbestätigte Registrierungen nach 7 Tagen (ohne Spieldaten), MessageLog-Einträge nach 90 Tagen; läuft 1 Minute nach dem Start und dann alle 24 Stunden. `RunOnceAsync` ist öffentlich und direkt testbar. Die Fristen stehen so auch in der Datenschutzerklärung (`Legal__*` in der Web-App).
- DI: `AddAdministrationServices()` (inkl. `AddHostedService<DataRetentionWorker>`).
