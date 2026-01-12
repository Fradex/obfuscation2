#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/src/ObfuscationTool/ObfuscationTool.csproj"
OUTPUT_DIR="$ROOT_DIR/dist"
RUNTIME="${1:-linux-x64}"

mkdir -p "$OUTPUT_DIR"

dotnet publish "$PROJECT" \
  -c Release \
  -r "$RUNTIME" \
  /p:PublishSingleFile=true \
  /p:SelfContained=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUTPUT_DIR"

echo "Single-file build available in: $OUTPUT_DIR"
