# GP-PET-LOGIN-RESUMMON — a pet that was out at logout re-appears on login

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> If a player logs out with a pet active (not returned to its egg), the same pet re-appears
> automatically on the next login — matching rAthena's `status.pet_id` auto-hatch in `pc_authok`.

## Player story / why it matters

GP-PET completes the **egg** round-trip: a returned pet sits in the bag as a bound egg (CARD0_PET +
pet_id) and re-hatches into the saved pet when used. But rAthena also supports the case where the pet
is **still out** at logout: the char keeps `status.pet_id`, and on login `pc_authok` →
`intif_request_petdata` re-spawns the saved pet directly (no egg, no hatch UI). The C# side has no
login-time pet auto-load, so a pet left out at logout silently disappears on relog (the player has to
have returned it to an egg first).

This is the "pet out at logout" half of "survives logout"; the egg-hatch half is done.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Egg-hatch load | ✅ | `Map.Server/Pet/PetOps/PetOpsService.cs` `BirthProcess` → `PetLoadAsync` → hydrate |
| Char pet_id on the enter snapshot | ☐ | `CharacterDataResponse` carries no current `pet_id`/incubate state |
| Login auto-resummon | ☐ | nothing calls `PetLoadAsync` + `Summon` at map enter |

## rAthena reference

- `rathena/src/map/pc.cpp` `pc_authok` / `rathena/src/map/clif.cpp` LoadEndAck — when `sd->status.pet_id`
  is set, `intif_request_petdata` is issued and the pet re-spawns on the player's map.

## Dependencies — and how to satisfy

- The char→map enter snapshot (`CharacterDataResponse`) needs the player's current `pet_id` (+ the
  pet being "out", not incubated). Extend the proto + the char-side fill, then auto-load on enter
  (mirrors the GP-QUEST load-on-enter wiring in `NotifyActorInitHandler`).

## Scope — every layer

- [ ] Add `pet_id` (current active pet) to the char enter snapshot.
- [ ] On map enter (LoadEndAck), if the char has an active pet_id, `PetLoadAsync` → `Summon` with the
      saved state (re-using the FEATURE-27 load path).

## Done criteria

- Log out with a pet following you → log back in → the same pet (intimacy/hunger/name) is following you
  again, without touching the egg.

## Test plan

- Service/handler test: enter with an active pet_id → `PetLoadAsync` + `Summon` with the loaded state.

## Notes

- Filed by GP-PET (turn 7). The egg round-trip (return → relog → re-hatch from the egg) is done; this is
  the still-out-at-logout path.
