# GP-PET — Pet works end-to-end

> **Epic:** gameplay · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SCR-DOMAIN (pet builtins)

## The deliverable

> A player can **tame a monster, hatch the egg into a live pet that follows + fights,
> feed it (intimacy/hunger), rename it, and return it to an egg** — live client, surviving
> logout with the pet's intimacy/hunger/name intact.

## Player story

Pets are an iconic feature. The *service* lands catch-roll, egg create, hatch, and a live
`PetEntity` in the world (archive FEATURE-07), but no client packet reaches it (capture/hatch/
feed/rename/return), the pet doesn't fight or pick loot, and the pet_id↔egg-card binding +
persistence round-trip are incomplete.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Data | ✅ | `pet_db` seeded (egg item → mob class) |
| Service | ✅ verify | `Map.Server/Pet/PetOps/PetOpsService.cs` — CreateEgg/EggSearch/GetEgg/BirthProcess + catch roll (archive FEATURE-07) |
| Live entity | ✅ verify | `PetEntity` spawned + AOI visibility |
| Persistence | partial | pet_id↔egg-card binding + create round-trip incomplete (archive FEATURE-27) |
| Combat/loot | ❌ | auto-skill dispatch + loot bag missing (archive FEATURE-28) |
| CZ handlers | ❌ | capture/hatch/feed/rename/return/emotion/equip missing |
| ZC emits | ❌ | pet info, intimacy/hunger update, emotion missing |

## rAthena reference

- `rathena/src/map/pet.cpp` — `pet_catch_process` (tame roll), `pet_create_egg`,
  `pet_birth_process` (hatch), `pet_hungry`/`pet_food` (hunger+intimacy), `pet_change_name`,
  `pet_return_egg`, `pet_ai_sub_hard` (follow + attack), `pet_lootitem`, `pet_unequipitem`.
- `rathena/src/map/clif.cpp` — parse `CZ_TRYCAPTURE_MONSTER` (0x019f), `CZ_SELECT_PETMENU`
  (hatch/feed/rename/return/equip), `CZ_PET_ACT`/emotion; emit `clif_pet_roulette`,
  `clif_sendegg`, `clif_send_petdata` (intimacy/hunger/name), `clif_pet_emotion`.
- `char/inter_pet`/`intif_save_petdata` — persistence.

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Pet save IPC — `IntifService.PetSave` exists; wire it (avoid the DI cycle: companion-save
  dispatch rides the save fan-out, not an injected `IIntifService` into PetOpsService).

## Scope — every layer

- [~] **CZ handlers**: pet-menu (`CZ_COMMAND_PET` 0x01a1 → `PetMenuHandler` → `Menu`, turn 1);
      try-capture (`CZ_TRYCAPTURE_MONSTER` 0x019f → `PetCaptureHandler` → `CatchProcessEnd`, turn 2).
      Remaining: hatch (egg-use → `BirthProcess`), rename (`CZ_RENAME_PET`), select-egg
      (`CZ_SELECT_PETEGG`), pet emotion (`CZ_PET_ACT`).
- [~] **Service**: feed → intimacy/hunger + emit (turn 1); `Menu` corrected to rAthena mapping
      (0=info/1=feed/2=performance/3=return/4=unequip — was wrong) + runaway gate. Remaining: verify
      catch/hatch at HEAD; bind the hatched pet to a pet_id stored on the egg card (archive FEATURE-27);
      rename.
- [ ] **Combat/loot**: pet AI auto-skill dispatch + loot pickup bag (archive FEATURE-28).
- [ ] **Persistence**: pet row (class/name/intimacy/hunger/equip) load on hatch / save on
      mutate + logout; egg card carries pet_id across logout.
- [~] **ZC emits**: pet status (`ZC_PROPERTY_PET` 0x01a2) + pet data
      (`ZC_CHANGESTATE_PET` 0x01a4: intimacy/hunger/accessory/performance) via new `IPetClientService`,
      wired into `Summon`/`Food`/`SetIntimate`/`Menu` (turn 1); capture cursor (`ZC_START_CAPTURE`
      0x019e) + roulette (`ZC_TRYCAPTURE_MONSTER` 0x01a0) (turn 2). Remaining: send-egg, emotion
      (`ZC_PET_ACT`).

## Done criteria

- Player tames a Poring (roll), gets an egg, hatches it → a live Poring pet follows + attacks
  the player's target; feeding raises intimacy + hunger; renaming sticks; returning makes the
  egg again.
- Relog → the same pet (intimacy/hunger/name) re-hatches from the egg; loyalty bonus applies.
- No pet CZ handler / ZC emit missing.

