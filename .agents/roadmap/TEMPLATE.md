# <TICKET-ID> — <Capability, stated as a player outcome>

> **Epic:** <gameplay/combat/status/skills/scripting/infra/mobai> · **Status:** ❌ Not started · **Size:** S/M/L/XL · **Player-visible:** yes/no
> **Depends on:** <ticket ids or "none"> · **Unlocks:** <ticket ids or "none">

## The deliverable (definition of done, in one sentence)

> A player can **<do the thing>** against the live client, and it **survives logout**.

A ticket is a **vertical slice**: it owns the capability end-to-end across every layer
it needs (data → persistence → service → IPC → client packets → client-observable
behaviour). It is NOT "the service method" or "the packet" — those are *layers of this
ticket*, not separate tickets. If you cannot make the player-outcome true without
touching another layer, that layer is in scope here. Do not split a layer out into a
follow-up; build it.

## Player story / why it matters

What the player does, step by step, and what's broken today that stops them. Be concrete:
"Open the cash shop → pick Bubble Gum → it's free / errors / nothing happens because …".

## Current state — what exists vs. what's missing (per layer)

A table so the implementer knows exactly what to reuse vs. build. Cite files + lines.

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Data / seed (`*_db`, YAML→SQL) | ☐/partial/✅ | `Core.Database/...`, importer converter, seed file |
| Entity + EF migration | ☐/✅ | `Core.Database/Entities/...` |
| Repository | ☐/✅ | `Core.Database/Repositories/...` |
| Service logic | ☐/partial/✅ | `Map.Server/.../XService.cs` — what's real, what returns 0/false |
| Persistence IPC (proto + char RPC) | ☐/✅ | `*.proto`, `IntifService`, char-side handler |
| Client → map packet (CZ handler) | ☐/✅ | `Core.Server/Packets/In/...`, `Map.Server/Handlers/...` |
| Map → client packet (ZC emit) | ☐/✅ | `Core.Server/Packets/Out/...`, emit site |
| Game-loop / observer / lifecycle wiring | ☐/✅ | tick hook, observer registration |

> If a row says ✅ from earlier work, **verify it still holds at HEAD and that it's
> actually reached end-to-end** — "service exists" means nothing if no packet calls it.

## rAthena reference (source of truth)

Per layer, the canonical `rathena/src/...` functions. The map-side switch arms live in
the monolithic `clif.cpp` (packet parse/emit), `<feature>.cpp` (logic), `intif.cpp`
(inter-server), `char/...` (persistence). Quote the key validation gates, state
transitions, packet shapes, and formulas. Note where this C# port intentionally
diverges (and why).

## Dependencies — and how to satisfy them

For each dependency, say whether it's a **prerequisite ticket** (must land first) or a
**foundation pattern** (build it here, following an existing example):
- `<DEP-ID>` — prerequisite; this ticket is blocked until it's done. Why.
- Packet-bridge pattern — foundation; add the CZ handler + ZC emit yourself following
  the ~39 existing handlers (e.g. `Map.Server/Handlers/<example>.cs`). Not a separate ticket.
- Persistence-IPC pattern — foundation; wire the save/load IPC following `IntifService` +
  an existing char-side RPC. Not a separate ticket.

## Scope — every layer this capability needs (build all of it)

Group the checklist by layer so nothing is silently dropped. Each box ships real behaviour.
- [ ] **Data**: …
- [ ] **Entity + migration**: …
- [ ] **Repository / loader**: …
- [ ] **Service**: … (every method, real bodies)
- [ ] **Persistence**: load on enter, save on mutate + logout (no in-memory-only state)
- [ ] **CZ handler(s)**: `[PacketHandler]` + `IPacketHandler<TSession,TPacket>`
- [ ] **ZC emit(s)**: the client-visible packets
- [ ] **Wiring**: game-loop / observer / DI
- [ ] **Client-observable behaviour**: the thing the player sees

## Done criteria (player-observable + survives logout)

Concrete, end-to-end, testable. Each bullet is something a player or an integration test
can observe — not "the method returns the right value".
- The player can <X> against the live client.
- It persists: relog → state intact.
- rAthena-exact numbers for cases A/B/C.
- No layer left as a stub / log-only / `return 0` — and no NEW follow-up ticket carrying
  a layer this capability needs.

## Test plan (cross-layer)

- Unit/regression for the service + formula numbers.
- Handler test for the CZ packet → service path.
- Persistence round-trip (save → reload → equal).
- Manual/live-client checklist for the full player story.

## Notes / gotchas

Anything that'll trip the implementer (DI cycles, timezone, escrow ordering, etc.).

---

### When you finish

Flip Status → `✅ Done (<date>)`, append a `## History` line, add a TIMELINE Progress-log
line, `git mv` to `done/`. **Filing a follow-up is only legitimate for a genuinely
NEW capability you discovered — never for a layer this ticket already needed.** If you
catch yourself writing "service landed, packets → later", you have not finished the ticket.
