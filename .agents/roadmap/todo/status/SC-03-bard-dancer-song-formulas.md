# SC-03 — Correct Bard/Dancer song formulas + collapse duplicate registrations

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SC-01 (overwrite guard makes the dup-registration safe to collapse) · **Blocks:** none

## Problem

Several Bard/Dancer song SCs are registered **twice** with **different formulas**, and the
copy that wins at runtime uses the wrong magnitude. The duplication itself is the trap: a
correct body exists, but a second registration with an approximate formula runs later in the
ctor and overwrites it.

Concretely, the live (winning) bodies are wrong for:
- **Assncros** (Soul-Linker-tagged ASPD song / DC_DONTFORGETME family): live body uses
  `Val2 = 5 + 5*Val1`. rAthena: `val2 = val1 < 10 ? val1*2 - 1 : 20`.
- **Appleidun** (DC_SERVICEFORYOU / BA Idun's Apple): live body uses `Val2 = 5 + 2*Val1`,
  `Val3 = 5 + 5*Val1`. rAthena renewal arm: `val2 = (5 + 2*val1) + (vit_of_caster/10) + BA_MUSICALLESSON/2`
  (HP rate). The caster VIT and MusicalLesson contributions are dropped.
- **Drumbattle** is *correct* in the shadowed copy (line 3908: Val2=15+5·Val1 Atk, Val3=Val1·15
  Def) but the generator default (`StatusCalcFlagDefaults` maps `Drumbattle → Def` only) would
  give +Val1 Def with no Atk if the real body were ever lost.

## Verified registration map (ctor order = which wins)

Ctor calls `RegisterWave4aBespokeFormulas()` at line 961, then `RegisterWave32Val2Val3Formulas()`
at line 1002 — so the **Wave32 copy (lower line number, ~1051-1430) WINS** over the Wave4a copy
(~3908-4038). For each song, the WINNING line and its correctness:

| SC | Wave32 (wins) | Wave4a (shadowed) | Winner correct? | rAthena |
|---|---|---|---|---|
| Whistle | 1066 | 3928 | ✅ both correct | val2=18+2·v1 Flee, val3=(v1+1)/2 Flee2 |
| Humming | 1099 | 3947 | ✅ | val2=4·v1 Hit |
| Fortune | 1138 | 3966 | ✅ (×10 Cri scale) | val2=v1·10 Cri |
| Service4u | 1154 | 3982 | verify | val2=v1<10?9+v1:20 MaxSP%, val3=5+v1 SP cost% |
| Dontforgetme | 1117 | 4038 | verify | val2=1+30·v1 ASPD pen, val3=5+2·v1 move% |
| **Assncros** | **1085** | 4000 | ❌ uses 5+5·v1 | **val2 = v1<10 ? v1·2-1 : 20** (status.cpp:10736) |
| **Appleidun** | **1412** | 4016 | ❌ drops vit/lesson | **val2 = (5+2·v1)+(casterVit/10)+MusicalLesson/2** (status.cpp:12136) |
| Drumbattle | — | 3908 | ✅ (only copy) | val2=15+5·v1 Atk, val3=v1·15 Def (status.cpp:10721) |

NOTE the earlier Whistle Val3 confusion: the Wave32 Whistle (1066) computes
`Val3 = ((Val1+1)/2)*10` while the Wave4a copy (3932) computes `(Val1+1)/2`. rAthena
(status.cpp:10734) is `(val1+1)/2`. **The Wave32 (winning) Whistle Val3 is 10× too high** — fix it.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs:1027` — `RegisterWave32Val2Val3Formulas()` holds the
  winning copies: Assncros (1085), Humming (1099), Dontforgetme (1117), Fortune (1138),
  Service4u (1154), Whistle (1066), Appleidun (1412).
- `Map.Server/Status/StatusEffectRegistry.cs:3908-4054` — the Wave4a copies (Drumbattle 3908,
  Whistle 3928, Humming 3947, Fortune 3966, Service4u 3982, Assncros 4000, Appleidun 4016,
  Dontforgetme 4038); shadowed by Wave32 except Drumbattle which only lives here.
- The OnStart bodies do NOT receive a caster reference for VIT/skill-level terms — the
  `StatusEffectHandler.OnStart` signature is `(target, sc, source)`. Appleidun's caster-VIT and
  BA_MUSICALLESSON terms must be pre-computed by the apply-side caller and passed via Val2/Val3, OR
  read from `source` if it is the caster entity.

## rAthena reference (source of truth)

- `rathena/src/map/status.cpp:10721` `SC_DRUMBATTLE: val2 = 15 + val1*5; val3 = val1*15;`
- `status.cpp:10732` `SC_WHISTLE: val2 = 18 + 2*val1; val3 = (val1+1)/2;`
- `status.cpp:10736` `SC_ASSNCROS: val2 = val1 < 10 ? val1*2 - 1 : 20;`
- `status.cpp:10747` `SC_HUMMING: val2 = 4*val1;`
- `status.cpp:12136` (renewal arm) `SC_APPLEIDUN: val2 = (5 + 2*val1) + (status_get_vit(src)/10);`
  `if (s_sd) val2 += pc_checkskill(s_sd, BA_MUSICALLESSON)/2;` — HP rate %.
- Pre-renewal `SC_APPLEIDUN: val2 = val1 < 10 ? 9 + val1 : 20;` `val3 = 2*val1;` (potion rate).
  Match the renewal arm (`db/re`).
- Consumers: Drumbattle Atk read at `status.cpp:7110-7111` (`watk += val2`); Appleidun HP-rate at
  `status.cpp:3154-3155` (`bonus += val2`).

## Scope — every sub-system that must be touched

- [ ] **Delete the duplicate registrations**: keep ONE body per song SC (preferably consolidate
      into `RegisterWave32Val2Val3Formulas` since it wins; move Drumbattle there too). After SC-01's
      guard lands, the dup is a no-op, but it MUST be removed for clarity.
- [ ] **Fix Assncros** to `Val2 = Val1 < 10 ? Val1*2 - 1 : 20` (AspdRate += Val2).
- [ ] **Fix Whistle Val3** to `(Val1+1)/2` (remove the `*10`).
- [ ] **Fix Appleidun** to renewal HP-rate: `Val2 = (5 + 2*Val1) + (casterVit/10) + (musicalLesson/2)`.
      Thread caster VIT + BA_MUSICALLESSON level: if `source` is the caster `PlayerEntity`, read its
      VIT and skill level; otherwise the apply-side caller must pre-fill Val2 and OnStart respects a
      non-zero Val2 (`if (sc.Val2 == 0) ...`).
- [ ] **Verify Service4u and Dontforgetme** winning copies match rAthena (table above) and fix if
      not; the doc-comments at 1111-1116 / 1154 should match the live formula.
- [ ] **Ensure Drumbattle survives** (move to the winning method or rely on SC-01 guard) and applies
      both Atk (Val2 to WatkMin/WatkMax) and Def (Val3) — the generator's `Def`-only map is wrong.

## Done criteria

- Assncros Val1=5 → Val2=9 (`5*2-1`), AspdRate +9; Val1=10 → Val2=20.
- Whistle Val1=5 → Flee+28, Flee2+3 (not +30).
- Appleidun Val1=5, caster VIT=80, MusicalLesson=10 → Val2 = (5+10) + 8 + 5 = 28 (HP rate %).
- Drumbattle Val1=5 → Watk +40, Def +75.
- Exactly one `Register` per song SC; no shadowed duplicate.
- `StatusEffectCompletenessTests` green.

## Test plan

- Extend `Wave97Batch1FormulaTests` / add `BardDancerSongFormulaTests`: parametrized Val1 cases
  pinning Val2/Val3 and the resulting stat deltas for all eight songs, including Appleidun's
  caster-VIT/MusicalLesson path with a stub `PlayerEntity` source.
- Boundary cases: Assncros Val1=9 (→17) vs Val1=10 (→20); Appleidun pre/renewal arm selection.
- Re-run `StatusEffectCompletenessTests`, `StatusCalcServiceTests`.

## Notes / gotchas

- The `if (sc.Val2 == 0)` idempotence guard means a caller that pre-computes the magnitude (with
  full caster context) is respected; OnStart only fills defaults. Preserve this for Appleidun so a
  party-buff apply path can pass the exact value.
- Confirm `BattleStats` ASPD convention (higher = faster) before applying Assncros/Dontforgetme;
  Dontforgetme is a *penalty* (subtract), Assncros a *bonus* (add).
- After SC-01, removing the shadowed copies is mechanical; do it in the same PR to avoid leaving
  two now-identical bodies.
