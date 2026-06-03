# PACKET-05-mercenary — Mercenary client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** FEATURE-mercenary (MercenaryService + Intif merc RPCs exist) · **Blocks:** none

## Problem

`Map.Server/Mercenary/MercenaryService.cs` implements the mercenary surface (`Create`,
`Delete`, `ContractStop`, `Heal`, `Kills`, …) and `IIntifService` has `MercenaryCreate`,
`MercenaryRequest`, `MercenarySave`, `MercenaryDelete`. But **no client→map mercenary packet is
wired**. A player cannot issue the merc command menu (move-to-owner / stand-by / cancel-contract)
from the client.

## Current state (C#)

- No handler exists for any mercenary packet.
- `Map.Server/Mercenary/IMercenaryService.cs` — `Create(master, classId, lifetimeMs)`,
  `Delete(master, reason)`, `ContractInit`, `ContractStop(master)`, `Heal`, `Kills`,
  `SerializeSnapshot`.
- `Map.Server/Services/Intif/IIntifService.cs:95-98` — `MercenaryCreate(data)`,
  `MercenaryRequest(accountId, mercId)`, `MercenarySave(data)`, `MercenaryDelete(mercId)`.

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp`:

- `clif_parse_mercenary_action` (`CZ_MER_COMMAND`) — `<command>.B`. rAthena handler:
  - `command == 0` → `mercenary_delete(sd->md, 2)` (fire/cancel contract).
  - `command == 1` → toggle stand-by / follow (mercenary AI mode) — see `unit` mode flip.
  The function gates on `sd->md` existing and the merc being alive.

ZC responses: `ZC_MER_PROPERTY` / `ZC_MER_INIT` (merc status panel on summon),
`ZC_MER_SKILLINFO_LIST`, `ZC_MER_PAR_CHANGE` (param updates), and the delete/expire notice.
The summon-time panel is emitted on `RecvData`; this ticket only needs the command path + the
panel re-emit on relevant changes. **Read `clif_packetdb.hpp` for `CZ_MER_COMMAND` id.**

## Scope — every sub-system that must be touched

- [ ] **In packet** (`Core.Server/Packets/In/CZ/`): `CZ_MER_COMMAND` (`clif_parse_mercenary_action`)
      — fixed, `<command>.B`.
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`): ensure `ZC_MER_PROPERTY` (status panel) and
      `ZC_MER_SKILLINFO_LIST` exist for the summon path; emit a delete/expire notice on cancel.
      (If the summon path is owned by FEATURE-mercenary, only add what is missing.)
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (`CZ_MER_COMMAND` fixed-size).
- [ ] **Handler** (`Map.Server/Handlers/Mercenary/MerCommandHandler.cs`):
  - [ ] Gate: session spawned, master has a live mercenary.
  - [ ] `command == 0` → `IMercenaryService.Delete(master, reason: 2)` →
        `IIntifService.MercenaryDelete`; emit the expire/delete notice.
  - [ ] `command == 1` → flip the merc AI/follow mode (stand-by ↔ follow) via the unit/AI state.
- [ ] No new char-side RPC — merc persistence RPCs exist.

## Done criteria

- Sending `CZ_MER_COMMAND` with command 0 fires the mercenary (contract cancelled), despawns it,
  and persists the deletion; command 1 toggles follow/stand-by.
- Command is ignored (no crash) when the player has no mercenary or it is dead — matches the
  rAthena `clif_parse_mercenary_action` gate.
- No stub, no `// TODO`.

## Test plan

- Handler test: command 0 with a live merc calls `Delete(master, 2)` + `MercenaryDelete`; command
  with no merc is a no-op; command 1 flips the mode flag.
- Manual: summon a merc (via merc scroll), open its command window, cancel the contract.

## Notes / gotchas

- Verify the exact `command` byte meaning against the current `clif_parse_mercenary_action` body —
  some PACKETVERs add a third command. Mirror whatever the checkout has.
- The mercenary is a separate owned `Entity`; the command targets `sd->md`, resolve via the
  master→merc link.
