#!/usr/bin/env bash
#
# Builds a portable Linux AppImage for Noor.
#
# Requirements:
#   - .NET 10 SDK (builds the app self-contained)
#   - curl / wget (to fetch appimagetool)
#
# Usage:
#   ./packaging/linux/build-appimage.sh [architecture]
#   (default architecture: x64  →  produces linux-x64 AppImage)
#
set -euo pipefail

ARCH="${1:-x64}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PKG_DIR="${ROOT_DIR}/packaging"
ICON="${PKG_DIR}/icons/noor.png"
VERSION="${NOOR_VERSION:-1.0.0}"

case "${ARCH}" in
  x64|amd64) ARCH="x64"; DOTNET_RID="linux-x64"; AAI_ARCH="x86_64" ;;
  arm64)     DOTNET_RID="linux-arm64"; AAI_ARCH="aarch64" ;;
  *) echo "Unsupported arch: ${ARCH}" >&2; exit 1 ;;
esac

BUILD_DIR="${ROOT_DIR}/build/appimage"
PUBLISH_DIR="${ROOT_DIR}/Noor/bin/Release/net10.0-desktop/${DOTNET_RID}/publish"
APPDIR="${BUILD_DIR}/Noor.AppDir"
OUTPUT="${ROOT_DIR}/dist/Noor-${VERSION}-${DOTNET_RID}.AppImage"

echo "==> Publishing Noor (${DOTNET_RID}, self-contained)…"
cd "${ROOT_DIR}"
dotnet publish Noor/Noor.csproj \
  -c Release \
  -f net10.0-desktop \
  -r "${DOTNET_RID}" \
  --self-contained true \
  -p:PublishSingleFile=False \
  -p:PublishTrimmed=False \
  -o "${PUBLISH_DIR}"

echo "==> Assembling AppDir…"
rm -rf "${BUILD_DIR}"
mkdir -p "${APPDIR}/usr/bin"

# Copy the entire self-contained publish output into AppDir/usr/bin
cp -a "${PUBLISH_DIR}/." "${APPDIR}/usr/bin/"
chmod +x "${APPDIR}/usr/bin/Noor" 2>/dev/null || true

# Icon
cp "${ICON}" "${APPDIR}/noor.png"

# .desktop (AppImage convention: Exec=AppRun)
cat > "${APPDIR}/noor.desktop" <<EOF
[Desktop Entry]
Type=Application
Version=1.0
Name=Noor
GenericName=Prayer Times
Comment=Islamic prayer time companion with accurate calculations and Hijri calendar
Exec=AppRun %f
Icon=noor
Terminal=false
Categories=Education;
Keywords=islam;prayer;salah;salat;namaz;azan;athan;hijri;muslim;quran;
StartupNotify=true
StartupWMClass=Noor
EOF

# AppRun launcher
cat > "${APPDIR}/AppRun" <<'EOF'
#!/usr/bin/env bash
set -e
HERE="$(dirname "$(readlink -f "${0}")")"
export DOTNET_ROOT="${HERE}/usr/bin"
cd "${HERE}/usr/bin"
exec "${HERE}/usr/bin/Noor" "$@"
EOF
chmod +x "${APPDIR}/AppRun"

echo "==> Fetching appimagetool (${AAI_ARCH})…"
AAI_DIR="${BUILD_DIR}/appimagetool"
AAI_BIN="${AAI_DIR}/appimagetool-${AAI_ARCH}.AppImage"
mkdir -p "${AAI_DIR}"
if [ ! -x "${AAI_BIN}" ]; then
  curl -fsSL -o "${AAI_BIN}" \
    "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-${AAI_ARCH}.AppImage"
  chmod +x "${AAI_BIN}"
fi

echo "==> Building AppImage…"
mkdir -p "${ROOT_DIR}/dist"
# Running inside CI / systemd needs ARCH env for appimagetool
ARCH="${AAI_ARCH}" "${AAI_BIN}" --appimage-extract-and-run "${APPDIR}" "${OUTPUT}" || \
  ARCH="${AAI_ARCH}" "${AAI_BIN}" "${APPDIR}" "${OUTPUT}"

echo "==> Done: ${OUTPUT}"
ls -lh "${OUTPUT}"
