# Logic.Profile

Profil des angemeldeten Benutzers (`UserProfileService`): Profildaten lesen und ändern, Passwort ändern.

- DI: `AddProfileServices()`.

Validierung: Vor- und Nachname Pflicht, alle Namen max. 100 Zeichen (sonst 400). `ChangePassword` prüft das aktuelle Passwort mit `PasswordHasher.VerifyPassword` (BCrypt mit Salt, nie Hashes vergleichen), verlangt `PasswordPolicy` (8 Zeichen bis 72 Bytes) und ein anderes neues Passwort; Fehler → 400. Das Elo steht lesend im Profilmodell.

## Profilbild

- Tabelle `UserAvatars` (1:1 zum User, MEDIUMBLOB), aktuelle Version in `Users.AvatarVersion` (null = kein Bild → Initialen).
- Der Browser verkleinert auf 256 px und wandelt in JPEG um (`RequestImageFileAsync`); der Server nimmt max. 512 KB (`AvatarRules`) und bestimmt den Typ aus den Signatur-Bytes (JPEG/PNG/WebP), nie aus Client-Angaben.
- API: `Profile/UploadAvatar`, `Profile/DeleteAvatar`, `Profile/GetAvatar?v=<Version>` (nur das eigene Bild; `Cache-Control: private, immutable`, `nosniff`).
- Beim Löschen (Anonymisieren) durch einen Admin oder selbst wird das Bild entfernt.
- `DeleteOwnAccount`: eigenes Konto löschen, bestätigt mit dem Passwort (nicht für Admins); nutzt `IUserAnonymizer`.
