#!/bin/bash
# 打包桌面版：macOS .app、.zip、.dmg，以及 Windows x64 .zip。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if [[ -x /tmp/dotnetbin/dotnet ]]; then
  export PATH="/tmp/dotnetbin:${PATH}"
fi
if [[ -z "${DOTNET_ROOT:-}" && -d "${HOME}/.dotnet" ]]; then
  export DOTNET_ROOT="${HOME}/.dotnet"
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export COPYFILE_DISABLE=1

VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/MessageRecord/MessageRecord.csproj | head -1)"
DIST="${ROOT}/artifacts/dist"
rm -rf "${DIST}"
mkdir -p "${DIST}"

publish_mac() {
  local rid="$1"
  echo "publish ${rid}"
  dotnet publish src/MessageRecord/MessageRecord.csproj \
    -c Release -r "${rid}" --self-contained true \
    -p:UseAppHost=true \
    -o "${ROOT}/artifacts/publish/${rid}"

  local app="${DIST}/MessageRecord.app"
  rm -rf "${app}"
  mkdir -p "${app}/Contents/MacOS" "${app}/Contents/Resources"
  cp packaging/macos/Info.plist "${app}/Contents/Info.plist"
  cp -R -X "${ROOT}/artifacts/publish/${rid}/." "${app}/Contents/MacOS/"
  chmod +x "${app}/Contents/MacOS/MessageRecord"
  codesign --force --deep --sign - "${app}"

  ditto -c -k --norsrc --keepParent "${app}" "${DIST}/MessageRecord-${VERSION}-${rid}.zip"

  local stage="${DIST}/dmg-stage"
  rm -rf "${stage}"
  mkdir -p "${stage}"
  cp -R "${app}" "${stage}/MessageRecord.app"
  ln -s /Applications "${stage}/Applications"
  hdiutil create -volname "MessageRecord" -srcfolder "${stage}" -ov -format UDZO \
    "${DIST}/MessageRecord-${VERSION}-${rid}.dmg"
  rm -rf "${stage}" "${app}"
}

publish_win() {
  local rid="win-x64"
  echo "publish ${rid}"
  dotnet publish src/MessageRecord/MessageRecord.csproj \
    -c Release -r "${rid}" --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "${ROOT}/artifacts/publish/${rid}"
  local stage="${DIST}/MessageRecord"
  rm -rf "${stage}"
  mkdir -p "${stage}"
  cp -X "${ROOT}/artifacts/publish/${rid}/MessageRecord.exe" "${stage}/"
  cp -X "${ROOT}/artifacts/publish/${rid}/MessageRecord.pdb" "${stage}/"
  ditto -c -k --norsrc --keepParent "${stage}" \
    "${DIST}/MessageRecord-${VERSION}-${rid}.zip"
  rm -rf "${stage}"
}

publish_mac "osx-arm64"
publish_win || echo "Windows 套件沒有完成，macOS 套件仍會保留。"

mkdir -p "${ROOT}/release"
(
  cd "${DIST}"
  shasum -a 256 MessageRecord-"${VERSION}"-*.zip MessageRecord-"${VERSION}"-*.dmg
) | tee "${ROOT}/release/MessageRecord-desktop-v${VERSION}-SHA256SUMS.txt"

echo "packages are in ${DIST}"
