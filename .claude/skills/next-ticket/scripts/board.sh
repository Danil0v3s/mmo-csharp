#!/usr/bin/env bash
# Print the parity-roadmap kanban board so the next-ticket skill can pick the
# next card. Lists IN PROGRESS, DONE (count + ids), and TODO grouped by epic with
# each card's "Depends on:" header so dependency-eligibility is visible at a glance.
#
# Usage: board.sh [roadmap-root]   (default: the repo's .agents/roadmap)
set -euo pipefail

ROOT="${1:-/Volumes/1TB/Projetos/mmo-csharp/.agents/roadmap}"
[ -d "$ROOT" ] || { echo "roadmap root not found: $ROOT" >&2; exit 1; }

dep_of() {  # extract the "Depends on:" value from a ticket header (best-effort)
  grep -m1 -i 'Depends on' "$1" 2>/dev/null \
    | sed -E 's/.*Depends on:\*\* *//; s/ *·.*//; s/[<>]//g' \
    | tr -d '\r' || true
}

echo "=== IN PROGRESS ==="
found=0
while IFS= read -r f; do found=1; echo "  $(basename "$f")  [deps: $(dep_of "$f")]"; done \
  < <(find "$ROOT/inprogress" -name '*.md' ! -name 'README.md' 2>/dev/null | sort)
[ "$found" = 0 ] && echo "  (empty)"

done_n=$(find "$ROOT/done" -name '*.md' ! -name 'README.md' 2>/dev/null | wc -l | tr -d ' ')
echo
echo "=== DONE ($done_n) ==="
find "$ROOT/done" -name '*.md' ! -name 'README.md' 2>/dev/null -exec basename {} \; | sort | sed 's/^/  /' || true
[ "$done_n" = 0 ] && echo "  (none yet)"

echo
echo "=== TODO (by epic; check deps against DONE above) ==="
for epic in $(find "$ROOT/todo" -mindepth 1 -maxdepth 1 -type d 2>/dev/null | sort); do
  echo "  [$(basename "$epic")]"
  while IFS= read -r f; do
    printf '    %-52s deps: %s\n' "$(basename "$f")" "$(dep_of "$f")"
  done < <(find "$epic" -name '*.md' | sort)
done

echo
echo "Selection: resume any IN PROGRESS card first; else walk TIMELINE.md phases in"
echo "order and take the first TODO card whose deps are all in DONE (or 'none')."
