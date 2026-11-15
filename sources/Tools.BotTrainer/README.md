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
- **Situationen:** 60 % im Finish-Bereich (≤ 170), 20 % Straight-Out, 20 % Double-In (davon ein Teil noch nicht eröffnet); Fähigkeiten zu 80 % zwischen 0,3 und 1.
- **Schema 2:** Spalten `SituationId` (Gruppe) und `RelativeDarts` (Label: Darts mehr als der beste Kandidat der Situation). Datensätze mit Schema 1 können nicht mehr trainiert werden.
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
3. Pipeline: Segment und Multiplikator one-hot, alle Merkmale zusammenführen, FastTree-Regression auf `RelativeDarts`; 80/20 Training/Test, getrennt nach Situationen.
4. Kennzahlen RMSE, MAE, R² und – entscheidend für die Zielwahl – Trefferquote des besten Ziels je Testsituation und mittlerer Verlust in Darts, in `MetricsJson`; Modell als `ml_data/models/…zip` (nie überschrieben) + `DartBotModels` (nächste Version je Typ).

Optional: `BOTTRAINER_COMMIT` (Git-Commit) wird als `CodeVersion` gespeichert.

## compare (Phase 4)

Spielt je Skill-Stufe (0,5 / 0,7 / 0,85 / 0,95; TacticalIntelligence 1) dieselben Legs ab 501 einmal mit der Modell-Zielwahl und einmal mit `RuleBasedTargetSelector`. Die Würfe nutzen je Leg denselben Zufallsstrom (gepaarter Vergleich), damit nur die Zielwahl den Unterschied macht.

```powershell
dotnet run --project Tools.BotTrainer -c Release -- compare --model 2 --legs 2000
```

| Option | Standard | Bedeutung |
|---|---|---|
| `--model` | – | Id aus `DartBotModels` (Pflicht) |
| `--legs` | 2000 | Legs je Stufe und Strategie |
| `--seed` | 1 | Zufall der Legs |
| `--max-darts` | 60 | Obergrenze je Leg |
| `--straight-out` | – | Straight-Out statt Double-Out |
| `--margins` | 0,0.3,0.5,1 | Schwellen für den Hybrid (Komma-Liste, Dezimalpunkt) |
| `--tactical` | 1 | TacticalIntelligence der Testprofile (0–1); unter 1 macht das Regelwerk absichtliche Fehler |

**Hybrid:** Das Regelwerk schlägt ein Ziel vor; das Modell ersetzt es nur, wenn sein bester Kandidat laut Vorhersage um mehr als die Schwelle (in Darts) besser ist. Schwelle 0 = reines Modell. Je Stufe und Schwelle wird zusätzlich ausgegeben, wie oft das Modell abgewichen ist. Am Ende stehen die Schwellen, mit denen das Modell auf keiner Stufe signifikant schlechter ist.

Ausgabe je Stufe: Darts pro Leg (Regelwerk, Modell), mittlere Differenz mit 95-%-Intervall (negativ = Modell besser), Anteil Legs, in denen das Modell besser bzw. schlechter war.
Das Ergebnis wird unter `Comparison` in `MetricsJson` des Trainingslaufs gespeichert. Exit-Code 4, wenn das Modell auf einer Stufe signifikant schlechter ist.

Die Modell-Zielwahl (`MlTargetSelector`) liegt vorläufig im Werkzeug; in Phase 5 wandert sie nach `Logic.Bot`.
