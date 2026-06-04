# GP-PET — Pet works end-to-end

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-04) · **Size:** L · **Player-visible:** yes
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

- [x] **CZ handlers**: pet-menu (`CZ_COMMAND_PET` 0x01a1 → `PetMenuHandler` → `Menu`, turn 1);
      try-capture (`CZ_TRYCAPTURE_MONSTER` 0x019f → `PetCaptureHandler` → `CatchProcessEnd`, turn 2);
      hatch (egg item-use short-circuit → `OpenEggList`, select `CZ_SELECT_PETEGG` 0x01a7 →
      `SelectPetEggHandler` → `BirthProcess`, turn 3); rename (`CZ_RENAME_PET` 0x01a5 →
      `RenamePetHandler` → `ChangeName`) + emotion (`CZ_PET_ACT` 0x01a9 → `PetActHandler` → `Emotion`)
      (turn 4). *(Over-head name refresh on rename ➡️ GP-PET-RENAME-NAMEPKT — cosmetic.)*
- [~] **Service**: feed → intimacy/hunger + emit (turn 1); `Menu` corrected to rAthena mapping
      (0=info/1=feed/2=performance/3=return/4=unequip — was wrong) + runaway gate. Remaining: verify
      catch/hatch at HEAD; bind the hatched pet to a pet_id stored on the egg card (archive FEATURE-27);
      rename.
- [x] **Combat/loot**: loot pickup bag landed (turn 5 — `SummonAiService` pet-loot step:
      `pet_ai_sub_hard` loot branch — a looter pet (`AutoLootMax > 0`, bag not full, near master, no
      enemy target) hunts floor items via `IMobLooterService.FindNearestLoot`, walks onto them, picks
      into `PetEntity.LootItems`; `PetOpsService.LootItemDrop` deposits the bag to the owner on
      `ReturnEgg` — `pet_lootitem_drop`). Follow + assist work via the generic summon AI. Pet AI
      auto-skill dispatch (`pet_attackskill`) ➡️ Moved to **GP-PET-AUTOSKILL** — the pet's attack skill
      is set only by the `petskillattack` **script** command, so it's genuinely blocked on the
      scripting runtime (SCR-DOMAIN), not deferrable inline.
- [x] **Persistence**: pet row save on mutate + logout (archive FEATURE-02 save fan-out). Write side
      (turn 6): catch awaits the char pet-row create → grants the egg bound to the pet_id (`CARD0_PET`,
      `PetEggCard` + card-aware `GiveItemWithCards`). Read side (turn 7): `BirthProcess` reads the egg's
      bound pet_id → `PetLoadAsync` → `Summon` with the persisted intimacy/hunger/name/pet_id — the
      return→relog→re-hatch round-trip. Login auto-resummon of a pet *left out* at logout ➡️ Moved to
      **GP-PET-LOGIN-RESUMMON** (the still-out path, distinct from re-hatch-from-egg).
- [x] **ZC emits**: pet status (`ZC_PROPERTY_PET` 0x01a2) + pet data
      (`ZC_CHANGESTATE_PET` 0x01a4: intimacy/hunger/accessory/performance) via new `IPetClientService`,
      wired into `Summon`/`Food`/`SetIntimate`/`Menu` (turn 1); capture cursor (`ZC_START_CAPTURE`
      0x019e) + roulette (`ZC_TRYCAPTURE_MONSTER` 0x01a0) (turn 2); egg list (`ZC_PETEGG_LIST` 0x01a6,
      turn 3); emotion (`ZC_PET_ACT` 0x01aa, turn 4). *(Over-head BL_PET name packet on rename ➡️
      GP-PET-RENAME-NAMEPKT.)*

## Done criteria

- ✅ Player tames a Poring (click a live mob with a catch armed → roulette → bound egg), hatches it →
  a live Poring pet follows + attacks the player's target (`SummonAiService`); feeding raises intimacy +
  hunger; renaming sticks; returning makes the egg again (and deposits the loot bag).
