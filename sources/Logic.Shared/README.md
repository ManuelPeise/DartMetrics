# Logic.Shared

Gemeinsame Bausteine der Fachlogik.

## Inhalt

```
ALogicBase                 Basisklasse aller Services (UnitOfWork, MessageLog)
UnitsOfWork/               UnitOfWork (SaveChangesAsync, Audit-Felder, Unveränderlichkeits-Schutz),
                           UserUnitOfWork, DartTrainingUnitOfWork, DartBotUnitOfWork, TournamentUnitOfWork
Repositories/RepositoryBase Generisches Repository (Get…, Any, Add, Update, Delete)
Rules/                     DartRulesBase (Segment/Multiplikator/Score), X01Rules (Bust, Checkout, Checkout-Wege;
                           In-Modus Straight/Double, Out-Modus Double/Straight, CountedScore für Double-In),
                           AroundTheClockRules
Logger/MessageLog          Log in die Datenbank und den .NET-Logger
Mail/                      EmailClient, EmailBodyFactory (Bestätigung, Aktivierung, Passwort-Reset)
Users/UserAnonymizer       Kontolöschung (Admin und selbst): Daten ersetzen, Login/Bild löschen, Bot-Spiele anonym,
                           Turniere über ITournamentWithdrawal
Helpers/ClaimsHelper       Aktueller Benutzer aus den JWT-Claims, Claim-Namen
DI/SharedServiceRegistration AddSharedServices(): IUnitOfWork, IMessageLog, IClaimsHelper, IUserAnonymizer, IEmailClient
```

## Regeln

- Ein `SaveChangesAsync()` pro fachlichem Vorgang.
- Kein `Update()` auf geladene Objektgraphen; getrackte Entities direkt ändern.
- `MessageLog.LogMessage` leert den ChangeTracker – nur nach dem Speichern oder im catch-Block aufrufen.
- Neues Repository: Property in der passenden `…UnitOfWork` und deren Interface in `Shared.Interfaces`.
