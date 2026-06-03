# FEATURE-07 — Pet lifecycle

> **Epic:** Gameplay-Companion · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-01 (catch roll on mob death), FEATURE-02 (pet save) · **Blocks:** none
> **Related:** PACKET-* (pet UI / egg / intimacy packets)

## Problem

Pets half-exist. Summon/recall + hunger decay work and a `PetEntity` spawns
into the world, but the **egg→hatch→catch→persist loop is broken**: `CreateEgg`
only logs, `CatchProcessEnd` never rolls the catch nor creates an egg
("for now we clear the target marker"), `BirthProcess` never actually hatches,
and no pet client packets are emitted. Evolution / autofeed / loot / pet-skill
bodies exist but are unreachable. A player cannot tame a monster.

## Current state (C#)

- `Map.Server/Pet/PetService.cs` — `Summon` (`:47`, real spawn into registry + visibility), `Recall` (`:70`), `Tick` (`:81`, hunger/intimacy decay, runaway at 0), `SerializeSnapshot` (`:115`, real). Hunger decay wired in the loop (`MapServerImpl.cs:309 _pet.Tick`).
- `Map.Server/Pet/PetOps/PetOpsService.cs`:
  - `:71 CreateEgg(master, itemId)` — *log only*, returns true. No `IntifService.PetCreate`.
  - `:341 CatchProcessEnd(master, targetMobClass)` — clears `PetCatchTargetClass = -1`, logs. *"roll the catch chance ... For now we clear the target marker — the success path is handled by the mob-death observer"* (which doesn't exist → FEATURE-01).
  - `:115 BirthProcess(master)` — clears the selected egg slot, *"Real Summon happens in the item-use handler"* — does not hatch.
  - `:128 RecvPetData`, `:80 GetEgg`, `:99 EggSearch` (returns -1), `:107 SelectEgg` — partial/seam.
  - Working bodies (reachable once triggered): `Food` (`:138`), `SetIntimate` (`:162`), `Evolution` (`:211`), `Menu` (`:265`), `ChangeName`/`ChangeNameAck` (`:237`/`:247`), `AttackSkill` (`:173`, returns 0 = no cast), `TargetCheck` (`:182`), autobonus (`:308`–`:330`).
- `Map.Server/Services/Intif/IntifService.cs`: real `PetCreate` (`:513`), `RequestPetInfo` (`:537`), `SavePet` (`:550`), `DeletePet` (`:567`) — orphaned.
- `pet_db` catalog loads (`PetOpsService.Reload :354`, `GetCatalogEntry :373`).

## rAthena reference (source of truth)

- `rathena/src/map/pet.cpp`:
  - `pet_catch_process_start(sd, target_id, item_id)` — arm catch: set `sd->catch_target_class`, `clif_pet_roulette` / catch UI start.
  - `pet_catch_process2(sd, target_id)` — on the catch attempt resolution: read `pet_db[class]->capture` rate, roll; success → `intif_create_pet(account_id, char_id, class, pet_db lv, egg_id, 0, intimate, hungry, ...)`; failure → `clif_pet_roulette(sd, 0)` and clear target. (FEATURE-01 calls this on the killing blow during a catch.)
  - `pet_create_egg(sd, item_id)` — from a hatchable egg item: `intif_create_pet(...)` to insert the pet row (incubated egg in inventory).
  - `pet_get_egg(account_id, pet_class, pet_id)` — char returned the new pet_id; grant the egg item to inventory with the pet_id bound to the item's card slots.
  - `pet_birth_process(sd, pd)` / hatch — egg item used → spawn the live pet (`pet_data_init`), `clif_spawn`, `clif_send_petdata`, start the hunger timer.
  - `pet_data_init` — sets intimacy/hunger from the saved row, computes battle status from `pet_db` `Status`.
  - `clif_send_petdata` / `clif_pet_emotion` / `clif_pet_food` — pet client packets.

## Scope — every sub-system that must be touched

- [ ] `PetOpsService.CreateEgg` — call `IntifService.PetCreate(master, classId, nameId, rename, eggItemId, intimate, hungry, gender, petName)` to insert the pet row; on the char-side `pet_get_egg` response, grant the egg item bound to the pet_id.
- [ ] `PetOpsService.CatchProcessEnd` — **implement the real roll** (called from FEATURE-01 observer on the killing blow when armed): read `pet_db[mobClass].CaptureRate`, roll `rng(10000) < rate`; success → `IntifService.PetCreate(...)` (egg created); failure → emit catch-fail clif + clear marker. Remove the "for now we clear the target marker" no-op.
- [ ] `PetOpsService.BirthProcess` / hatch — egg item used: resolve the pet class + saved row, `IPetService.Summon(...)` the live pet, emit `clif_send_petdata`, start hunger timer.
- [ ] `PetOpsService.GetEgg` — grant egg item bound to pet_id (inventory).
- [ ] `PetOpsService.EggSearch` — real inventory scan for the egg item id (currently `-1`); inject the inventory service.
- [ ] `PetOpsService.RecvPetData` — bind the char-hydrated pet row to the master (login / hatch) via `IPetService.Summon`.
- [ ] `AttackSkill` / pet-skill — once `pet_db` `AttackRate`/`SupportSkill` rows are loaded, roll + dispatch the pet skill (currently always 0); route through the skill engine.
- [ ] `LootItemDrop` — model the pet loot bag (currently log-only) if `pet_db` `Loot` is set; drop accumulated loot on vaporize/rename.
- [ ] **Client packets**: ZC_PROPERTY_PET / ZC_PET_ACT / ZC_FEED_PET / ZC_PET_EVOLUTION_RESULT / catch UI (ZC_START_CAPTURE etc.). Define in `Core.Server/Packets` or use PACKET-* seam; pet **state mutation must occur here**.
- [ ] **Save**: `IntifService.SavePet` via FEATURE-02 fan-out + at hatch/vaporize.

## Done criteria

- Arming a catch (`CatchProcessStart`) then landing the killing blow on the target rolls catch at the `pet_db.CaptureRate` probability; success creates an egg (char-side row + inventory egg item), failure clears the marker and notifies the client.
- Using a pet egg hatches a live `PetEntity` into the world with the saved intimacy/hunger and emits pet data to the client.
- `CreateEgg` / `GetEgg` / `BirthProcess` / `RecvPetData` all reach `IntifService.PetCreate`/`Summon` — no log-only stub.
- Pet hunger decay + runaway still work (unchanged from current `PetService.Tick`).
- Pet save persists hunger/intimacy/level across relog (FEATURE-02).

## Test plan

- `Map.Server.Tests` (extend `PetServiceTests`):
  - catch roll: forced-pass RNG → `IntifService.PetCreate` called once; forced-fail → marker cleared, no create;
  - hatch: egg use → `PetEntity` spawned + petdata emit;
  - `CreateEgg` calls `IntifService.PetCreate` with the right args.
- Integration with FEATURE-01 (catch on death) + FEATURE-02 (save).
- Manual/live: buy a hunting net (or use a taming item), catch a Poring, hatch it, feed it, confirm it follows + persists across relog.

## Egg ⇄ pet binding (the loop that's broken)

```
catch armed (CatchProcessStart) → killing blow on target (FEATURE-01 observer)
  → CatchProcessEnd: roll rng(10000) < pet_db[mob].CaptureRate
     success → IntifService.PetCreate → char inserts pet row, returns pet_id
        → pet_get_egg: grant egg item bound to pet_id (card slots carry pet_id)
     fail → clif catch-fail; clear PetCatchTargetClass
egg item used → BirthProcess: resolve class from egg's bound pet_id row
  → IPetService.Summon(master, class, name) spawns PetEntity
  → clif_send_petdata; start hunger timer (PetService.Tick already running)
return-to-egg (ReturnEgg :89) → Recall + repack egg item
```

Currently the chain is severed at three points: `CatchProcessEnd` (no roll), `CreateEgg`/`GetEgg` (log only), `BirthProcess` (no hatch).

## Notes / gotchas

- The catch resolution timing is rAthena's: catch is decided on the **killing blow** during an armed attempt, hence the FEATURE-01 observer dependency. `CatchProcessEnd` (`PetOpsService.cs:341`) is the callback the observer invokes.
- `PetEntity.PetName` is init-only — rename currently recalls + re-summons (`ChangeNameAck :247`). Keep that approach unless you make the field mutable.
- `pet_db.CaptureRate` field: confirm the column name on `PetDbEntity` (loaded in `Reload` :354, keyed by `MobAegis`); add it to the loader if absent.
- One live pet per owner is enforced in `Summon` (`PetService.cs:49`) — preserve.
- `PetCatchTargetClass` is reused as both the catch target and the "selected egg slot" marker (`SelectEgg :111`) — be careful not to clobber one with the other; consider separate fields if the reuse causes ambiguity.
- `IntifService.PetCreate` (`:513`) already takes the full arg set (class, nameId, rename, eggItemId, intimate, hungry, gender, petName) and dispatches `PetCreateAsync` — `CreateEgg`/`CatchProcessEnd` just need to call it.
