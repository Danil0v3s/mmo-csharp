#!/usr/bin/env bash
# Find C# call sites for a given rAthena symbol.
#
# Usage:
#   ./coverage.sh pc_setoption
#   ./coverage.sh skill_castend_damage_id Map.Server
#
# Greps the C# tree (default: Map.Server + Core.Server) for the symbol
# and prints file:line hits, ignoring bin/obj.
set -euo pipefail

if [[ $# -lt 1 ]]; then
    echo "usage: $0 <rathena-symbol> [search-root]" >&2
    exit 1
fi

symbol="$1"
root="${2:-/Volumes/1TB/Projetos/mmo-csharp}"

# Search both .cs and .md (audit docs reference rAthena names too).
grep -rn "$symbol" "$root" \
    --include="*.cs" --include="*.md" \
    2>/dev/null \
    | grep -v "/bin/" | grep -v "/obj/" \
    || echo "(no hits for $symbol in $root)"
