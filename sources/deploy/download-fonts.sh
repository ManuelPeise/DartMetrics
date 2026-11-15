#!/usr/bin/env bash
# Lädt die Schriften der Web-App einmalig nach Web.App/Web.App/wwwroot/fonts (lokal ausgeliefert, DSGVO).
set -euo pipefail
target="$(dirname "$0")/../Web.App/Web.App/wwwroot/fonts"
mkdir -p "$target"
for font in barlow-condensed/latin-600-normal barlow-condensed/latin-700-normal \
            caveat/latin-500-normal caveat/latin-700-normal \
            inter/latin-400-normal inter/latin-600-normal; do
  family="${font%%/*}"; variant="${font#*/}"
  url="https://cdn.jsdelivr.net/fontsource/fonts/${family}@latest/${variant}.woff2"
  echo "  $url"
  curl -fsSL "$url" -o "$target/${family}-${variant}.woff2"
done
echo "Fertig: $target"
