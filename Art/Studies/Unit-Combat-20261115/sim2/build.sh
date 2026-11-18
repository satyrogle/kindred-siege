#!/bin/bash
# Builds the layer study (sim2) with Unity's own compiler against the sim's DeterministicRng; run with Unity's Mono.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/../../../.." && pwd)"
U="C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data"
NS="$U/NetStandard/ref/2.1.0/netstandard.dll"
OUT="$(cygpath -m "$HERE/bin")"; mkdir -p "$OUT"
FILES=()
for f in "$HERE"/*.cs; do FILES+=("$(cygpath -m "$f")"); done
"$U/NetCoreRuntime/dotnet.exe" "$U/DotNetSdkRoslyn/csc.dll" -nologo -noconfig -nostdlib -langversion:9.0 -warn:0 \
  -target:exe -out:"$OUT/Brawl2.exe" -reference:"$NS" "$(cygpath -m "$REPO/Assets/Scripts/Sim/DeterministicRng.cs")" "${FILES[@]}"
echo "BUILD OK $OUT/Brawl2.exe"
