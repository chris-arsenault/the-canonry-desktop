#!/usr/bin/env bash
set -euo pipefail

PUBLISH_DIR="/mnt/c/Users/carse/AppData/Local/TheCanonry"
PROJECT="src/TheCanonry.Desktop/TheCanonry.Desktop.csproj"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
STAGING="$REPO_ROOT/.publish-staging"

echo "Publishing TheCanonry Desktop → $PUBLISH_DIR"

# Build to a local Linux staging directory first (avoids cross-filesystem bundling issues)
rm -rf "$STAGING"
dotnet publish "$REPO_ROOT/$PROJECT" \
    -c Release \
    -r win-x64 \
    --self-contained \
    -o "$STAGING" \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true

# Copy to Windows destination
mkdir -p "$PUBLISH_DIR"
cp -rf "$STAGING"/* "$PUBLISH_DIR"/
rm -rf "$STAGING"

echo ""
echo "Done. Run from Windows:"
echo "  C:\\Users\\carse\\AppData\\Local\\TheCanonry\\TheCanonry.Desktop.exe"
