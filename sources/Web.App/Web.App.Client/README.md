# Web.App.Client

Blazor WebAssembly: öffentliche Seiten und Spieler-Bereich.

## Struktur

```
Authentication/   BffAuthenticationStateProvider (Anmeldestatus über /bff/user), Formular-Modelle
Layout/           MainLayout, PublicLayout, LoginDisplay, AreaLink
Pages/            Home, Login, Logout, Register, VerifyEmail, AccessDenied, NotFound
Routing/AppAreas  Grenze zwischen WASM und Server-Bereich (/admin)
Services/         AccountService (Login, Registrierung, E-Mail-Bestätigung, Logout über /bff),
                  ApiClient (API über den BFF-Proxy /bff/api/**), BffHttpClient + CsrfHeaderHandler
Routes.razor      Router mit AuthorizeRouteView, nimmt im Server-Modus die Admin-Seiten auf
```

## Regeln

- Keine Server-Abhängigkeiten (läuft im Browser).
- API-Aufrufe nur über das BFF (`/bff/api/**`), nie direkt an die API.
- Links in den Admin-Bereich mit `AreaLink` (voller Seitenwechsel).
- Farben/Schriften aus `public.css` (`--pl-*`, Präfix `pl-`).
