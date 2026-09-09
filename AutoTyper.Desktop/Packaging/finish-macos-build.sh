#!/bin/bash
# Completes what a Windows-side `dotnet publish` cannot finish: builds the
# .app bundle structure, restores the executable bit that Windows-published
# files lose, generates the .icns icon, ad-hoc signs, and zips for
# distribution. Run this on a Mac after:
#
#   dotnet publish AutoTyper.Desktop -c Release -r osx-arm64 --self-contained -o ./publish-osx-arm64
#
# Usage: ./finish-macos-build.sh <publish-dir> <iconset-dir>
#   <publish-dir>  the -o directory from the dotnet publish command above
#   <iconset-dir>  a .iconset folder (see `man iconutil`) with the app's icon
#                  at the required sizes; there is no source icon in this
#                  repo yet (AutoTyper.Desktop/Assets/AppIcon.ico is a 32x32
#                  Windows icon, far too small for .icns) — new artwork is
#                  needed before this argument can be supplied for real.
set -euo pipefail

PUBLISH_DIR="${1:?Usage: finish-macos-build.sh <publish-dir> <iconset-dir>}"
ICONSET_DIR="${2:?Usage: finish-macos-build.sh <publish-dir> <iconset-dir>}"
APP_NAME="AutoTyper"
BUNDLE="${PUBLISH_DIR}/${APP_NAME}.app"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "==> Building .app bundle structure at ${BUNDLE}"
rm -rf "${BUNDLE}"
mkdir -p "${BUNDLE}/Contents/MacOS" "${BUNDLE}/Contents/Resources"

echo "==> Copying published output"
# Everything dotnet publish produced becomes the executable's own directory —
# .app bundles conventionally put the whole self-contained payload alongside
# the entry-point binary in Contents/MacOS/, not just the single exe.
find "${PUBLISH_DIR}" -maxdepth 1 -mindepth 1 ! -name "${APP_NAME}.app" -exec cp -R {} "${BUNDLE}/Contents/MacOS/" \;

echo "==> Restoring the executable bit (dotnet publish on Windows drops it)"
chmod +x "${BUNDLE}/Contents/MacOS/AutoTyper.Desktop"

echo "==> Installing Info.plist"
cp "${SCRIPT_DIR}/Info.plist.template" "${BUNDLE}/Contents/Info.plist"

if [ -d "${ICONSET_DIR}" ]; then
    echo "==> Building AppIcon.icns from ${ICONSET_DIR}"
    iconutil -c icns "${ICONSET_DIR}" -o "${BUNDLE}/Contents/Resources/AppIcon.icns"
else
    echo "WARNING: iconset dir '${ICONSET_DIR}' not found — skipping icon; app will use a generic icon."
fi

echo "==> Ad-hoc code signing (no paid Developer ID required)"
# Ad-hoc (the "-" identity) is NOT notarization: Gatekeeper will still warn on
# first launch ("AutoTyper can't be opened because it is from an unidentified
# developer" -> right-click, Open). Expected for a hand-distributed build.
codesign --deep --force --sign - "${BUNDLE}"

echo "==> Verifying signature"
codesign --verify --verbose "${BUNDLE}"

echo "==> Zipping for distribution"
ditto -c -k --keepParent "${BUNDLE}" "${PUBLISH_DIR}/${APP_NAME}.zip"

echo "==> Done. ${PUBLISH_DIR}/${APP_NAME}.zip is ready."
echo "    First launch will need a right-click > Open (Gatekeeper, ad-hoc signed, not notarized)."
