# PACKET-03-pet — Pet client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-pet (PetService / PetOpsService + Intif pet RPCs exist) · **Blocks:** none

## Problem

`Map.Server/Pet/PetService.cs` and `Map.Server/Pet/PetOps/PetOpsService.cs` implement the full
`pet.cpp` surface (`CatchProcessStart/End`, `GetEgg`, `SelectEgg`, `ReturnEgg`, `Food`,
`ChangeName`, `Menu`, `EquipItem`, `AttackSkill`, `Evolution`, …) and `IIntifService` has
`PetCreate`, `RequestPetInfo`, `SavePet`, `DeletePet`. But **no client→map pet packet is wired**.
A player cannot capture a monster, hatch an egg, feed/rename/return their pet, trigger pet
performance/emotion, or equip a pet accessory.

## Current state (C#)

- No handler exists for any pet packet (`Map.Server/Handlers/` has no `Pet/` folder).
- `Map.Server/Pet/PetOps/IPetOpsService.cs` — `CatchProcessStart(master, targetMobClass)`,
  `CatchProcessEnd(master, targetMobClass)`, `GetEgg(master, classId, itemId, gender)`,
  `SelectEgg(master, eggIndex)`, `ReturnEgg(master)`, `Food(master)`, `ChangeName(master, newName)`,
  `ChangeNameAck(master, flag)`, `Menu(master, choice)`, `EquipItem(master, inventoryIndex)`,
  `AttackSkill(master, targetId)`, `Evolution(master, evoTo)`.
- `Map.Server/Pet/IPetService.cs` — `Summon`, `Recall`, `SerializeSnapshot`.
- `Map.Server/Services/Intif/IIntifService.cs:87-90` — `PetCreate(master, classId, nameId, rename,
  eggItemId, intimate, hungry, gender, petName)`, `RequestPetInfo`, `SavePet`, `DeletePet`.

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp` parse functions to port:

- `clif_parse_CatchPet` → `pet_catch_process_start` (begin capture; sent after a taming-item use
  set the catch state) → on success `pet_catch_process_end`.
- `clif_parse_SelectEgg` → `pet_select_egg` (`pet.cpp` `SelectEgg`) — hatch from inventory egg index.
- `clif_parse_PetMenu` (`pet.cpp` `Menu`) → choices: 0=info, 1=feed, 2=performance,
  3=return-to-egg, 4=unequip-accessory.
- `clif_parse_ChangePetName` → `pet_change_name` (`ChangeName`).
- `clif_parse_SendEmotion` — pet emotion (pet performance/emote on the pet entity).
- Pet accessory equip: triggered by using the accessory item; the equip path is
  `pet_equipitem` (`EquipItem`) — confirm whether a dedicated CZ packet or the normal
  `CZ_USE_ITEM` path drives it in the target PACKETVER.

ZC responses: `ZC_PROPERTY_PET` (pet status panel: name/level/hungry/intimacy/accessory),
`ZC_FEED_PET` (feed result + intimacy delta), `ZC_PET_ACT` (performance/emotion broadcast),
`ZC_CHANGE_PETNAME` / rename ack, `ZC_TRYCAPTURE_MONSTER` / capture-result, `ZC_PET_EVOLUTION_RESULT`.

**Read `clif_packetdb.hpp`** for numeric ids (taming/menu/select-egg are PACKETVER-shuffled).

## Scope — every sub-system that must be touched

- [ ] **In packets** (`Core.Server/Packets/In/CZ/`):
  - [ ] `CZ_TRYCAPTURE` (`clif_parse_CatchPet`) — `<target_id>.L`.
  - [ ] `CZ_SELECT_PETEGG` (`clif_parse_SelectEgg`) — `<egg_index>.W`.
  - [ ] `CZ_COMMAND_PET` / `CZ_PET_ACT` (`clif_parse_PetMenu`) — `<menu>.B` (feed/perform/return/unequip).
  - [ ] `CZ_RENAME_PET` (`clif_parse_ChangePetName`) — `<name>.24B`.
  - [ ] `CZ_REQ_PET_EMOTION` (`clif_parse_SendEmotion`) — `<emotion>.L` (if separate from menu).
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`): `ZC_PROPERTY_PET`, `ZC_FEED_PET`,
      `ZC_PET_ACT`, `ZC_CHANGE_PETNAME` (or name-ack), `ZC_TRYCAPTURE_MONSTER` result,
      `ZC_PET_EVOLUTION_RESULT` (if evolution exposed via menu).
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (rename is name-bearing).
- [ ] **Handlers** (`Map.Server/Handlers/Pet/`):
  - [ ] `PetCaptureHandler` → `IPetOpsService.CatchProcessStart` then `CatchProcessEnd`; on success
        `IIntifService.PetCreate`. Emit capture-result ZC.
  - [ ] `PetSelectEggHandler` → `IPetOpsService.SelectEgg`; hatch → `IPetService.Summon` +
        `ZC_PROPERTY_PET`.
  - [ ] `PetMenuHandler` → dispatch on menu byte: feed→`Food` + `ZC_FEED_PET`; perform→`ZC_PET_ACT`;
        return-to-egg→`ReturnEgg` + `IIntifService.SavePet`; unequip→`EquipItem`(remove).
  - [ ] `PetRenameHandler` → `IPetOpsService.ChangeName` + `ChangeNameAck`.
  - [ ] `PetEquipAccessoryHandler` (or fold into UseItem) → `IPetOpsService.EquipItem`.
- [ ] No new char-side RPC — `PetCreate`/`SavePet`/`DeletePet` exist.

## Done criteria

- Using a taming item then sending capture against a valid mob class either creates the pet egg
  (intimacy/hungry seeded from pet DB) or fails per rAthena `pet_catch_process_end` rules.
- Hatching an egg summons the pet and shows the status panel; feeding updates intimacy/hunger and
  the panel; renaming (once) sets the name and rejects a second rename (rAthena `rename` flag).
- Return-to-egg despawns the pet and saves; performance plays the `ZC_PET_ACT` animation.
- No stub, no `// TODO`.

## Test plan

- Handler tests pinning: capture against invalid class fails; second rename rejected; feed when
  not hungry caps intimacy correctly; return-to-egg calls `SavePet`.
- Manual: tame → hatch → feed → rename → return-to-egg cycle on a live client.

## Notes / gotchas

- Capture is two-phase: the taming-item *use* (normal `CZ_USE_ITEM`) sets `sd`'s catch state and
  the *target id* arrives in `CZ_TRYCAPTURE`. Don't try to capture without the prior item use.
- `Menu` choices differ slightly by client; verify the byte→action map against `pet.cpp` `Menu`.
- Rename is one-shot unless the pet DB allows re-rename; mirror the `rename` flag in `PetCreate`.
