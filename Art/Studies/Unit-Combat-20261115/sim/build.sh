#!/bin/bash
# Builds the unit combat study with Unity's own compiler, the way tools/sim_checks does, against the sim's DeterministicRng.
# usage: build.sh  -> sim/bin/Brawl.exe ; run it with Unity's Mono.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/../../../.." && pwd)"
U="C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data"
NS="$U/NetStandard/ref/2.1.0/netstandard.dll"
OUT="$(cygpath -m "$HERE/bin")"; mkdir -p "$OUT"
CSC() { "$U/NetCoreRuntime/dotnet.exe" "$U/DotNetSdkRoslyn/csc.dll" -nologo -noconfig -nostdlib -langversion:9.0 -warn:0 "$@"; }
CSC -target:exe -out:"$OUT/Brawl.exe" -reference:"$NS" \
  "$(cygpath -m "$REPO/Assets/Scripts/Sim/DeterministicRng.cs")" \
  "$(cygpath -m "$HERE/Brawl.cs")" "$(cygpath -m "$HERE/Program.cs")"
echo "BUILD OK $OUT/Brawl.exe"
