# Tools.BotTrainer

Konsolenwerkzeug für die ML-Phasen der DartBots. Läuft lokal oder als Docker-Container, nie in der API.

## Schnellweg: alles in Docker

Nach einem frischen Deployment ist kein Modell aktiv (die Bots spielen dann regelbasiert). Ein Befehl erledigt generate → train → compare → activate:

```powershell
.\deploy\train-model.ps1                    # Windows
./deploy/train-model.sh                      # Linux / Raspberry Pi
```

Aktiviert wird nur, wenn das Modell auf keiner Stufe signifikant schlechter ist als das Regelwerk. Ohne `-Force` / `--force` passiert nichts, wenn schon ein Modell aktiv ist. `deploy/deploy.ps1` ruft das Skript automatisch auf.

Einzelne Befehle im Container (Dienst `bottrainer`, Profil `tools`, schreibt nach `./ml_data`):

```powershell
docker compose --profile tools run --rm bottrainer generate --situations 1000 --rollouts 50 --seed 1
docker compose --profile tools run --rm bottrainer train --dataset 4
```

Die Optionen unten gelten für beide Wege. Lokal mit `dotnet run` nutzt das Werkzeug die DB auf `localhost:3306` (nur in der Entwicklung offen, siehe `docker-compose.override.yml`).

## Aus dem Admin-Bereich: worker

Der Container `bottrainer-worker` (in `docker-compose.yml`, startet mit `docker compose up -d`) läuft dauerhaft mit `worker`.
Er prüft alle 10 s die Tabelle `DartBotTrainingJobs` und führt einen Auftrag aus `/admin/models` aus:
`generate` → `train` → `compare`, mit Fortschritt und Ergebnis im Admin-Bereich. Aktiviert wird nur von Hand.

| Umfang   | Situationen | Rollouts | Vergleichs-Legs je Stufe |
|----------|-------------|----------|--------------------------|
| Schnell  | 3000        | 50       | 500                      |
| Standard | 20000       | 50       | 2000                     |
| Gründlich | 40000      | 75       | 5000                     |

Standard entspricht `deploy/train-model`. „Schnell“ ist nur ein Funktionstest: mit wenigen Situationen ist das Modell meist schlechter als das Regelwerk.

- Es wartet oder läuft immer höchstens ein Auftrag; jeder Auftrag bekommt einen neuen Seed.
- Abbrechen: wartende Aufträge sofort, laufende beim nächsten Zwischenstand (während des eigentlichen Trainings erst danach).
  Ein abgebrochener Datensatz bleibt als Datei in `ml_data/datasets` liegen, wird aber nicht registriert.
- Ohne Lebenszeichen (alle 30 s) für 10 min gilt ein Auftrag als verloren; nach einem Neustart des Workers werden laufende Aufträge als fehlgeschlagen markiert.
- CPU-Begrenzung über `BOTTRAINER_CPUS` in `.env` (Standard 2), damit die API daneben spielbar bleibt.
- Logs: `docker compose logs -f bottrainer-worker`.

Standard und Gründlich stimmen das Training ab (`--tune`); der Vergleich läuft immer als Prüfstand (`--benchmark`).

## Besseres Modell: Prüfstand, Abstimmen, Runden (Schema 3)

- **Merkmale, Schema 3:** zusätzlich `LeftoverMinDarts` (kürzester Checkout-Weg nach Treffer), `LeftoverFinishableThisVisit`,
  `HitProbability` und `BustProbability` (je Ziel und gerundetem Profil simuliert, zwischengespeichert). Modelle mit Schema 2
  gelten danach als „Altes Schema“ und müssen neu trainiert werden.
- **Situationen (Sampler v2):** Rest gewichtet 2–60 30 %, 61–110 25 %, 111–170 20 %, 171–501 25 %; Fähigkeiten wie echte Bots
  (Level 3–20 über die Level-Kurve ± 0,05); Gegnerstand halb in der Nähe des eigenen Rests.
- **Rollouts nach Rest:** ≤ 100 doppelt, > 300 halb so viele (`--fixed-rollouts` schaltet das ab).
- **`train --tune`:** fünf Varianten (Bäume, Blätter, Mindestbeispiele, Lernrate) auf einem Validierungsanteil der Trainingsdaten,
  die mit dem kleinsten Verlust (Regret) wird auf allen Trainingsdaten trainiert. Ergebnis und alle Varianten in `MetricsJson`.
- **`compare --benchmark`:** Prüfstand mit festem Seed, 5.000 Legs je Stufe, Bot-Level 5/10/15/20 (Fähigkeiten aus der Level-Kurve).
  `BenchmarkScore` = mittlere Darts pro Leg gegenüber dem Regelwerk mit der besten sicheren Schwelle (negativ = besser).
  Nur Werte aus dem Prüfstand sind zwischen Modellen vergleichbar.
- **Runden (`generate --policy-model <id>`):** In den Rollouts spielt das Modell (Hybrid, Schwelle `--policy-margin`) bis Rest
  `--policy-max-remaining` (Standard 170) weiter statt des Regelwerks; die Vorhersagen werden je Situation zwischengespeichert.
  Ablauf je Runde: generate mit dem besten Modell → train --tune → compare --benchmark; weiter nur, wenn der Prüfstand besser wird
  (höchstens 3 Runden oder bis die Verbesserung unter 0,05 Darts liegt). Die Ausgabe zeigt ms je Situation – damit messen,
  ob sich das im Admin-Bereich lohnt.

```
docker compose --profile tools run --rm bottrainer generate --situations 2000 --seed 7 --policy-model 2
```

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

- **Ablage:** `MlData:Path` (Standard `ml_data` im Lösungsordner). `docker-compose.yml` bindet denselben Ordner schreibgeschützt in die API ein (`/data/ml`), damit sie die Modelle laden kann; der Container `bottrainer` bindet ihn beschreibbar ein. `ml_data/` gehört nicht ins Git.
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

Die Hybrid-Zielwahl (`HybridTargetSelector`, `LoadedTargetModel`) liegt in `Logic.Bot/Ml`; Werkzeug und API nutzen denselben Code.

## activate / deactivate (Phase 5)

```powershell
dotnet run --project Tools.BotTrainer -c Release -- activate --model 3
dotnet run --project Tools.BotTrainer -c Release -- deactivate
```

`activate` prüft Typ, FeatureSchemaVersion und SHA-256 der Datei und setzt den Eintrag für TargetSelection in `DartBotActiveModels` (`ActivatedBy` = BotTrainer). Neu gestartete Bot-Spiele nutzen dann die Hybrid-Zielwahl mit `DartBot:Ml:Margin` (Standard 0,3). `deactivate` entfernt den Eintrag: neue Spiele sind wieder regelbasiert.
