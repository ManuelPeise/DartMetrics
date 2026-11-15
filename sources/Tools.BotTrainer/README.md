# Tools.BotTrainer

Konsolenwerkzeug für die ML-Phasen der DartBots. Läuft lokal, nie in der API (Roadmap 3.5).

## generate (Phase 2)

Erzeugt einen Datensatz für das Zielwahl-Modell: zufällige Situationen (`SituationSampler`), je Situation alle Kandidaten mit Label aus N Rollouts (`Logic.Bot/Ml/CandidateEvaluator`).
Die Datei landet als CSV unter `ml_data/datasets/…`, die Metadaten (StorageKey, Zeilen, Größe, SHA-256, Seed, Parameter) in `DartBotTrainingDatasets`.

```powershell
dotnet run --project Tools.BotTrainer -c Release -- generate --situations 1000 --rollouts 50 --seed 1
```

**Datenbank:** das Werkzeug nimmt dieselbe Verbindung wie die API in der Entwicklung (`Web.Core/appsettings.Development.json`, Aufruf aus dem Lösungsordner). Fehlt sie, liest es `MYSQL_PASSWORD` aus der `.env` (Benutzer `dartforge`, `localhost:3306`). Abweichend: `ConnectionStrings__DartContext` setzen. Die Verbindung wird **vor** der Simulation geprüft.

| Option | Standard | Bedeutung |
|---|---|---|
| `--situations` | 1000 | Anzahl Situationen (je ~60 Kandidaten-Zeilen) |
| `--rollouts` | 50 | Rollouts je Kandidat |
| `--seed` | 1 | gleicher Seed = gleiche Datei |
| `--max-darts` | 60 | Obergrenze je Rollout |
| `--no-db` | – | nur Datei schreiben, keine Metadaten |

- **Ablage:** `MlData:Path` (Standard `ml_data` im Lösungsordner). `docker-compose.yml` bindet denselben Ordner schreibgeschützt in die API ein (`/data/ml`), damit sie später Modelle laden kann. `ml_data/` gehört nicht ins Git.
- **Situationen:** 60 % im Finish-Bereich (≤ 170), 20 % Straight-Out, 20 % Double-In (davon ein Teil noch nicht eröffnet), alle Fähigkeiten gleichverteilt 0–1.
- **Dauer:** grob einige Minuten für 1000 Situationen im Release-Build; die Kandidaten laufen parallel.
- **CSV:** Kopfzeile = Spaltennamen von `TargetCandidateFeatures`, Dezimalpunkt, Bool als 0/1.

## train (Phase 3)

Trainiert das Zielwahl-Modell aus einem Datensatz und speichert es, **ohne es zu aktivieren** (Aktivierung erst nach dem Vergleich mit dem Regelwerk, Phase 4).

```powershell
dotnet run --project Tools.BotTrainer -c Release -- train --dataset 4 --seed 1
```

| Option | Standard | Bedeutung |
|---|---|---|
| `--dataset` | – | Id aus `DartBotTrainingDatasets` (Pflicht) |
| `--seed` | 1 | Aufteilung Training/Test und Training reproduzierbar |
| `--trees` | 200 | Anzahl Bäume (FastTree) |
| `--leaves` | 64 | Blätter je Baum |
| `--min-leaf` | 20 | Mindestanzahl Zeilen je Blatt |

Ablauf:
1. Datensatz laden, SHA-256 und FeatureSchemaVersion prüfen (verändert oder falsche Version → Abbruch).
2. Lauf in `DartBotModelTrainings` anlegen (Status Running); bei Fehlern Status Failed mit Fehlermeldung.
3. Pipeline: Segment und Multiplikator one-hot, alle Merkmale zusammenführen, FastTree-Regression auf `ExpectedDartsToFinish`; 80/20 Training/Test.
4. Kennzahlen RMSE, MAE, R² in `MetricsJson`; Modell als `ml_data/models/…zip` (nie überschrieben) + `DartBotModels` (nächste Version je Typ).

Optional: `BOTTRAINER_COMMIT` (Git-Commit) wird als `CodeVersion` gespeichert.
