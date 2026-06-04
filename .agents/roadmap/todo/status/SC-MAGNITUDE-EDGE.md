# SC-MAGNITUDE-EDGE — the blocked/entangled magnitude tail from SC-MAGNITUDE

> **Epic:** status · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none (but two clusters are gated on other systems — see below) · **Unlocks:** none

## The deliverable

> The handful of generator-default SCs whose magnitude SC-MAGNITUDE could not faithfully convert
> (their value is set by a not-yet-ported casting skill, or it's entangled with the deferred
> weapon-element / regen systems) apply their real rAthena effect instead of the generic +Val1.

## Why these were carved out of SC-MAGNITUDE

SC-MAGNITUDE swept every *systematic* magnitude seam — wrong-stat (Batk↔Watk/Matk), wrong-sign
debuffs, fixed-constant magnitudes, percent-pool (clan), and absolute-DEF-override. The SCs below
are the residue that needs infrastructure SC-MAGNITUDE deliberately did not build.

### Cluster A — skill-set magnitude (generator adds +Val1; the real value is a different Val the casting skill must populate)

| SC | rAthena | Current C# bug |
|---|---|---|
| `SC_KYOUGAKU` | all six base stats −= **Val2** (status.cpp:6563-6907; Val2 set by KO_KYOUGAKU = `rnd()`-based) | generator *adds* +Val1 (wrong sign) |
| `SC_C_MARKER` | Flee −= **Val3** (status.cpp:7427) | generator *adds* +Val1 Flee |
| `SC_DEFSET` | Def = **Val1**, Def2 = **Val1** (absolute, status.cpp:7538/7637) | generator adds +Val1 (a `RegisterDefOverride` exists — wire it once the skill sets Val1) |
| `SC_MDEFSET` | Mdef = **Val1** (absolute, status.cpp:7711) | generator adds +Val1 |

These can't be converted faithfully until the C# **skill plugin** that casts each one sets the SC's
`Val2`/`Val3`/`Val1` to the rAthena amount at `sc_start` time. The `RegisterDefOverride` helper
(StatusEffectRegistry) already handles the Defset/Mdefset shape — they just need the skill side.

### Cluster B — multi-system elemental SCs (stat part + weapon-element endow + HP/SP regen, level-gated)

| SC | rAthena effect |
|---|---|
| `SC_FIRE_INSIGNIA` | lv1 HP-regen +100; lv2 Watk +50 + weapon→Fire; lv3 Matk +50 (status.cpp:7150/7239/5415) |
| `SC_WATER_INSIGNIA` | lv1 HP-regen; lv2 +Watk + weapon→Water + def; lv3 … |
| `SC_WIND_INSIGNIA` | lv2 +Watk + weapon→Wind; … |
| `SC_EARTH_INSIGNIA` | def+50, mdef+50, MaxHp/MaxSp, HP-regen (status.cpp) |
| `SC_CLIMAX_DES_HU` | Matk +100 (generator maps to Batk) + magic_atk_ele[WIND] +30 (status.cpp:7239/4841) |
| `SC_CLIMAX_CRYIMP` | Def +300, Mdef +100 + subele[WATER]+30 + magic_atk_ele[WATER]+30 (status.cpp:4845) |

The stat parts are convertible, but each is **level-gated** and entangled with the **weapon-element
endow** + **HP/SP regen** systems that the roadmap defers (see SC-MAGNITUDE Notes: "element-endow
deferred after gameplay"). Do these together with the element-endow work so the SC isn't left
half-implemented (a +Matk with no element change is still wrong for the player).

## rAthena reference

`rathena/src/map/status.cpp` — `status_calc_str/agi/…` (Kyougaku/Stomachache-style debuffs),
`status_calc_def/def2/mdef` (Defset/Mdefset overrides), `status_calc_watk/matk` + `status_calc_regen`
+ `status_calc_element` (insignia), and the per-SC start arms.

## Scope

- [ ] Cluster A: once each casting skill is ported, have it set the SC Val; then register the real
      stat handler (subtract for Kyougaku/C_Marker; the existing `RegisterDefOverride` for Defset/Mdefset).
- [ ] Cluster B: convert each insignia/climax stat part **alongside** its weapon-element + regen effect
      so the SC is faithful end-to-end (not a stat-only half-port).

## Done criteria

- None of these SCs are served by the generic +Val1 synthesis; each applies its rAthena stat magnitude
  (and, for Cluster B, its element/regen effect). `GeneratedStatModDefaultTypes` no longer lists them.

## Test plan

- Per-SC magnitude tests mirroring `SC02CalcFlagAllTests` (mob-based) + the `Combat53BespokeRefoldTests`
  recalc-survival theory for the derived-stat ones.

## History

- 2026-06-05 — Filed from SC-MAGNITUDE's close-out (rule 3). These are the SCs the systematic magnitude
  audit identified as wrong-but-not-cleanly-convertible: their magnitude is skill-set (Cluster A) or
  entangled with the deferred element-endow/regen systems (Cluster B).
