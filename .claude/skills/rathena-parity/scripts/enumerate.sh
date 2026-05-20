#!/usr/bin/env bash
# Enumerate the public function surface of an rAthena source file.
#
# Usage:
#   ./enumerate.sh /Volumes/1TB/Projetos/rathena/src/map/pc.cpp
#
# Output: one line per function, formatted as
#   <return_type> <name>
# Sorted, deduplicated. Suitable for piping into the audit doc.
#
# How it works:
#   rAthena consistently writes top-level functions starting at column 0
#   with the return type followed by the name on the same line. We match
#   the common return-type set and strip the parameter list.
set -euo pipefail

if [[ $# -lt 1 ]]; then
    echo "usage: $0 <rathena-source-file>" >&2
    exit 1
fi

src="$1"
if [[ ! -f "$src" ]]; then
    echo "error: $src not found" >&2
    exit 1
fi

# Return-type pattern covers the bulk of rAthena C/C++ functions.
# Add others (e.g. `t_tick`, `e_*`) as new subsystems get audited.
pattern='^(int32?|bool|void|short|char|uint8|uint16|uint32|uint64|int64|long|float|double|t_tick|TBL_PC \*|TBL_MOB \*|map_session_data\s*\*)'

grep -E "$pattern" "$src" \
    | sed 's/(.*//' \
    | awk 'NF >= 2' \
    | sort -u
