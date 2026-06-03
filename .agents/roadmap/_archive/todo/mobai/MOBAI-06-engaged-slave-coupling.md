# MOBAI-06 — Run the slave-coupling pass for engaged slaves (player-mastered drop)

> **Epic:** Mob AI parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** MOBAI-01 (the TickSlave body + wiring) · **Blocks:** none
> **Filed by:** MOBAI-01 — per the ticket's instruction the slave branch was inserted *after* the
> engaged-target `continue`, so an engaged slave (`mob.Attack != null`, valid target) never reaches
> `ISlaveMobService.TickSlave`. The TickSlave "player-mastered slave > 5 cells from its player master
> → drop target + walk back" arm is therefore dormant (correct code, unreachable wiring).

## Problem

`MobAiService.Tick`'s target-validation block `continue`s for an engaged mob before the MOBAI-01
slave branch runs. So `TickSlave`'s **target-busy** arm (rAthena `mob_ai_sub_hard_slavemob`'s
`bl->type==BL_PC && master_dist > 5 → mob_unlocktarget + unit_walktobl`) never fires — an engaged
slave keeps fighting regardless of distance. In practice this only affects a **player-mastered mob
slave**, which no normal C# spawn path produces today (player summons go through `SummonAiService`,
mob slaves are mob-mastered) — so it is dormant, not wrong. If a future feature spawns a
player-mastered mob slave, the leash-back behavior won't work.

## Current state (C#)

- `Map.Server/Mob/MobAiService.cs` — the slave branch (`if (mob.MasterId != null) switch (TickSlave...)`)
  sits after the engaged-target `continue`; engaged slaves skip it.
- `Map.Server/Mob/Slaves/SlaveMobService.cs:TickSlave` — the `slave.TargetId != 0 && master is
  PlayerEntity && dist > 5` drop-and-return arm exists but is unreached for engaged slaves.

## rAthena reference (source of truth)

- `mob.cpp:1857` runs `mob_ai_sub_hard_slavemob` after target validation but it handles the
  target-busy case internally (it can drop the target). The C# split puts the engaged `continue`
  before the slave branch.

## Scope

- [ ] Run `TickSlave` for engaged slaves too (move/duplicate the `mob.MasterId != null` call so it
      executes before the engaged-target `continue`, or call TickSlave's target-busy arm from the
      engaged block). On a `Handled` (dropped + walking back) result, stop the attack and skip the
      engaged handling; on `Continue` (keep fighting) fall through to the normal engaged path.

## Done criteria

- A player-mastered mob slave engaged on a target, more than 5 cells from its player master, drops
  the target and walks back to within 2 cells — matching rAthena.

## Test plan

- A slave with a `PlayerEntity` master + an active target, placed > 5 cells from the master, after a
  Tick has `TargetId == 0` and is walking toward the master.
