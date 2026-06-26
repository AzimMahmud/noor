#!/usr/bin/env bash
#
# Builds a .deb package for Noor (Debian/Ubuntu and derivatives).
#
# Requirements:
#   - .NET 10 SDK
#   - dpkg-deb (apt: dpkg-dev)
#
# Usage:
#   ./packaging/linux/build-deb.sh [architecture]
#   (default architecture: x64  →  produces amd64 .deb)
#
set -euo pipefail

ARCH="${1:-x64}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PKG_DIR="${ROOT_DIR}/packaging"
ICONS_DIR="${PKG_DIR}/icons"
DESKTOP="${PKG_DIR}/linux/noor.desktop"
VERSION="${NOOR_VERSION:-1.0.0}"
PKG_VERSION="${VERSION//-/}"; PKG_VERSION="${PKG_VERSION//+/}"

case "${ARCH}" in
  x64|amd64) DOTNET_RID="linux-x64"; DEB_ARCH="amd64" ;;
  arm64)     DOTNET_RID="linux-arm64"; DEB_ARCH="arm64" ;;
  *) echo "Unsupported arch: ${ARCH}" >&2; exit 1 ;;
esac

BUILD_DIR="${ROOT_DIR}/build/deb"
PUBLISH_DIR="${ROOT_DIR}/Noor/bin/Release/net10.0-desktop/${DOTNET_RID}/publish"
PKGROOT="${BUILD_DIR}/noor_${PKG_VERSION}-1_${DEB_ARCH}"
OUTPUT="${ROOT_DIR}/dist/noor_${PKG_VERSION}-1_${DEB_ARCH}.deb"

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

echo "==> Assembling .deb tree…"
rm -rf "${BUILD_DIR}"
mkdir -p \
  "${PKGROOT}/DEBIAN" \
  "${PKGROOT}/usr/lib/noor" \
  "${PKGROOT}/usr/bin" \
  "${PKGROOT}/usr/share/applications" \
  "${PKGROOT}/usr/share/doc/noor"

# Application files
cp -a "${PUBLISH_DIR}/." "${PKGROOT}/usr/lib/noor/"
chmod -R go-w "${PKGROOT}/usr/lib/noor"

# Wrapper script → /usr/bin/noor
cat > "${PKGROOT}/usr/bin/noor" <<'EOF'
#!/usr/bin/env bash
set -e
APPDIR="/usr/lib/noor"
export DOTNET_ROOT="${APPDIR}"
cd "${APPDIR}"
exec "${APPDIR}/Noor" "$@"
EOF
chmod 755 "${PKGROOT}/usr/bin/noor"

# Launcher .desktop
cp "${DESKTOP}" "${PKGROOT}/usr/share/applications/noor.desktop"

# Icons (hicolor)
for size in 16 24 32 48 64 128 256 512; do
  src="${ICONS_DIR}/icon-${size}.png"
  [ -f "${src}" ] || continue
  d="${PKGROOT}/usr/share/icons/hicolor/${size}x${size}/apps"
  mkdir -p "${d}"
  cp "${src}" "${d}/noor.png"
done

# control metadata
INSTALLED_SIZE="$(du -sk "${PKGROOT}/usr/lib/noor" | cut -f1)"
cat > "${PKGROOT}/DEBIAN/control" <<EOF
Package: noor
Version: ${PKG_VERSION}-1
Section: utils
Priority: optional
Architecture: ${DEB_ARCH}
Installed-Size: ${INSTALLED_SIZE}
Maintainer: AzimMahmud <mahamud.azim@gmail.com>
Description: Islamic prayer time companion
 Noor provides accurate prayer-time calculations, a Hijri calendar,
 audio azan reminders, and a focus overlay — all offline-first.
 .
 It ships a built-in astronomical prayer-time engine (no third-party
 prayer library) and uses the Umm al-Qura Hijri calendar.
Homepage: https://github.com/AzimMahmud/Noor
Depends: libice6, libsm6, libfontconfig1
EOF

# Changelog stub (required by lintian-ish expectations)
cat > "${PKGROOT}/usr/share/doc/noor/changelog" <<EOF
noor (${PKG_VERSION}-1) stable; urgency=medium

  * Package build of Noor ${VERSION}.

 -- AzimMahmud <mahamud.azim@gmail.com>  $(date -R)
EOF
gzip -9n "${PKGROOT}/usr/share/doc/noor/changelog"

# Copyright
cat > "${PKGROOT}/usr/share/doc/noor/copyright" <<'EOF'
Format: https://www.debian.org/doc/packaging-manuals/copyright-format/1.0/
Upstream-Name: Noor
Source: https://github.com/AzimMahmud/Noor

Files: *
Copyright: 2024 AzimMahmud
License: MIT
License: full text at /usr/share/common-licenses/MIT or project LICENSE file.
EOF

echo "==> Building .deb…"
mkdir -p "${ROOT_DIR}/dist"
# Preserve timestamps and use deterministic build
dpkg-deb --root-owner-group -Zxz -b "${PKGROOT}" "${OUTPUT}"

echo "==> Done: ${OUTPUT}"
dpkg-deb -I "${OUTPUT}" | sed 's/^/    /'
ls -lh "${OUTPUT}"
