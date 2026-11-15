# Logic.Authentication

Anmeldung und Registrierung.

- `UserAuthentication`: Login (prüft `IsActive` und bestätigte E-Mail), Refresh-Token, Logout.
- `UserRegistration`: Registrierung (Konto zunächst inaktiv), E-Mail-Bestätigung, Rollenzuweisung.
  - Bestätigungsmail erneut senden (nur unbestätigte Konten).
  - Passwort vergessen: Link `/password/reset?token=…`, Token mit eigener Audience (`…PasswordReset`), 60 Minuten gültig, an `PasswordChangedAt` gebunden (einmal nutzbar). Nach dem Setzen wird das Refresh-Token widerrufen (überall abgemeldet).
  - Beide Anfragen verraten nicht, ob es die Adresse gibt, und schicken höchstens eine Mail je Adresse und Zweck alle 2 Minuten (Sperre im Speicher der API).
- Einmal-Passwort: `UserCredentials.MustChangePassword` (gesetzt beim Admin-Seed). Das Access-Token trägt dann den Claim `MustChangePassword`; `Web.Core/MiddleWare/MustChangePasswordMiddleware` lässt bis zur Änderung nur Profil laden, Passwort ändern und An-/Abmelden zu (sonst 403). `ChangePassword` hebt das Flag auf; danach neu anmelden.
- `JwtTokenGenerator`: Access-Token mit Claims aus `ClaimsHelper` (Rollen im Claim `UserRoles`).
- DI: `AddAuthenticationServices()`.
