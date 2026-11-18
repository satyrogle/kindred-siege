#!/bin/bash
# Determinism across processes (the in-process rerun in Program.cs only proves a process matches itself, v12 audit):
# rebuilds the sim from source (it tested a stale exe if nobody rebuilt, v15 audit), then runs it twice in two separate
# processes for each seed given and compares the trace files byte for byte. Any failure prints and exits non zero.
# This proves one machine and one runtime only.
# usage: bash sim2/check.sh [seed ...]   (default seeds 1 2 3)
HERE="$(cd "$(dirname "$0")" && pwd)"
MONO="C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/MonoBleedingEdge/bin/mono.exe"
bash "$HERE/build.sh" || { echo "CHECK FAILED: build"; exit 1; }
TMP="$(mktemp -d)"
SEEDS="${@:-1 2 3}"
fail=0
for s in $SEEDS; do
  "$MONO" "$HERE/bin/Brawl2.exe" "$s" "$TMP/a$s" > "$TMP/a$s.log" 2>&1; ra=$?
  "$MONO" "$HERE/bin/Brawl2.exe" "$s" "$TMP/b$s" > "$TMP/b$s.log" 2>&1; rb=$?
  if [ $ra -ne 0 ] || [ $rb -ne 0 ]; then echo "seed $s: run failed (exit $ra and $rb; exit 2 is an in process mismatch)"; tail -3 "$TMP/a$s.log"; fail=1; continue; fi
  for f in fight1.json fight2.json; do
    if cmp -s "$TMP/a$s/$f" "$TMP/b$s/$f"; then echo "seed $s $f: identical across processes"; else echo "seed $s $f: DIFFERENT"; fail=1; fi
  done
done
rm -rf "$TMP"
[ $fail -eq 0 ] && echo "CHECK OK" || echo "CHECK FAILED"
exit $fail
