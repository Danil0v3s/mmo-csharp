# PACKET-09-instance — Instance / memorial dungeon client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-instance (InstanceService exists) · **Blocks:** none

## Problem

`Map.Server/Instance/InstanceService.cs` implements the instance surface (`Create`, `Destroy`,
`DestroyCommand`, `ReqInfo`, `Enter`, `AddUsers`/`DelUsers`, idle/keep timers, `GenerateMapName`).
But **no client→map instance packet is wired**. A player at a memorial-dungeon NPC cannot see
the instance status (time remaining), and the client's "destroy instance" button does nothing.

## Current state (C#)

- No handler exists for any instance packet.
- `Map.Server/Instance/IInstanceService.cs` — `Create(dbId, ownerId, mode)`, `Destroy(instanceId)`,
  `DestroyCommand(pc, instanceId)`, `ReqInfo(pc, instanceId)`, `Enter(pc, instanceId)`,
  `GetOwner(instanceId)`, `StartIdleTimer` / `StopIdleTimer` / `StartKeepTimer`.
- Instance **creation/entry** is normally driven by NPC scripts (`instance_create`,
  `instance_enter` script commands), not a client packet — so most lifecycle is server-driven.
  The client-side packet surface is mainly the **status/info window** and the **destroy command**.

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp`:

- `clif_parse_MemorialDungeonCommand` (`clif.cpp:18854`) → the memorial-dungeon UI command. In
  rAthena this currently maps to the "return / destroy" action → `instance_destroy` on the party/
  guild instance the player belongs to. Read the function body for the exact command semantics in
  this checkout (it is short).
- Server→client info emitters (the ZC side, in `clif.cpp`):
  - `clif_instance_create` → `ZC_INSTANCE_CREATE` — sent when an instance is created.
  - `clif_instance_changewait` → `ZC_INSTANCE_STATE`/wait — queue position.
  - `clif_instance_status` → `ZC_INSTANCE_INFO` — name + remaining/elapsed time (the status window).
  - `clif_instance_changestatus` → `ZC_INSTANCE_STATE` — live time update.
  - `clif_instance_changewait` — busy/wait flag.

These ZC emitters are driven by `InstanceService` lifecycle events (create / timer tick / destroy),
**not** by a client request — so the bridge here is: (1) the `CZ` destroy/command parse handler, and
(2) wiring `InstanceService` to emit the ZC info/state packets at the right lifecycle points.
**Read `clif_packetdb.hpp` for the `ZC_INSTANCE_*` ids and the memorial-dungeon CZ id.**

## Scope — every sub-system that must be touched

- [ ] **In packet** (`Core.Server/Packets/In/CZ/`): `CZ_MEMORIALDUNGEON_COMMAND`
      (`clif_parse_MemorialDungeonCommand`) — `<command>.L` (or `<command>.B`; confirm width).
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`):
  - [ ] `ZC_INSTANCE_CREATE` — created notice (name).
  - [ ] `ZC_INSTANCE_INFO` — status window: `<name>.61B <type>.L <remaining>.L <elapsed>.L` (confirm).
  - [ ] `ZC_INSTANCE_STATE` — live state/time update.
  - [ ] `ZC_INSTANCE_DELETE` (if a separate destroy-notice exists) / wait-state.
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (instance info is var/fixed per id).
- [ ] **Handler** (`Map.Server/Handlers/Instance/MemorialDungeonCommandHandler.cs`):
  - [ ] Resolve the player's active instance (party/guild/owner link).
  - [ ] On the destroy command → `IInstanceService.DestroyCommand(pc, instanceId)`.
  - [ ] Gate: only the instance owner (or party leader) may destroy — mirror rAthena.
- [ ] **Lifecycle wiring** (`Map.Server/Instance/InstanceService.cs` consumers): emit
  - [ ] `ZC_INSTANCE_CREATE` on `Create`.
  - [ ] `ZC_INSTANCE_INFO` on `ReqInfo` and to members on join.
  - [ ] `ZC_INSTANCE_STATE` on the idle/keep timer tick so the client clock stays in sync.
  - [ ] destroy notice on `Destroy`.
      Add a thin `IInstanceClientService` (or extend `IClifWireService`) so the service can push
      these without depending on the handler. Match the rAthena `clif_instance_*` call sites.
- [ ] No new char-side RPC — instances are not persisted via the char server here.

## Done criteria

- The instance status window shows the correct name and a counting-down timer that matches the
  keep/idle timer remaining seconds (parity with `clif_instance_status`).
- Pressing the memorial-dungeon destroy command destroys the player's instance (owner/leader gated;
  non-owner request is rejected), despawns its maps/NPCs, and frees the instance id.
- `ZC_INSTANCE_CREATE` fires on creation and `ZC_INSTANCE_STATE` updates on timer ticks.
- No stub, no `// TODO`.

## Test plan

- Handler test: destroy command by non-owner → rejected; by owner → `DestroyCommand` called with
  the resolved instance id.
- Service test: timer tick emits `ZC_INSTANCE_STATE` with the correct remaining seconds.
- Manual: enter a memorial dungeon (script-created), observe the status window timer, destroy it.

## Notes / gotchas

- Instance **creation and entry are script-driven** (`instance_create` / `instance_enter` NPC
  commands), not a client packet. Do not add a "client creates instance" packet — that is not how
  rAthena works. The client packet surface is only the status window + destroy/command.
- The destroy command resolves the instance via the player's party/guild/owner mode (instance
  `mode`: character / party / guild / clan) — resolve the same way `DestroyCommand` expects.
- The status-window time fields are seconds; keep them in sync with the actual keep/idle timer, do
  not recompute independently.
