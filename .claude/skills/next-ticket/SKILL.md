---
name: next-ticket
description: Pick up the next ticket from the parity roadmap and implement it end-to-end through the kanban lanes (todo → inprogress → done), following the ticket to the letter with ZERO stubs and ZERO deferrals. Use whenever the user invokes `/next-ticket` (optionally with a ticket id like `/next-ticket COMBAT-01`), or asks to "work the roadmap", "do the next ticket", "pick up the next roadmap item", "continue the roadmap", "walk a ticket through the lanes", or "grab the next card". The roadmap lives at `/Volumes/1TB/Projetos/mmo-csharp/.agents/roadmap/`; selection order is governed by `TIMELINE.md`.
---

# Roadmap ticket driver

You implement **exactly one** parity-roadmap ticket per invocation, fully and
faithfully, and move its card through the kanban. The roadmap is the canonical
worklist at `/Volumes/1TB/Projetos/mmo-csharp/.agents/roadmap/`:

```
README.md      index + ground truth
TIMELINE.md    phase order + dependency cheat-sheet + Progress log
TEMPLATE.md    ticket format
todo/<epic>/   tickets waiting (grouped by epic)
inprogress/    the card currently being worked (flat)
done/          completed cards (flat)
```

Project conventions are in `/Volumes/1TB/Projetos/mmo-csharp/CLAUDE.md`. rAthena
source of truth is `/Volumes/1TB/Projetos/rathena/src/map/` — the monolithic
`skill.cpp`/`battle.cpp`/`status.cpp`/`pc.cpp`/`mob.cpp` switch arms (the
`rathena-fork/src/map/skills/...` paths in some C# docstrings do NOT exist here).

## HARD RULES — non-negotiable

1. **No stubs.** No empty bodies, no `return false/0/null/default` placeholders, no
   log-only no-ops, no `NotImplementedException`, no `// TODO`/`// FIXME`/
   `data-pending` left in any file you touch. Every item in the ticket's **Scope**
   checklist ships real behavior.
2. **No deferrals.** You do not defer a Scope item to "a later ticket". If you hit a
   genuinely-separate gap that is *out of this ticket's scope*, you **create a new
   ticket** in `todo/<epic>/` (using `TEMPLATE.md`) and note it — you never just
   leave the work undone or write a placeholder.
3. **Every caveat becomes a ticket — no exceptions.** If, when you finish, there is
   *anything* you could not do 100% — a Done-criterion you couldn't fully meet, a
   formula approximated, a numeric tolerance, behavior verified for some cases but
   not all, a known limitation, an "interim" implementation, a TODO you'd otherwise
   write in your report, or an "honest caveat" you'd otherwise just mention — you
   **MUST first capture it as a follow-up ticket in `todo/<epic>/`** (via
   `TEMPLATE.md`) before you report it. The rule of thumb: **if it would appear under
   an "Honest caveats / leftovers" heading in your summary, it must already exist as
   a `todo/` file.** A caveat without a matching ticket is a process failure. Each
   reported caveat must cite its ticket id. This is how COMBAT-01 spawned COMBAT-10.
4. **Parity-first.** Match rAthena's validation gates, state transitions, formulas,
   and failure modes exactly — not "what makes sense in C#". Read the cited source.
5. **Done means done.** Every line in the ticket's **Done criteria** must literally
   hold (or be explicitly moved to a follow-up ticket per rule 3), with the
   **Test plan** tests added and the suite green, before the card moves to `done/`.
6. **One ticket per invocation.** Finish it completely. Don't start a second
   (filing follow-up tickets per rules 2–3 does not count as "starting" them).

## The loop

### 1. Select the card

- Run `scripts/board.sh` (bundled) to see the board state.
- **If the user named a ticket** (`/next-ticket COMBAT-01`), use that one.
- **Else if `inprogress/` already has a card**, resume it (do not pick a new one) —
  this is work to finish, possibly from a prior session.
- **Else pick the next eligible card from `todo/`:** read `TIMELINE.md` and walk its
  phases in order (Phase 0 → 5). Within the earliest phase that still has todo cards,
  pick the first ticket whose `**Depends on:**` header lists only tickets already in
  `done/` (or "none"). If every card in the earliest phase is dependency-blocked,
  move to the next phase. State which card you chose and why (phase + deps satisfied).

### 2. Move it to the in-progress lane

```
git mv .agents/roadmap/todo/<epic>/<TICKET>.md .agents/roadmap/inprogress/<TICKET>.md
```
Flip the ticket's Status header `❌ Not started` → `🚧 In progress`. Commit just this
move so the board reflects WIP even if work spans sessions:
`git commit -m "roadmap: start <TICKET-ID>"` (end with the Co-Authored-By trailer per
CLAUDE.md / the harness rule).

### 3. Read the ticket to the letter

Read the **whole** ticket. Then open **every** cited rAthena function and **every**
cited C# file/line. Confirm the current C# state matches what the ticket claims (the
ticket was written from an audit — verify it still holds at HEAD; if the code moved,
update the ticket's Current-state section as you go). Build the full mental model
before editing.

### 4. Implement every Scope item

Work the Scope checklist top to bottom. For each box: implement the real behavior,
wire it into the call sites the ticket names (entity/field + EF migration if
persisted, repository/loader, service body, packet def + handler, IPC proto + char
RPC, game-loop/observer hookup, ZC emit). Honor CLAUDE.md: persisted state goes
through `GameDbContext`/repositories (no in-memory shortcuts), packet handlers use
`[PacketHandler]` + `IPacketHandler<TSession,TPacket>`, repositories inject directly,
game state stays single-threaded (queue to the loop, don't lock), log at info/warn/
error appropriately. If the ticket says an EF migration is needed, create it
(`dotnet ef migrations add <Name> --project Core.Database`) and a seed path if the
data comes from rAthena YAML (via `Tools.RathenaImporter` + `Core.Database/Seeds`).

If you discover the ticket under-specified something, expand the ticket inline AND
implement it. If you discover an adjacent but separate gap, file a new `todo/` ticket
(rule 2) — do not absorb scope creep silently or defer it.

### 5. Verify — build, test, no stubs

- `dotnet build Map.Server` (and any other touched project) → **0 errors**.
- Add the tests named in the ticket's **Test plan**, then
  `dotnet test <project> --filter ...` → green. Run the broader suite if the change
  is cross-cutting.
- Grep the touched files: `grep -rn 'TODO\|FIXME\|data-pending\|NotImplementedException'`
  → must be empty for your changes. Confirm each **Done criteria** bullet literally
  holds (compute the numbers where the ticket gives rAthena-exact values).

### 6. Capture every leftover as a follow-up ticket (rule 3)

Before you touch the docs, write down — for yourself — the list of anything that is
not 100%: unmet/partial Done-criteria, approximations, numeric tolerances, "interim"
code, cases you didn't verify, known limitations, anything you'd put under an
"Honest caveats" heading. **For each one, create a `todo/<epic>/<NEW-ID>.md` from
`TEMPLATE.md`** with the real scope (rAthena ref, C# locations, done-criteria,
tests). Then:

- In the finishing ticket, annotate the moved/partial Done-criteria with `➡️ Moved
  to <NEW-ID>` so the boundary is explicit.
- Add the new ticket(s) to the `README.md` index table and (if relevant) the
  `TIMELINE.md` dependency notes.

If your leftover list is empty, the card is genuinely 100% — proceed. If it is not
empty and you have not filed the tickets, you are not done.

### 7. Update the docs + advance the lane

- In the ticket file: Status header → `✅ Done (<YYYY-MM-DD>)`; append a one-line
  `## History` entry (date + what landed + commit will be added).
- In `TIMELINE.md`: add a line to the **Progress log** (`<date> · <TICKET-ID> ·
  inprogress→done · <one-line summary>`).
- Move the card:
  `git mv .agents/roadmap/inprogress/<TICKET>.md .agents/roadmap/done/<TICKET>.md`.

### 8. Commit

Stage the production code + tests + the ticket file(s) + `TIMELINE.md` (including any
follow-up tickets filed in step 6), and commit with a
message that names the ticket and what shipped, e.g.:

```
<TICKET-ID> — <short title>

<what changed, which rAthena refs matched, which Scope items landed>

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
```

Keep roadmap/lane changes in the same commit as the implementation so a completed
card and its code land together. Do not push unless asked. If on the default branch,
follow the project's established convention (it commits waves directly to `main`).

## Reporting back

Tell the user: which ticket you took (and why it was next), the Scope items you
implemented, the tests added + green status, build status, the commit hashes (start +
finish), and a **"Follow-ups filed"** list. Be honest about anything not 100% — but
per rule 3, **every such caveat must already be a filed `todo/` ticket, and you cite
its id when you mention it.** Never write an "Honest caveats" line that does not point
at a ticket; never claim parity you didn't confirm. If you found yourself wanting to
say "this part isn't fully done" without a ticket, go back to step 6 and file it.

## Notes

- This skill pairs with `rathena-parity` (which drives a whole `.cpp` file). Use
  `next-ticket` for the roadmap's pre-decomposed cards; use `rathena-parity` when the
  user names a raw rAthena source file instead.
- If `git mv` reports the source not tracked, the roadmap files may be uncommitted —
  fall back to `mv` + `git add -A .agents/roadmap`.
