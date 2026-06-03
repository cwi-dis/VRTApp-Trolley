#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
UNITY_VERSION=6000.3.10f1
UNITY_CMD="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
APP_NAME=VRTApp-Trolley

BUILD_PATH="$PROJECT_DIR/Build/$APP_NAME.app"
ZIP_PATH="$PROJECT_DIR/Build/$APP_NAME-mac.zip"
LOG_PATH="$PROJECT_DIR/Build/buildlog-mac.txt"

mkdir -p "$PROJECT_DIR/Build"

echo "Building macOS player..."
echo "  Project: $PROJECT_DIR"
echo "  Output:  $BUILD_PATH"
echo "  Log:     $LOG_PATH"

if ! "$UNITY_CMD" -batchmode -projectPath "$PROJECT_DIR" \
        -buildOSXUniversalPlayer "$BUILD_PATH" \
        -quit -logfile "$LOG_PATH"; then
    echo "============= Unity build failed ============="
    cat "$LOG_PATH"
    echo "=============================================="
    exit 1
fi

echo "Zipping to $ZIP_PATH..."
rm -f "$ZIP_PATH"
(cd "$PROJECT_DIR/Build" && zip -r "$APP_NAME-mac.zip" "$APP_NAME.app")
echo "Done: $ZIP_PATH"
