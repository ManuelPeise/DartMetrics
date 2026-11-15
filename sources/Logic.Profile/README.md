# Logic.Profile

Profil des angemeldeten Benutzers (`UserProfileService`): Profildaten lesen und ändern, Passwort ändern.

- DI: `AddProfileServices()`.

Validierung: Vor- und Nachname Pflicht, alle Namen max. 100 Zeichen (sonst 400). `ChangePassword` prüft das aktuelle Passwort mit `PasswordHasher.VerifyPassword` (BCrypt mit Salt, nie Hashes vergleichen), verlangt `PasswordPolicy` (8 Zeichen bis 72 Bytes) und ein anderes neues Passwort; Fehler → 400. Das Elo steht lesend im Profilmodell.
