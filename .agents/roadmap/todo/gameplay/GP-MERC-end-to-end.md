# GP-MERC — Mercenary works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SCR-DOMAIN

## The deliverable

> A player can **summon a mercenary (from a scroll) that follows + fights for a limited
> contract time, see its info/faith/calls, and it expires/dies/is dismissed** — live client,
> surviving logout (re-summon resumes the remaining contract).

## Player story

The *entity* slice is real (live `MercenaryEntity` spawns on Create/RecvData, vanishes on
Delete/Dead/ContractStop, snapshot projects real MercenaryData — archive FEATURE-09). Missing:
AI/combat, the lifetime-expiry + summon callsite + kill-bonus trigger, and client packets.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Data | ✅ | `mercenary_db` seeded |
| Entity + lifecycle | ✅ verify | `Map.Server/Entities/MercenaryEntity.cs` + `MercenaryService` spawn/vanish (archive FEATURE-09) |
| AI / combat | ❌ | follow + attack the master's target (archive FEATURE-32) |
| Lifetime / summon | ❌ | contract-time expiry, the scroll summon callsite, kill-bonus, mercId round-trip (archive FEATURE-33) |
| CZ handlers | ❌ | command (delete), info req (archive PACKET-05) |
| ZC emits | ❌ | merc info/faith/calls/expiry missing |
| Persistence | ❌ | merc row + remaining contract time (FEATURE-17 save fan-out) |

## rAthena reference

- `rathena/src/map/mercenary.cpp` — `mercenary_create`/`mercenary_recv_data`,
  `mercenary_delete`, `mercenary_killbonus`, `mer_hom`-style AI (`mob`-FSM follow+attack),
  the contract `life_time` timer (`mercenary_contract_stop` on expiry), `mercenary_get_faith`/
  `mercenary_set_calls`.
- `rathena/src/map/clif.cpp` — `CZ_MER_COMMAND` (delete), `clif_mercenary_info`,
  `clif_mercenary_skillblock`, `clif_mercenary_message`/`clif_mercenary_updatestatus`.
- The scroll item-script `mercenary_create` callsite (item use → summon).

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Merc save IPC — `IntifService.MercSave` exists; wire via the save fan-out (DI-cycle rule as
  GP-HOMUN, archive FEATURE-17).
- Item-use → summon — the mercenary scroll's `mercenary_create` callsite (wire it).

## Scope — every layer

- [ ] **AI/combat**: merc follows + attacks the master's target, uses its skills.
- [ ] **Lifetime**: contract `life_time` countdown → auto-`ContractStop` on expiry; summon
      from the scroll; kill-bonus (faith/calls) on kills (archive FEATURE-33).
- [ ] **CZ handlers**: command (delete/rest), info request.
- [ ] **ZC emits**: merc info, faith/calls, status updates, expiry message.
- [ ] **Persistence**: merc row + remaining contract time; re-summon resumes the contract.

## Done criteria

- Player uses a merc scroll → a mercenary spawns, follows + fights for the contract time,
  shows info/faith/calls; on expiry (or dismiss/death) it vanishes; kills accrue faith.
- Relog mid-contract → the merc re-summons with the remaining time + faith intact.

## Test plan

- Handler tests: command/info → service.
- Service: contract expiry, kill-bonus, summon (extend archived MercenarySpawnTests).
- Persistence round-trip (contract time).
- Live: scroll → fight → faith → expiry; relog mid-contract.

## Notes / gotchas

- `SerializeSnapshot` already projects real MercenaryData (archive FEATURE-09) — the save
  fan-out is the persistence path.