## Test plan

- Handler tests: capture / pet-menu → service.
- Service: catch roll, hatch binding, feed intimacy/hunger curve, return.
- Persistence round-trip (hatch → mutate → reload → equal).
- Live: tame → hatch → feed → rename → return.

## Progress log (multi-turn vertical)

- **2026-06-04 (turn 1)** — Pet-menu client bridge. New pet packets `CZ_COMMAND_PET` (0x01a1, 3B
  `<type>.B`), `ZC_PROPERTY_PET` (0x01a2, 37B status panel: name/renamed/level/hunger/intimacy/
  accessory/class), `ZC_CHANGESTATE_PET` (0x01a4, 11B `<type>.B <GID>.L <data>.L`, `PetDataType`
  enum = rAthena `e_changestate_pet`). New `IPetClientService`/`PetClientService` emit hub (mirrors
  `IPartyClientService`, routes via `ISessionManagerAccessor`) with `SendPetStatus` +
  `SendPetData`. `PetMenuHandler` → `PetOpsService.Menu`. **Fixed a parity bug**: `Menu`'s mapping
  was wrong (was 0=feed/1=rename/2=return/3=unequip); corrected to rAthena `pet_menu` (pet.cpp:1422):
  0=info(send status)/1=feed/2=performance/3=return-to-egg/4=unequip, with the
  `intimate <= PET_INTIMATE_NONE` runaway gate (returns 1). Emits wired: `Summon` →
  `clif_send_petdata(INIT)` + `clif_send_petstatus`; `Food` → HUNGER + INTIMACY changestate;
  `SetIntimate` → INTIMACY; `Menu` info/performance/unequip. Added `PetEntity.RenameFlag` (drives the
  status "modified" byte) + made `PetName` settable. `PetMenuEmitTests` (6: status panel offsets,
  feed hunger+intimacy changestate, return→recall, unequip, runaway-reject, handler routing); full
  suite 4450 pass (1 = standing replay-fixture).
- **2026-06-04 (turn 2)** — Capture flow. New packets `ZC_START_CAPTURE` (0x019e, header-only cursor),
  `CZ_TRYCAPTURE_MONSTER` (0x019f, `<target id>.L`), `ZC_TRYCAPTURE_MONSTER` (0x01a0, `<result>.B`
  roulette). `IPetClientService` gained `SendCatchProcess` + `SendPetRoulette`. **Parity fix**: the
  capture was wrongly modelled as a mob-DEATH event (death-time 2·capture rate via `MobDeathObserver`);
  rewrote `CatchProcessEnd(master, EntityId)` to the real `pet_catch_process_end` (pet.cpp:1241) — the
  player clicks a LIVE mob, gating on armed/alive/tameable/class-match/distance (Chebyshev ≤ 5) and
  rolling the non-legacy rate `capture + ((100−hp%)·capture)/100` (≥1) against the mob's current HP%;
  on success removes the mob (`NotifyVanishedToArea` + `entities.Remove`) + roulette(true) +
  `intif PetCreate`, on fail roulette(false). `CatchProcessStart` now emits `ZC_START_CAPTURE`. New
  `PetCaptureHandler` (`CZ_TRYCAPTURE_MONSTER`). Removed the death-based hook + its 2 obsolete tests
  from `MobDeathObserver` (dropped the now-unused `IPetOpsService` dep). Injected `IVisibilityService`
  into `PetOpsService`. `PetCaptureTests` (8: arm/cursor, success-removes-mob+egg, roll-fail-keeps-mob,
  HP%-raises-rate, not-armed/wrong-class/out-of-range gates, handler routing); full suite 4456 pass
  (1 = standing replay-fixture). **Filed GP-PET-CATCH-GATES** (nopetcapture mapflag + hide-check +
  inventory-blank guards — needs subsystems not yet present).
- **Remaining (next turns → done):** hatch (egg item-use → `BirthProcess` + status emit), rename
  (`CZ_RENAME_PET` → char-side uniqueness), select-egg + emotion CZ/ZC, pet combat/loot (FEATURE-28),
  persistence binding pet_id↔egg-card + round-trip (FEATURE-27). The loop resumes this card.

## Notes / gotchas

- The egg→pet-class index is lazy `pet_db EggItem→MobAegis→class` (archive FEATURE-07).
- Loyalty intimacy thresholds gate the stat bonus — match `pet_db` intimacy bands.
- Pet GID on the wire = `PetEntity.Id.Value` (same id the AOI spawn packet uses).