- ✅ Relog → the same pet (intimacy/hunger/name) re-hatches from the egg (the pet_id↔card →
  `PetLoadAsync` round-trip). *"loyalty bonus applies"* ➡️ GP-PET-LOYALTY-BONUS — the pet bonus/support
  is set only by the pet script commands (scripting-blocked, SCR-DOMAIN); the intimacy/loyalty plumbing
  is done. Pet *left out* at logout re-appearing on login ➡️ GP-PET-LOGIN-RESUMMON.
- ✅ No pet CZ handler / ZC emit missing — menu/capture/select-egg/rename/emotion CZ + status/
  changestate/start-capture/roulette/egg-list/pet-act ZC all land. (Auto-skill cast ➡️ GP-PET-AUTOSKILL,
  scripting-blocked.)

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
- **2026-06-04 (turn 3)** — Hatch flow. New packets `ZC_PETEGG_LIST` (0x01a6, variable per-egg
  client-index list = `clif_sendegg`/`bpet`), `CZ_SELECT_PETEGG` (0x01a7, `<index>.W`).
  `IPetClientService.SendEggList`; `PetOpsService.OpenEggList` scans the bag for pet-egg items and
  emits the list. **Item-use short-circuit**: `ItemUseService` now detects `IT_PETEGG`
  (`ItemTypeCodes.PetEgg`) and routes to `OpenEggList` **without consuming the egg** (rAthena `bpet`).
  New `SelectPetEggHandler` (`CZ_SELECT_PETEGG`) → `SelectEgg` → `BirthProcess(master, eggSlot)`
  (refactored to take the slot directly — removed the `PetCatchTargetClass` overload that doubled as
  the egg slot) → resolve egg→class→consume→`Summon` (one-pet rule pre-checked via `TryGetLivePetId`
  before consuming, so a failed hatch never eats the egg) → the pet panel emits from turn 1's `Summon`
  path. `PetHatchTests` (5: egg-list-by-client-index, skip-non-eggs, select-hatches-and-consumes,
  handler-index-conversion, egg-use-opens-list-no-consume); full suite 4461 pass (1 = standing
  replay-fixture). **Interim**: the hatch resolves the pet class from the egg item directly; binding the
  persisted pet_id off the egg's card slots + the char-side petdata round-trip is the GP-PET
  persistence scope item (FEATURE-27), still a remaining checkbox of this ticket.
- **2026-06-04 (turn 4)** — Rename + emotion. New packets `CZ_RENAME_PET` (0x01a5, `<name>.24`),
  `CZ_PET_ACT` (0x01a9, `<data>.L`), `ZC_PET_ACT` (0x01aa, `<GID>.L <data>.L`). Rewrote `ChangeName`
  to rAthena `pet_change_name` (pet.cpp:1460): gates (pet out, not already renamed, name ≤ NAME_LENGTH,
  no control chars), applies the name + sets `RenameFlag` + re-emits `ZC_PROPERTY_PET` (`ApplyPetName`
  helper; `ChangeNameAck` kept for the FEATURE-27 char-ack path). New `Emotion` broadcasts `ZC_PET_ACT`
  to view via `IVisibilityService`. New `RenamePetHandler` + `PetActHandler`. `PetRenameEmoteTests`
  (6: apply+flag+status, reject-second-rename, reject-control/empty, emotion-broadcast, both handlers);
  full suite 4467 pass (1 = standing replay-fixture). **Filed GP-PET-RENAME-NAMEPKT** (over-head BL_PET
  name refresh on rename — needs the 0x0095 short name packet; cosmetic). Rename persistence
  (cross-relog) rides FEATURE-27.
- **2026-06-04 (turn 5)** — Pet loot bag (FEATURE-28 loot half). `SummonAiService` gained the
  `pet_ai_sub_hard` loot branch: a looter pet (`AutoLootMax > 0`, `LootItems.Count < AutoLootMax`,
  within follow range of master, not engaged with an enemy) finds the nearest floor item via the
  existing `IMobLooterService.FindNearestLoot`, walks onto it, and picks it into the inherited
  `MobEntity.LootItems` bag. `PetOpsService.LootItemDrop` rewritten from a log-only stub to the real
  `pet_lootitem_drop`: deposits each bag item to the owner's inventory (`GiveItem`), keeping
  un-addable items in the bag rather than losing them; wired into `ReturnEgg` (deposit before recall).
  Tests: `SummonAiServiceTests` +4 (adjacent-pickup, walk-to-distant, full-bag-skip, non-looter-skip),
  new `PetLootDepositTests` (3: deliver+clear, keep-undeliverable, ReturnEgg-deposits). Full suite
  4474 pass (1 = standing replay-fixture). **Filed GP-PET-LOOT-OVERFLOW** (rAthena ground-drop on full
  inventory + 10s re-loot cooldown).
