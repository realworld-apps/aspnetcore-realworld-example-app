#!/usr/bin/env bash
set -euo pipefail

VERSION=8.0.1
NAME="hurl-$VERSION-x86_64-unknown-linux-gnu"
ARCHIVE="$NAME.tar.gz"
BASE="https://github.com/Orange-OpenSource/hurl/releases/download/$VERSION"
cd "${RUNNER_TEMP:?This installer expects GitHub Actions RUNNER_TEMP}"
curl --fail --silent --show-error --location --output "$ARCHIVE" "$BASE/$ARCHIVE"
checksum="$(curl --fail --silent --show-error --location "$BASE/$ARCHIVE.sha256")"
printf '%s  %s\n' "$checksum" "$ARCHIVE" | sha256sum --check -
tar xzf "$ARCHIVE"
echo "$RUNNER_TEMP/$NAME/bin" >> "${GITHUB_PATH:?This installer expects GitHub Actions GITHUB_PATH}"
