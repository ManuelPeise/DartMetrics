# Third-Party Notices

DartMetrics' **source code** is licensed under the MIT License (see [LICENSE](LICENSE)).
The following bundled assets are **not** covered by the MIT License and remain under their own licenses.
They may be used as part of DartMetrics, but must not be extracted and redistributed on their own.

## Images (bundled in `sources/Web.App/Web.App/wwwroot/img`)

Licensed under the [Pixabay Content License](https://pixabay.com/service/license-summary/).
Free to use within this application, no attribution required. Selling or redistributing the images
on a standalone basis (unchanged, e.g. as a stock photo, wallpaper or print) is **not** permitted.

| File | Author | Source |
| --- | --- | --- |
| `background.jpg` | TODO: contributor name | TODO: Pixabay URL |
| `dart_board.png` | TODO: contributor name | TODO: Pixabay URL |

If you fork DartMetrics and want to use it under different terms, replace these images with your own.

## Fonts (bundled in `sources/Web.App/Web.App/wwwroot/fonts`)

Licensed under the [SIL Open Font License 1.1](https://openfontlicense.org), obtained via [Fontsource](https://fontsource.org).

| Font | Copyright | Source |
| --- | --- | --- |
| Barlow Condensed | Jeremy Tribby | https://github.com/jpt/barlow |
| Caveat | Impallari Type | https://github.com/googlefonts/caveat |
| Inter | Rasmus Andersson | https://github.com/rsms/inter |

## NuGet packages (not bundled in this repository)

Restored at build time; their licenses are shipped with the packages.

| Package | License |
| --- | --- |
| .NET / ASP.NET Core / Entity Framework Core | MIT |
| MySql.EntityFrameworkCore, MySql.Data | GPL-2.0 with Universal FOSS Exception |
| BCrypt.Net-Next | MIT |
| System.IdentityModel.Tokens.Jwt, Microsoft.IdentityModel.Tokens | MIT |
| Swashbuckle.AspNetCore.SwaggerUI | MIT |
| xUnit, Testcontainers (tests only) | Apache-2.0 / MIT |

## Docker images (not bundled)

`mysql:8.4` (GPL-2.0), `axllent/mailpit` (MIT, development only), `mcr.microsoft.com/dotnet/*` (MIT).
