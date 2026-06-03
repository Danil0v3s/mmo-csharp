# Parity Roadmap — Timeline & pick-order (vertical rebuild)

Companion to [README.md](README.md). The README is the **what**; this is the
**in-what-order**. Each ticket is a vertical slice (one playable capability, all layers).
The loop / a contributor takes the **first ticket in pick-order whose `Depends on:` are
all in `done/`**.

Old per-layer sequencing + the full historical progress log are archived in
[`_archive/TIMELINE-history.md`](_archive/TIMELINE-history.md).

## Standing directives

- **Gameplay first.** Phase 2 capabilities are the biggest player-facing win and where
  "done ≠ playable" hurt most.
- **Combat last, scripting truly last** (user pivot, persisted in memory).
- **Each ✅ must be playable end-to-end and survive logout** — not "service exists".

## Phase order

```
Phase A  gameplay/   ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓   playable capabilities (16)   ← do these
Phase B  infra/      ▓▓▓▓▓▓▓▓             small vertical features (8)  ← parallel anytime
Phase B  mobai/      ▓▓                   mob AI (2)                   ← parallel anytime
Phase C  status/     ▓▓▓▓                 SC depth (4)
Phase D  skills/     ▓▓▓▓▓▓▓▓▓            per-family depth (9)
Phase E  combat/     ▓▓▓▓▓                damage formula tail (5)      ← combat last
Phase F  scripting/  ▓▓▓▓▓▓▓              NPC runtime + content (7)    ← truly last
```

## Pick-order (Phase A — gameplay, do top-to-bottom)

Ordered by leverage (most-used capabilities + dependency-free first):

1. **GP-MAIL** — most-used social feature; the mail service already has real transfer logic, needs the RODEX packet set + persistence round-trip. Dependency-free.
2. **GP-PARTY** — service mostly exists; pure packet-bridge work. Dependency-free.
3. **GP-QUEST** — drives most PvE content; quest service real, needs load-on-enter + objective filters + UI packets. Dependency-free.
4. **GP-PET** — popular; entity + catch/hatch landed, needs packets + combat + persistence. Dependency-free.
5. **GP-VEND** — player economy; transfer logic landed, needs packets + autotrade persistence. Dependency-free.
6. **GP-BUYSTORE** — pairs with vending. Dependency-free.
7. **GP-CASHSHOP** — buy path landed, needs catalog data + point persistence + UI packets. Dependency-free.
8. **GP-AUCTION** — economy; map-side wiring landed, needs packets + item fidelity. Dependency-free.
9. **GP-ACHIEVE** — pairs with quest UI packets. Dependency-free (shares PACKET work with GP-QUEST — coordinate).
10. **GP-GUILD** — large packet set; service exists. Dependency-free.
11. **GP-HOMUN** — entity slice landed, needs AI/combat/growth/hunger/packets. Dependency-free.
12. **GP-MERC** — entity slice landed, needs AI/combat/lifetime/packets. Dependency-free.
13. **GP-ELEM** — lifetime sweep landed, needs AI + create/load/delete IPC. Dependency-free.
14. **GP-INSTANCE** — lifecycle landed; **must build the dynamic-map subsystem** (the hard prerequisite) before instances are enterable. Largest gameplay ticket.
15. **GP-WOE** — scheduler landed; needs castle Emperium/guardian content + can-hit gate. Soft-depends on GP-GUILD (castle ownership) + GP-INSTANCE patterns.
16. **GP-MVPFAME** — MVP reward packets + fame ranking board. Soft-depends on GP-PARTY (kill credit fan-out).

Phase B (infra + mobai) can be pulled **in parallel** any time a contributor wants a
smaller, self-contained vertical — none of them block Phase A.

Then Phase C (status) → D (skills) → E (combat) → F (scripting), per the standing
directive. Within each, take the first dependency-free row; `SK-ENGINE` should lead the
skills phase (it unblocks the family tickets).

## Progress log

Update as cards move (date · ticket · todo→inprogress / inprogress→done · one line).

- **2026-06-03** — Board restructured from layer-sliced to vertical. Old board (112 todo
  + 119 done) archived under `_archive/`; new TEMPLATE enforces end-to-end scope. The
  landed code from the archived `FEATURE-01..15` / `COMBAT-01..96` / `SC-01..08` etc.
  remains in the repo and is cited by the vertical tickets as "verify-and-extend".
- **2026-06-03** — **GP-MAIL** inprogress→done (Phase A, 6 turns). Built the entire RODEX client packet bridge to rAthena struct fidelity + handler unit tests (the service + persistence IPC were already built in archive FEATURE-05): receive side (open-mailbox/refresh→ZC_ACK_MAIL_LIST, read→ZC_ACK_READ_RODEX, zeny/item-from-mail→acks, delete→ack) + compose side (begin-write/check-name/add-item/remove-item/send→acks) + the service methods (RequestInbox/Read/Delete/CheckReceiver) + the overweight gate. A player can compose a mail with zeny+a carded item, send it, and the recipient opens RODEX, reads, claims (cards intact), and deletes it — all client→service→client. 16 handler tests + extended service tests; full suite 4414 pass (1 standing replay-fixture). Filed GP-MAIL-RENTAL (rental expiry) + GP-MAIL-PARTIAL-CLAIM (separated claims); live-client wire validation is the project's standing deferred pass.
- **2026-06-03** — **GP-PARTY** inprogress→done (Phase A, 3 turns). Built the party client packet bridge (the char IPC + notify layer + cache were already present): create (CZ_MAKE_GROUP) + invite-by-name (CZ_PARTY_JOIN_REQ) + leave/expel/change-leader/change-option handlers (driving the established IIntifService path) + the HP-bar/minimap-dot sync (new ZC_NOTIFY_HP_TO_GROUPM + PartySyncService, rAthena party_send_xy_timer cadence, wired into MapServerImpl). A player can create/invite/accept/leave/expel/change-leader/set-EXP-share and see teammates' HP bars + dots. 19 party-suite tests; full suite 4429 pass (1 standing replay-fixture). Filed GP-PARTY-EXPEL-REASON + GP-PARTY-INSTANT-HP.
