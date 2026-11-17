# DartMetrics – Benutzerhandbuch

So nutzt du DartMetrics als Spieler, GameMaster oder Administrator. Installation und Betrieb: [Technische Dokumentation](../sources/README.md).

## Inhalt

1. [Konto](#1-konto)
2. [Würfe eingeben](#2-würfe-eingeben)
3. [Trainings](#3-trainings)
4. [Gegen einen Bot spielen](#4-gegen-einen-bot-spielen)
5. [Turniere](#5-turniere)
6. [Statistiken](#6-statistiken)
7. [Profil](#7-profil)
8. [Rollen](#8-rollen)
9. [Administration](#9-administration)
10. [Datenschutz](#10-datenschutz)

## 1. Konto

- **Registrieren:** Vorname, Nachname, optional ein Anzeigename, E-Mail-Adresse und Passwort (mindestens 8 Zeichen). Danach kommt eine Bestätigungsmail; erst nach dem Klick auf den Link kannst du dich anmelden.
- **Keine Mail bekommen?** Auf der Anmeldeseite „Bestätigungsmail erneut senden“. Nicht bestätigte Registrierungen werden nach 7 Tagen automatisch gelöscht.
- **Passwort vergessen:** Auf der Anmeldeseite „Passwort vergessen“. Der Link in der Mail gilt 60 Minuten und nur einmal; danach bist du auf allen Geräten abgemeldet.
- **Zu viele Versuche:** Nach 10 Anmelde- oder Registrierungsversuchen pro Minute musst du kurz warten.
- Nach dem Anmelden landen Spieler im Spieler-Bereich (`/player`), Administratoren in der Administration (`/admin`). Die Anmeldung gilt höchstens 8 Stunden.

## 2. Würfe eingeben

Alle Spiele und Trainings nutzen dasselbe Eingabefeld, **ein Dart nach dem anderen**:

- **Double** oder **Triple** antippen und dann die Zahl: der Multiplikator gilt nur für den nächsten Dart.
- **25** ist das Single-Bull, **Bull** (50) ist Double 25. Ein Triple auf 25 gibt es nicht.
- **0** für einen Fehlwurf außerhalb der Scheibe.
- Bust, Checkout und Ende der Aufnahme erkennt die App selbst und zeigt sie sofort an.
- **Senden:** Die Darts einer Aufnahme werden erst mit **Senden** gewertet. Bis dahin kannst du mit ↶ korrigieren.
- **Checkout-Hinweis:** Sobald dein Rest checkbar ist, zeigt die App den empfohlenen Weg, z. B. „T20 · T20 · Bull“. Er wird nach jedem Dart neu berechnet: wenigste Darts, beliebte Doubles (D20, D16, D8 …), einfache Felder vor Triples.
- Ein angefangenes Training oder Bot-Spiel wird beim nächsten Öffnen fortgesetzt, auch nach einem Neuladen der Seite.

## 3. Trainings

Unter **Training** gibt es vier Modi:

| Modus | Ablauf | Auswertung |
|---|---|---|
| **X01** | 301 oder 501 allein, ein Satz aus 5 Legs; Straight- oder Double-In, Double- oder Straight-Out | 3-Dart-Average, Checkouts, Busts |
| **Checkout** | Immer wieder denselben Zielwert auschecken (40, 81, 100, 121 oder 170); ein Versuch = bis zu 3 Darts, Double-Out | Checkout-Quote |
| **Segment** | Ein Feld (Single, Double oder Triple einer Zahl oder Bull) über 5, 10 oder 20 Aufnahmen treffen | Trefferquote, Ø Treffer und Punkte je Aufnahme |
| **Around the Clock** | Die Segmente der Reihe nach treffen | Trefferquote, Darts bis zum Ziel |

„Beenden“ schließt ein Training vorzeitig ab; das Ergebnis zählt trotzdem.

## 4. Gegen einen Bot spielen

Unter **Gegen Bot spielen**:

1. **Bot wählen.** Jeder Bot hat ein Level (0 = Anfänger bis 20 = Profi) und eine Elo-Wertung.
2. **Einstellungen:**
   - *Training* (immer Best of 5) oder *Match* (Best of 5, 7, 9 oder 13 Legs; zählt als Wettkampf)
   - 301 oder 501, wer im ersten Leg anwirft, Straight-/Double-In, Double-/Straight-Out
3. **Spielen:** Du gibst deine Darts ein und schickst die Aufnahme mit **Senden** ab, auch bei einem Checkout oder Bust mit 1–2 Darts. Der Bot wirft sofort und zeigt, worauf er gezielt hat.
4. **Ergebnis:** Nach dem Spiel ändern sich deine Elo und die des Bots. Der Bot sammelt Erfahrungspunkte und steigt im Level auf: Er wird mit jedem Spiel gegen Menschen besser.

## 5. Turniere

Unter **Turniere** gibt es zwei Formate:

- **Liga:** Jeder gegen jeden, Hin- und Rückrunde, mit Tabelle.
- **K.-o.:** Turnierbaum mit Freilosen, optional mit Setzliste aus einer beendeten Liga.

Und zwei Arten:

| Art | Wer legt an | Teilnehmer |
|---|---|---|
| **Offiziell** | GameMaster | Spieler melden sich an, der GameMaster bestätigt; Bots fügt der GameMaster hinzu |
| **Privat** | jeder Spieler | Der Besitzer und Bots |

- **Spielplan:** Der Turnierleiter legt je Runde einen Termin fest. Bot-gegen-Bot-Matches startet der Server zum Termin von selbst; du kannst sie live verfolgen.
- **Match leiten:** Matches mit Menschen gibt der GameMaster (offiziell) bzw. der Besitzer (privat) Dart für Dart ein; mit Korrektur-Taste. Alle anderen sehen den Stand live.
- Offizielle Turniere verändern die Elo; private nicht.

## 6. Statistiken

Unter **Statistiken** wählst du oben:

- **Kategorie:** *Overall* (alles), *Training* (X01-Training, alle Spiele gegen Bots, Übungen) oder *Official* (Turnier-Matches)
- **Zeitraum:** gesamt, 7 Tage, 30 Tage, dieser Monat, dieses Jahr oder ein eigener Zeitraum

Alle Werte und Diagramme folgen dieser Auswahl:

| Bereich | Inhalt |
|---|---|
| Überblick | 3-Dart-Average, First-9-Average, Checkout-Quote, Siegquote – je mit Vergleich zum gleich langen Zeitraum davor (▲/▼); Verlauf von 3-Dart- und First-9-Average mit deinem Bestwert |
| Scoring | Highest Score, 180er (auch pro 100 Legs), 140+, 100+, 80+, 60+, Verteilung deiner Aufnahmen |
| Checkout | Checkout-Quote, Checkouts, Missed Doubles, Highest und Ø Checkout, Busts, Verlauf der Quote |
| Legs & Matches | Matches und Legs (gespielt/gewonnen), Ø Darts pro Leg, bestes Leg, längste Siegesserie, Elo |
| Entwicklung | Zeitraum gegen Vorzeitraum, letzte 10 gegen die 10 Matches davor, Bestwerte; bei genug Daten automatische Hinweise |
| Übungen | Checkout-, Segment- und Around-the-Clock-Training (nur Training/Overall) |

So wird gerechnet:
- **3-Dart-Average** = Punkte ÷ Darts × 3; **First 9** = die ersten drei Aufnahmen jedes Legs.
- **Checkout-Quote** = Checkouts ÷ Darts auf ein Doppel. Ein Dart zählt als Doppel-Versuch, wenn ein Ein-Dart-Finish möglich war (Rest gerade bis 40 oder 50); jeder verfehlte Doppel-Dart zählt mit. Nur Double-Out.
- Es zählen nur beendete Trainings und Spiele; abgebrochene Spiele nicht. Ein X01-Training allein ist kein Match (keine Siegquote).

## 7. Profil

- Name, Anzeigename und Profilbild ändern (JPEG, PNG oder WebP; der Browser verkleinert es auf 256 px)
- Elo und Verlauf ansehen
- Passwort ändern
- **Konto löschen:** mit Passwort bestätigen. Name, E-Mail-Adresse, Profilbild und Anmeldedaten werden gelöscht. Deine gespielten Matches bleiben als „Gelöschter Spieler“ erhalten, damit Tabellen und Ergebnisse anderer stimmen; offene Turnier-Matches gewinnt dein Gegner kampflos.

## 8. Rollen

Jeder Benutzer hat genau eine Rolle.

| Rolle | Kann |
|---|---|
| **Player** | Trainings, Spiele gegen Bots, eigene und offizielle Turniere, Statistiken, Profil |
| **GameMaster** | Offizielle Turniere anlegen und leiten (spielt dort nicht selbst mit) |
| **Admin** | Nur die Administration, kein Spieler-Bereich |

Neue Konten sind Player. Die Rolle GameMaster vergibt ein Admin.

## 9. Administration

Nur für Admins, unter `/admin`. Beim allerersten Login muss der Admin sein Startpasswort ändern.

| Bereich | Funktion |
|---|---|
| **Dashboard** | Benutzer, Bots, Trainings, Systemstatus, letzte Aktivitäten |
| **Benutzerverwaltung** | Rolle Player ↔ GameMaster ändern, Konten sperren/entsperren, anonymisiert löschen. Admin-Konten und das eigene Konto sind geschützt. |
| **DartBot-Verwaltung** | Bots anlegen (Name, Kürzel, optional Spielstil), aktivieren, deaktivieren, Entwicklung verfolgen |
| **System-Logs** | Fehler, Warnungen und Hinweise durchsuchen und löschen; Einträge werden nach 90 Tagen automatisch gelöscht |
| **Passwort ändern** | Eigenes Passwort |

## 10. Datenschutz

DartMetrics speichert nur, was für Konto und Spiele nötig ist: kein Tracking, keine Werbung, keine externen Dienste, nur ein technisch notwendiges Anmelde-Cookie. Details stehen in der Datenschutzerklärung der jeweiligen Instanz (`/datenschutz`), verantwortlich ist deren Betreiber (`/impressum`).
