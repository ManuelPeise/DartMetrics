# Logic.Profile

Profil des angemeldeten Benutzers (`UserProfileService`): Profildaten lesen und ändern, Passwort ändern.

- DI: `AddProfileServices()`.

Validierung: Vor- und Nachname Pflicht, alle Namen max. 100 Zeichen (sonst 400). `ChangePassword` gibt Fehler weiter (falsches aktuelles Passwort → 400). Das Elo steht lesend im Profilmodell.