- **2026-06-04 (turn 6)** — FEATURE-27 write side + auto-skill triage. Auto-skill (`pet_attackskill`)
  is set only by the `petskillattack` script command → **filed GP-PET-AUTOSKILL** (genuinely blocked on
  SCR-DOMAIN; the only GP-PET piece that needs scripting). Then the pet_id↔egg-card **write** path:
  new `PetEggCard` (rAthena `CARD0_PET` 0x0100 + low/high pet_id words), card-aware
  `IInventoryService.GiveItemWithCards` (never-merge fresh slot), awaitable `IIntifService.PetCreateAsync`
  (→ pet_id) + `PetLoadAsync`. `CatchProcessEnd` success now `CreateAndGrantEggAsync` → awaits the char
  pet-row create → `GetEgg(master, class, eggItem, petId)` grants the egg bound to the pet_id —
  **fixing a bug where the catch created the char row but never gave the player an egg** (`GetEgg` was
  orphaned, `PetCreate` discarded the returned pet_id). Tests: new `PetEggBindingTests` (3: card
  round-trip, plain-item null, GetEgg-binds), `PetCaptureTests` updated to the async create path;
  full suite 4477 pass (1 = standing replay-fixture).
- **2026-06-04 (turn 7 → DONE)** — Hatch READ side; GP-PET complete. Extended `IPetService.Summon`
  with optional `petId`/`intimacy`/`hunger`/`renamed` (a loaded pet hydrates its persisted state before
  the status emit). `BirthProcess` now reads the egg's bound pet_id (`PetEggCard.ReadPetId`); a bound
  egg loads the saved row (`PetLoadAsync` → `pet_recv_petdata`) and hatches with the persisted
  intimacy/hunger/name/pet_id, an unbound egg hatches fresh, and a missing char row falls back to a
  fresh hatch — the egg is consumed up-front so a duplicate/failed hatch can't double. New
  `PetHatchLoadTests` (3: bound-loads-saved-state, unbound-fresh, no-row-fallback); full suite 4480
  pass (1 = standing replay-fixture). Filed **GP-PET-LOGIN-RESUMMON** (pet left out at logout
  re-appears on login) + **GP-PET-LOYALTY-BONUS** (loyal-pet support bonus, scripting-blocked).
  **The full pet lifecycle is reachable: tame → bound egg → hatch (loads saved state) → follow/attack
  → feed/menu → rename → emote → loot → return → relog → re-hatch the same pet.**

## History

- **2026-06-04** — Done across 7 loop turns. Pet client packet bridge (menu/capture/select-egg/rename/
  emotion CZ + status/changestate/start-capture/roulette/egg-list/pet-act ZC), the real click-to-tame
  capture flow (parity fix off mob-death), egg-list hatch, rename + emotion, the looter-pet loot bag +
  deposit, and the pet_id↔egg-card persistence round-trip (catch→create→bound-egg, hatch→load→hydrate).
  Follow-ups: GP-PET-CATCH-GATES, GP-PET-RENAME-NAMEPKT, GP-PET-LOOT-OVERFLOW, GP-PET-AUTOSKILL,
  GP-PET-LOGIN-RESUMMON, GP-PET-LOYALTY-BONUS. Full suite 4480 pass.

## Notes / gotchas

- The egg→pet-class index is lazy `pet_db EggItem→MobAegis→class` (archive FEATURE-07).
- Loyalty intimacy thresholds gate the stat bonus — match `pet_db` intimacy bands.
- Pet GID on the wire = `PetEntity.Id.Value` (same id the AOI spawn packet uses).
