#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_DIR="$(cd "$ROOT_DIR/.." && pwd)"
INFO_PLIST="$ROOT_DIR/Sources/DesktopIconManagerMac/Resources/Info.plist"
ENTITLEMENTS_PLIST="$ROOT_DIR/Sources/DesktopIconManagerMac/Resources/Entitlements.plist"
APP_NAME="桌面管理器"
EXECUTABLE_NAME="DesktopIconManagerMac"
VERSION="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$INFO_PLIST")"
BUILD_DIR="$ROOT_DIR/.build/mac-app"
DIST_DIR="$ROOT_DIR/dist"
APP_DIR="$BUILD_DIR/$APP_NAME.app"
MACOS_DIR="$APP_DIR/Contents/MacOS"
RESOURCES_DIR="$APP_DIR/Contents/Resources"
ICON_SOURCE="$REPO_DIR/src/DesktopIconManager/Assets/app-icon.png"

mkdir -p "$MACOS_DIR" "$RESOURCES_DIR" "$DIST_DIR"

BINARIES=()
for ARCH in arm64 x86_64; do
  SCRATCH_DIR="$ROOT_DIR/.build/swiftpm-$ARCH"
  swift build --package-path "$ROOT_DIR" --scratch-path "$SCRATCH_DIR" -c release --arch "$ARCH"
  BIN_DIR="$(swift build --package-path "$ROOT_DIR" --scratch-path "$SCRATCH_DIR" -c release --arch "$ARCH" --show-bin-path)"
  BINARIES+=("$BIN_DIR/$EXECUTABLE_NAME")
  test -x "$BIN_DIR/$EXECUTABLE_NAME"
done

lipo -create "${BINARIES[@]}" -output "$MACOS_DIR/$EXECUTABLE_NAME"
ARCHITECTURES="$(lipo -archs "$MACOS_DIR/$EXECUTABLE_NAME")"
[[ " $ARCHITECTURES " == *" arm64 "* && " $ARCHITECTURES " == *" x86_64 "* ]] || {
  echo "Universal 架构检查失败：$ARCHITECTURES"
  exit 1
}

cp "$INFO_PLIST" "$APP_DIR/Contents/Info.plist"
printf 'APPL????' > "$APP_DIR/Contents/PkgInfo"
/usr/bin/plutil -lint "$APP_DIR/Contents/Info.plist"

if [[ -f "$ICON_SOURCE" ]]; then
  ICONSET_DIR="$BUILD_DIR/AppIcon.iconset"
  mkdir -p "$ICONSET_DIR"
  for SIZE in 16 32 128 256 512; do
    sips -s format png -z "$SIZE" "$SIZE" "$ICON_SOURCE" --out "$ICONSET_DIR/icon_${SIZE}x${SIZE}.png" >/dev/null
    DOUBLE_SIZE=$((SIZE * 2))
    sips -s format png -z "$DOUBLE_SIZE" "$DOUBLE_SIZE" "$ICON_SOURCE" --out "$ICONSET_DIR/icon_${SIZE}x${SIZE}@2x.png" >/dev/null
  done
  iconutil -c icns "$ICONSET_DIR" -o "$RESOURCES_DIR/AppIcon.icns"
fi

if [[ -n "${DEVELOPER_ID_APPLICATION:-}" ]]; then
  codesign --force --options runtime --timestamp --entitlements "$ENTITLEMENTS_PLIST" --sign "$DEVELOPER_ID_APPLICATION" "$APP_DIR"
else
  codesign --force --entitlements "$ENTITLEMENTS_PLIST" --sign - "$APP_DIR"
  echo "未配置 Developer ID，已使用临时签名。"
fi
codesign --verify --deep --strict --verbose=2 "$APP_DIR"

STAGING_DIR="$(mktemp -d "$BUILD_DIR/dmg-stage.XXXXXX")"
ditto "$APP_DIR" "$STAGING_DIR/$APP_NAME.app"
ln -s /Applications "$STAGING_DIR/Applications"
DMG_PATH="$DIST_DIR/DesktopIconManagerMac-$VERSION-universal.dmg"
hdiutil create -volname "$APP_NAME" -srcfolder "$STAGING_DIR" -ov -format UDZO "$DMG_PATH"
hdiutil verify "$DMG_PATH"

if [[ -n "${APPLE_ID:-}" || -n "${APPLE_TEAM_ID:-}" || -n "${APPLE_APP_PASSWORD:-}" ]]; then
  if [[ -z "${DEVELOPER_ID_APPLICATION:-}" || -z "${APPLE_ID:-}" || -z "${APPLE_TEAM_ID:-}" || -z "${APPLE_APP_PASSWORD:-}" ]]; then
    echo "公证配置不完整；需要 Developer ID、Apple ID、Team ID 和 App 专用密码。"
    exit 1
  fi
  xcrun notarytool submit "$DMG_PATH" --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" --password "$APPLE_APP_PASSWORD" --wait
  xcrun stapler staple "$DMG_PATH"
fi

echo "App: $APP_DIR"
echo "DMG: $DMG_PATH"
echo "Architectures: $ARCHITECTURES"
