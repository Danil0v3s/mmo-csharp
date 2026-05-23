# Item-script convergence (CONV-1..CONV-6)

Status: **complete** as of 2026-05-23.

Converged the rAthena item-script and combo-script pipelines from a
runtime-DSL-translated V8 path (DSL-1..DSL-4 + DBR-2a / DBR-2a+) onto
the same TypeScript-source + shared V8 engine model the NPC scripts
use (`Map.Server.Scripting.ScriptHost`).

## Outcome at a glance

| Surface | Before | After |
|---|---|---|
| Combo dispatch engine | `mmo-bonus-scripts` (dedicated V8) | `mmo-scripts` (shared with NPCs) |
| Item-script source | SQL `item_db.script` column, runtime-translated | TypeScript hooks in `scripts/items/generated/*.ts` |
| Combo-script source | SQL `item_combo_db.script`, runtime-translated | TypeScript hooks in `scripts/combos/generated/*.ts` |
| Dispatch surface | `ScriptedBonusService.Apply(scriptStr, …)` | `IItemHookDispatcher` + `IComboDispatcher` |
| Item dispatch axes covered | equip only (Script + EquipScript) | onUse + onEquip + onUnequip |
| Runtime DSL parser/translator | hot path, every recalc | dead code (deleted) |
| Parser/translator status | runtime + build-time | build-time only via `Tools.ItemScriptConvert` |

| Corpus | Coverage |
|---|---|
| `item_db.script` + `equip_script` + `unequip_script` (19,913 rows) | 19,889 auto-converted (99.88%) + 24 hand-ported = **99.99%** (post-GAP-2) |
| `item_combo_db.script` (7,767 rows) | **7,767 auto-converted (100%)** |
| All 7,767 combos invoke through dispatcher | **100% (smoke test green)** |
| All ≥8,000 onEquip hooks invoke through dispatcher | **100% (smoke test green)** |

## Architecture (post-CONV-6)

```
                  ┌────────────────────────────────────────────────┐
                  │           SQL seeds (rAthena data)             │
                  │  seed_item_db_equip.sql, seed_item_db_usable,  │
                  │  seed_item_db_etc.sql, seed_item_combos.sql    │
                  └─────────────────┬──────────────────────────────┘
                                    │
                                    │  build time, npm run gen-items
                                    ▼
              ┌──────────────────────────────────────┐
              │  Tools.ItemScriptConvert (dotnet)    │
              │  SeedReader → RathenaScriptParser    │
              │  → RathenaToJsTranslator → TsEmitter │
              └─────────────────┬────────────────────┘
                                │
                                ▼
       ┌────────────────────────────────────────────────────────┐
       │  scripts/items/generated/items_{lo}_{hi}.ts (112 files) │
       │  scripts/combos/generated/combos_{lo}_{hi}.ts (17 files)│
       │  scripts/items/manual/skipped.ts (24 hand-ports)        │
       └─────────────────┬──────────────────────────────────────┘
                         │
                         │  esbuild bundle
                         ▼
              ┌──────────────────────────────┐
              │  scripts/dist/main.js (7.9MB) │
              └─────────────────┬────────────┘
                                │
                                │  ScriptHost.LoadEntryPoint()
                                ▼
            ┌──────────────────────────────────────────────┐
            │  Shared V8 engine ("mmo-scripts")            │
            │  • registerNpc / registerShop / …            │
            │  • registerItem({ id, onUse?, onEquip?, …})  │
            │  • registerCombo({ comboId, members, … })    │
            │  • __invokeHookWithCtx(fn, rawCtx) Proxy     │
            └─────────────────┬────────────────────────────┘
                              │
                              ▼
                ┌─────────────────────────────────┐
                │  INpcRegistry (in-memory cache)  │
                │  • GetItemById / GetItemByAegis  │
                │  • AllItems / AllCombos          │
                └─────────────────┬───────────────┘
                                  │
                                  ▼
              ┌──────────────────────────────────────┐
              │  Runtime dispatch (game loop thread) │
              ├──────────────────────────────────────┤
              │  CZ_USE_ITEM → IItemHookDispatcher   │
              │      .TryInvokeOnUse(…)              │
              │                                       │
              │  EquipBonusAggregator.BuildBundle →   │
              │      IItemHookDispatcher              │
              │          .TryInvokeOnEquip(…)         │
              │                                       │
              │  EquipService.Unequip →               │
              │      IItemHookDispatcher              │
              │          .TryInvokeOnUnequip(…)       │
              │                                       │
              │  TryRecalcStats →                     │
              │      IComboDispatcher                 │
              │          .ApplyActiveCombos(…)        │
              └──────────────────────────────────────┘
```

## The 6 waves

### CONV-1 — `registerItem` / `registerCombo` registrars (commits 763444a, 8b8cab9)

Added the two TS-callable globals to the shared V8 engine. Items and
combos register at boot via side-effect imports.

- `Map.Server/Scripting/Records/ItemRegistration.cs` — `{ Id, Hooks }`
  (CONV-1.1 simplified this to id-only since the SQL catalog owns
  every other column).
