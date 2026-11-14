# Logic.Authentication

Anmeldung und Registrierung.

- `UserAuthentication`: Login (prüft `IsActive` und bestätigte E-Mail), Refresh-Token, Logout.
- `UserRegistration`: Registrierung (Konto zunächst inaktiv), E-Mail-Bestätigung, Rollenzuweisung.
- `JwtTokenGenerator`: Access-Token mit Claims aus `ClaimsHelper` (Rollen im Claim `UserRoles`).
- DI: `AddAuthenticationServices()`.
