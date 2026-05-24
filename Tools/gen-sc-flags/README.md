# gen-sc-flags

One-shot Python generator that reads rAthena's `db/re/status.yml`
`CalcFlags` table and emits
[`Map.Server/Status/StatusCalcFlagDefaults.cs`](../../Map.Server/Status/StatusCalcFlagDefaults.cs).

That file is consumed by
[`StatusEffectRegistry.RegisterDefaultsForMissingTypes()`](../../Map.Server/Status/StatusEffectRegistry.cs)
to synthesize default `OnStart`/`OnEnd` stat-mod bodies for every SC
that doesn't have a hand-ported handler — closes the SC handler
depth gap from 48 hand-ported to ~350+ with stat-mod bodies (see
[`.agents/migrations/PARITY-CLOSURE-ROADMAP.md`](../../.agents/migrations/PARITY-CLOSURE-ROADMAP.md)
NS-3 wave 2 entry).

## Why not run the YAML parser at server boot?

rAthena's `db/re/status.yml` lives outside the C# repo (under
`/Volumes/1TB/Projetos/rathena/`). Shipping the parser at runtime
would require either bundling the file with Map.Server or pulling
it from disk, both of which couple the build to a specific local
path. Build-time codegen is the cleaner split — the generated
table gets reviewed + committed once, and re-runs on demand when
upstream `status.yml` updates.

## Usage

```bash
python3 Tools/gen-sc-flags/gen-sc-flags.py
```

Reads `/Volumes/1TB/Projetos/rathena/db/re/status.yml` + the C#
`StatusType` enum, writes
`Map.Server/Status/StatusCalcFlagDefaults.cs`.

## CalcFlag → BattleStats field mapping

| rAthena CalcFlag | C# BattleStats field | Notes |
|---|---|---|
| Str / Agi / Vit / Dex / Luk | same | base stats |
| Int                          | IntStat | C# rename to avoid keyword |
| Pow / Sta / Wis / Spl / Con / Crt | same | 4th-class trait stats |
| MaxHp / MaxSp / Hp / Sp      | same | |
| Hit / Flee / Flee2 / Cri     | same | Cri stored at 10× (handler scales) |
| Def / Def2 / Mdef / Mdef2    | same | soft/hard defense |
| Aspd                         | AspdRate | display-rate scaling |
| Speed                        | AspdRate | proxy (no dedicated MoveSpeed% field) |
| Batk / Patk / Smatk          | same | |
| Watk / Matk                  | Batk | no separate min/max field; collapses |
| Res / Mres / Hplus / Crate   | same | 4th-class combat |
| All                          | Str/Agi/Vit/Int/Dex/Luk | expands to 6 |
| Regen / Atk_Ele / Def_Ele / Mode / Dspd / Dye | (skipped) | presence-only or no direct stat |

## When to re-run

- rAthena `db/re/status.yml` updates upstream (new SCs added, new
  CalcFlags assigned)
- `StatusType` enum gains new values that need defaults
- BattleStats fields gain new ones the mapping should cover

After re-running, review the diff to
`Map.Server/Status/StatusCalcFlagDefaults.cs` and commit.

## Stats from the most recent run

```
1001 status.yml SCs parsed
 356 SCs with at least one mapped CalcFlag → generator output
 643 SCs with only-presence flags (no stat mod; default no-op + ScfFlag)
   2 status.yml names without StatusType enum entry (transient drift)
  30 CalcStatField enum values emitted
```
