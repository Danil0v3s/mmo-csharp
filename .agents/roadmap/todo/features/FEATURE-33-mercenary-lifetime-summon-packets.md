# FEATURE-33 — Mercenary lifetime expiry + summon callsite + kill-bonus trigger + packets

> **Epic:** Gameplay-Companion · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-09 (live merc entity) · **Blocks:** none

## Problem

FEATURE-09 made the merc a real entity, but several wiring links remain:

1. **Lifetime expiry** — the contract `ContractEnd` is stored (+ on `MercenaryEntity.ContractEndTick`)
   but nothing fires when it passes; a merc never expires automatically.
2. **Summon callsite** — `MercenaryService.Create` has **no caller** (rAthena's merc-scroll item
   script `mercenary_create`); a player can't summon a merc.
3. **Kill-bonus trigger** — `Kills(master)` (faith/calls on the master's kill) exists but the
   FEATURE-01 mob-death observer doesn't call it.
4. **mercId round-trip** — `Create` doesn't yet receive the char-assigned `merc_id` back (so
   `SerializeSnapshot` keys off a 0 id until a save happens).
5. **Client packets** — ZC_MER_INIT / ZC_MER_PROPERTY / ZC_MER_SKILLINFO_LIST / lifetime bar.

## Current state (C#)

- `Map.Server/Mercenary/MercenaryService.cs` — `Create`/`RecvData` spawn the entity; `ContractStop`
  despawns on demand; `SerializeSnapshot` projects the live merc by id; `Save` is a FEATURE-17 seam.
- No `_merc.Tick` in the game loop; no merc-scroll handler; the FEATURE-01 observer doesn't call `Kills`.

## rAthena reference

- `rathena/src/map/mercenary.cpp` — `mercenary_create` (scroll), the lifetime timer →
  `mercenary_contract_stop`, `mercenary_kills` on the master's kill, `clif_mercenary_info`.

## Scope

- [ ] Lifetime expiry: a per-tick sweep (`IMercenaryService.Tick`) that despawns + deletes mercs whose
      `ContractEnd` has passed; hook into the game loop.
- [ ] Summon callsite: the merc-scroll item-use path calls `Create`.
- [ ] FEATURE-01 observer calls `MercenaryService.Kills(master)` on the master's kill.
- [ ] Bind the char-assigned `merc_id` back into the live record on the create response.
- [ ] Emit the merc client packets at the spawn/HP/skill/lifetime seams left by FEATURE-09.

## Done criteria

- A merc summoned from a scroll expires automatically at contract end; the master's kills accrue
  faith/calls; the client shows the merc HP + lifetime bar.

## Test plan

- `MercenarySpawnTests` — lifetime tick despawns an expired merc; observer→Kills increments faith.
