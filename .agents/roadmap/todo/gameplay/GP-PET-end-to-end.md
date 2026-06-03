# GP-PET — Pet works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
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

- [ ] **CZ handlers**: try-capture, pet-menu (hatch/feed/rename/return/equip), pet emotion.
- [ ] **Service**: verify catch/hatch at HEAD; bind the hatched pet to a pet_id stored on the
      egg card (archive FEATURE-27); feed → intimacy/hunger; rename; return-to-egg.
- [ ] **Combat/loot**: pet AI auto-skill dispatch + loot pickup bag (archive FEATURE-28).
- [ ] **Persistence**: pet row (class/name/intimacy/hunger/equip) load on hatch / save on
      mutate + logout; egg card carries pet_id across logout.
- [ ] **ZC emits**: capture roulette, send-egg, pet data (intimacy/hunger/name), emotion.

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

## Notes / gotchas

- The egg→pet-class index is lazy `pet_db EggItem→MobAegis→class` (archive FEATURE-07).
- Loyalty intimacy thresholds gate the stat bonus — match `pet_db` intimacy bands.
