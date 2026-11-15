# Logic.Authentication

Anmeldung und Registrierung.

- `UserAuthentication`: Login (prüft `IsActive` und bestätigte E-Mail), Refresh-Token, Logout.
- `UserRegistration`: Registrierung (Konto zunächst inaktiv), E-Mail-Bestätigung, Rollenzuweisung.
  - Bestätigungsmail erneut senden (nur unbestätigte Konten).
  - Passwort vergessen: Link `/password/reset?token=…`, Token mit eigener Audience (`…PasswordReset`), 60 Minuten gültig, an `PasswordChangedAt` gebunden (einmal nutzbar). Nach dem Setzen wird das Refresh-Token widerrufen (überall abgemeldet).
  - Beide Anfragen verraten nicht, ob es die Adresse gibt, und schicken höchstens eine Mail je Adresse und Zweck alle 2 Minuten (Sperre im Speicher der API).
- `JwtTokenGenerator`: Access-Token mit Claims aus `ClaimsHelper` (Rollen im Claim `UserRoles`).
- DI: `AddAuthenticationServices()`.