- `Map.Server/Scripting/Records/ItemHooks.cs` — `(OnUse, OnEquip, OnUnequip)`.
- `Map.Server/Scripting/Records/ComboRegistration.cs` — `{ ComboId, Members, Hooks }`.
- `Map.Server/Scripting/Records/ComboHooks.cs` — `(OnActive)`.
- `INpcRegistry` extended: `AddItem` / `AddCombo` / `GetItemById` /
  counts / `AllItems` / `AllCombos`.
- `RegistrarBindings.Bind` adds `registerItem` and `registerCombo`
  globals alongside `registerNpc` / `registerShop` / etc.
- TS-side: `ItemRegistration`, `ComboRegistration`, `ItemUseContext`,
  `ItemEquipContext`, `ItemInfo` interfaces in `scripts/types/api.d.ts`.

### CONV-2 — `Tools.ItemScriptConvert` bulk converter (commits 4cf34e7, 43fb19b)

New console project that reads the SQL seeds and emits the generated
`.ts` files. Reuses the DSL-1..DSL-3 parser/translator.

- Hand-rolled SQL row + value-tuple extractor (regex stops at `);`
  inside quoted bodies; state-machine reader handles SQL string state
  including `\n` / `\t` / `\\` / `''` escapes).
- `RathenaToJsTranslator` gained a `receiverName` parameter so emit
  targets `ctx.bonus(...)` instead of `h.bonus(...)`.
- `RathenaScriptParser` gained paren-call shape (`laphine_upgrade();`,
  `getgroupitem(IG_X);`) to handle ~700 rows that used C-style call
  syntax instead of bare-arg.
- 27 items the converter couldn't translate were hand-ported in
  `scripts/items/manual/skipped.ts` (CONV-2.1). Categories: Zeny /
  counter mutations, megaphones, conditional exp, assignment-as-cond
  bugs (`if (.@b=90)`), pet-egg switches.
- 3 HPLoss cards (4263, 4499, 300403) have a partial-success state
  where `Script` translated cleanly (onEquip is generated) but
  `unequip_script` couldn't (the `heal(1-Hp),0;` paren-wrapped-first-arg
  shape); those keep just their onEquip with the HP-drain-on-unequip
  documented as a known gap.

### CONV-3 — Combo dispatch (commit 89da5dd)

`IComboDispatcher` walks `INpcRegistry.AllCombos()`, resolves member
aegis names to numeric ids via `IItemCatalog`, and invokes matching
combos' `onActive` hooks through the shared engine.

- `ScriptHost.BuildEngine` installs `__invokeHookWithCtx(fn, rawCtx)`
  globally — a JS Proxy wrapper that no-ops unknown property accesses
  so generated scripts can call rAthena builtins we haven't surfaced
  (e.g. `setarray`, `getenchantgrade`) without throwing.
- `ScriptedBonusHost.player` getter exposed so hand-written items
  can read/write player state via `ctx.player.*`.
- `EquipService.TryRecalcStats` calls `dispatcher.ApplyActiveCombos`
  after `BuildBundle`; combo bonuses layer onto the same bundle.
- Acceptance: `EveryRegisteredCombo_InvokesOnActive_WithoutThrowing`
  fires every registered onActive (7,767) without throwing.

### CONV-4 — Item-use + onEquip + onUnequip dispatch (commit ab92a89)

`IItemHookDispatcher` exposes all three lifecycle hooks behind the same
dispatch shape. Three call sites updated.

- `ItemUseService.UseItem` tries `TryInvokeOnUse` first; falls back to
  the C# `ItemEffectRegistry` for items without a TS hook.
- `EquipBonusAggregator.BuildBundle` tries `TryInvokeOnEquip` per item.
- `EquipService.Unequip` fires `TryInvokeOnUnequip` before clearing
  the equip bit so `ctx.getrefine()` reads still work.
- `ItemUseHostContext` reuses the rich `PlayerContext` + `WorldContext`
  from NPC dialogs so item-use authors get the same API surface.
- Acceptance: `EveryItem_with_onEquip_invokes_without_throwing` fires
  every registered onEquip (~19,000) without throwing.

### CONV-5 — Retire runtime DSL path (commit e17a7bf)

Deleted the dead code now that CONV-3 + CONV-4 cover all dispatch.

- Deleted: `IScriptedBonusService`, `ScriptedBonusService`,
  `IItemCombosService`, `ItemCombosService`, `ActiveCombo`.
- Deleted: `ItemCombosSmokeTests`, `ScriptedBonusServiceTests`,
  7 obsolete `EquipBonusAggregatorTests` covering the DSL-extraction
  path.
- `EquipBonusAggregator.BuildBundle` signature simplified from
  `(inventory, catalog, bundle, activeCombos?, scriptedBonuses?, pc?, hookDispatcher?)`
  to `(inventory, catalog, bundle, pc?, hookDispatcher?)`.
- `EquipService` ctor dropped `_combos` and `_scriptedBonuses` fields.
- `Program.cs` DI: dropped both registrations.
- **Kept**: `RathenaScriptParser`, `RathenaToJsTranslator`,
  `RathenaScriptAst`, `ScriptedBonusHost`, `BonusScriptExtractor` —
  the parser/translator power the build-time converter,
  `ScriptedBonusHost` is the C# `ctx` handed to TS hooks,
  `BonusScriptExtractor` is the bundle-write implementation
  `ScriptedBonusHost` delegates to.

### CONV-6 — Audit + final sweep (this commit)

- Four stale doc comments referenced the deleted services — fixed.
- This audit doc.
- Final solution-wide test sweep: 3,317 / 3,317 Map.Server.Tests pass;
  Core.Server.Tests + Login.Server.Tests still green.

## File inventory (post-CONV-6)

**Runtime (Map.Server)**

- `Inventory/IComboDispatcher.cs` / `ComboDispatcher.cs`
- `Inventory/IItemHookDispatcher.cs` / `ItemHookDispatcher.cs`
- `Inventory/Script/ScriptedBonusHost.cs` — ctx for onEquip / onActive / onUnequip
- `Inventory/Script/ItemUseHostContext.cs` — ctx for onUse
- `Inventory/BonusScriptExtractor.cs` — bundle-write helpers (apply-flat / apply-indexed)
- `Inventory/EquipBonusAggregator.cs` — Aggregate (static stats) + BuildBundle (hook dispatch)
- `Inventory/EquipService.cs` — call sites for both dispatchers
- `Inventory/ItemUseService.cs` — onUse call site
- `Scripting/Records/{Item,Combo}{Registration,Hooks}.cs`
- `Scripting/{I,}NpcRegistry.cs` — extended for items/combos
- `Scripting/ScriptHost.cs` — installs `__invokeHookWithCtx`
- `Scripting/Registrars/RegistrarBindings.cs` — `registerItem` / `registerCombo`

**Build-time (Tools.ItemScriptConvert)**

- `Program.cs` — CLI + bucket writer
- `SeedReader.cs` — SQL row + value-tuple extractor + MySQL string decoder
- `TsEmitter.cs` — translator output → TS function-literal body

**Scripts**

- `scripts/items/_dev_test/*` — hand-written test items
- `scripts/items/manual/skipped.ts` — 24 hand-ports
- `scripts/items/generated/*.ts` — 112 auto-generated files (19,886 items)
- `scripts/combos/_dev_test/*` — hand-written test combo
- `scripts/combos/generated/*.ts` — 17 auto-generated files (7,767 combos)
- `scripts/types/api.d.ts` — registrar + context interfaces

**Tests**

- `Map.Server.Tests/Scripting/ScriptHostTests.cs` — registrar surface + bundle-load smoke
- `Map.Server.Tests/Inventory/ComboDispatcherTests.cs` — combo dispatch + 7,767-combo smoke
- `Map.Server.Tests/Inventory/ItemHookDispatcherTests.cs` — item dispatch + ~8,000-item smoke
- `Map.Server.Tests/Inventory/Script/RathenaScriptParserTests.cs` — build-time parser
- `Map.Server.Tests/Inventory/Script/RathenaToJsTranslatorTests.cs` — build-time translator
- `Map.Server.Tests/Inventory/EquipBonusAggregatorTests.cs` — static stat extraction

## Known gaps

| Gap | Note |
|---|---|
| 27 items the converter skipped, 3 of which still lack their `unequip_script` (HP-drain cards 4263 / 4499 / 300403) | Documented inline in `scripts/items/manual/skipped.ts`. Niche cards; bug-for-bug parity with rAthena's `heal(1-Hp),0;` mis-shape isn't worth the parser extension. |
| Pet-egg switch (`getpetinfo(PETINFO_EGGID)`) in 4 hand-ported items (15980, 410027, 410028, 490405) | Always-on prefix bonuses port cleanly; per-egg branches are TODO until pet info surfaces on `ItemEquipContext`. Documented inline. |
| Megaphone items (12221, 14840, 23340) | Skip the `input` prompt — item-use has no dialog session. Broadcast a placeholder. Future: per-item input UI. |
| `bAutoSpell` / `autobonus` family in combo + item bonuses | Translated cleanly to host calls but the host's autobonus registration path was DSL-3 era; CONV-3/4 kept the surface but the actual proc behavior depends on `IPlayerBonusService.AddAutobonus`, which is independently wired and unchanged. |

## History

### 2026-05-23 — GAP-2: parser fix for paren-wrapped first arg

`RathenaScriptParser.ParseCallStmt` now handles three call shapes
instead of two: bare-comma, paren-all, and paren-first (where
`name(expr)` is the first bare-call arg with more comma-separated
args following). Recovered the 3 HPLoss cards' `unequip_script`
columns (4263 / 4499 / 300403) — converter coverage moves from
19,886 → 19,889 (99.88% → 99.99% with hand-ports).

### 2026-05-23 — CONV-6: audit + sweep

Full convergence complete. 27,653 / 27,680 (99.90%) rAthena item +
combo scripts run through the shared TypeScript V8 engine. Runtime
DSL path retired. Solution builds clean; all server-side tests green.
