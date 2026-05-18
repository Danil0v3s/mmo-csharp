# Phase 1 — Engine + NPC rendering

**Goal:** stand up the TypeScript-authored, Jint-executed scripting runtime end-to-end. At Map.Server boot, Jint loads `scripts/dist/main.js`, which side-effect-imports the rest of the tree. Each `register*()` call accumulates into `INpcRegistry`. After all modules evaluate, `NpcSpawnService` places `NpcEntity` instances on the map. Clients in view see the sprites via the existing visibility pipeline.

**`onClick` and the other hooks are captured (stored as `JsValue` handles) but not invoked.** Click handling is stubbed: log + send `ZC_CLOSE_DIALOG`. Phase 2 wires execution + suspension.

**Phase 1 ends with:** the engine works, IntelliSense works in `scripts/`, and 2–3 hand-written test NPCs are visible in prontera when you log in.

**Depends on:** [world.md](../world.md), [entities.md](../entities.md), [visibility.md](../visibility.md), the existing [`MobSpawnService`](../../../../Map.Server/Spawn/MobSpawnService.cs) pattern.

**Out of scope (Phase 2+):** dialog execution, suspension primitives, click dispatch, the `ctx.player.*` surface, variable persistence, event hooks, the rAthena translator.

## Pieces to build

### Server side

```
Map.Server/Scripting/
├── ScriptHost.cs                    — owns Jint Engine; loads dist/main.js at boot
├── ScriptHostOptions.cs             — config: ScriptsRoot, EntryFile, HotReload
├── INpcRegistry.cs / NpcRegistry.cs — indexed by name + (map, x, y) + map; rejects duplicates
├── NpcSpawnService.cs               — at-boot: registry → IEntityRegistry (mirrors MobSpawnService)
├── Registrars/
│   ├── RegistrarBindings.cs         — injects the five register* functions into Jint scope
│   ├── RegisterNpcBinding.cs
│   ├── RegisterFloatingNpcBinding.cs
│   ├── RegisterShopBinding.cs
│   ├── RegisterWarpBinding.cs
│   └── RegisterSpawnBinding.cs
└── Records/                         — typed C# records mirroring TS shapes
    ├── NpcRegistration.cs
    ├── FloatingNpcRegistration.cs
    ├── ShopRegistration.cs
    ├── WarpRegistration.cs
    ├── SpawnRegistration.cs
    └── ScriptHandle.cs              — wraps a JsValue closure; opaque to the rest of the server

Map.Server/Handlers/
└── ContactNpcHandler.cs             — CZ_CONTACTNPC stub: log + ZC_CLOSE_DIALOG

Map.Server/Visibility/VisibilityService.cs  — add NpcEntity arm to BuildEnterViewPacket
```

### Scripts project (new top-level dir)

```
scripts/
├── package.json                     — devDependency: typescript; scripts: "build", "watch"
├── tsconfig.json                    — target ES2022, module ES2022, strict, outDir ./dist
├── types/api.d.ts                   — THE contract (hand-authored for Phase 1)
├── lib/                             — empty for Phase 1; first occupant in Phase 2 (kafraDialog)
├── npcs/
│   ├── index.ts                     — `import "./_dev_test";` (Phase 2+: import the real tree)
│   └── _dev_test.ts                 — 2–3 test NPCs at known prontera coords
├── shops/index.ts                   — empty for Phase 1
├── warps/index.ts                   — empty for Phase 1
├── spawns/index.ts                  — empty for Phase 1
├── main.ts                          — `import "./npcs"; import "./shops"; import "./warps"; import "./spawns";`
└── dist/                            — tsc output; gitignored
```

## Implementation order

### 1. `scripts/` skeleton

- `package.json` with `typescript@5` as a devDependency.
- `tsconfig.json`: `target: ES2022`, `module: ES2022`, `moduleResolution: Bundler` (or `NodeNext`), `strict: true`, `outDir: ./dist`, `rootDir: .`.
- `types/api.d.ts` hand-authored — see "API contract" below.
- `main.ts` with the four side-effect imports.
- `npcs/_dev_test.ts` with two NPCs at `prontera (160, 160)` and `prontera (165, 160)` — `registerNpc` only, simple `onClick: async (ctx) => { /* Phase 2 */ }`.
- `npm run build` produces `dist/main.js` + a few module files.
- VSCode opens `scripts/` and IntelliSense works against `types/api.d.ts`. **This is the user-visible deliverable for the author experience.**

### 2. `api.d.ts` contract (Phase 1 subset)

Phase 1 ships the registrars and the type shapes, but the runtime only consumes `registerNpc` data. The full surface is in the contract from day one so authors can write against it.

```ts
// scripts/types/api.d.ts

declare global {
    // === Registrars ===
    function registerNpc(npc: NpcRegistration): void;
    function registerFloatingNpc(npc: FloatingNpcRegistration): void;
    function registerShop(shop: ShopRegistration): void;
    function registerWarp(warp: WarpRegistration): void;
    function registerSpawn(spawn: SpawnRegistration): void;
}

export interface NpcRegistration {
    map: string;
    x: number; y: number;
    dir?: number;                                // 0..7, default 0
    sprite: number;
    name: string;
    triggerArea?: { xs: number; ys: number };    // omit for click-only
    onClick?: NpcHandler;
    onTouch?: NpcHandler;
    onInit?: NpcHandler;
    onTimer?: Record<number, NpcHandler>;        // { 5000: ..., 30000: ... }
    onPCLogin?: NpcHandler;
    onPCDeath?: NpcHandler;
    onPCKill?: NpcHandler;
    onNPCKill?: NpcHandler;
    // (more hooks land in Phase 5)
}

export interface FloatingNpcRegistration {
    name: string;                                // unique; used for doevent("Name::OnFoo") in Phase 5
    onInit?: NpcHandler;
    onTimer?: Record<number, NpcHandler>;
    onClock?: Record<string, NpcHandler>;        // "0000" → midnight
    onPCLogin?: NpcHandler;
    onPCDeath?: NpcHandler;
}

export type ShopRegistration =
    | ({ kind: "shop"   } & ShopBase & { items: ShopItem[] })
    | ({ kind: "cash"   } & ShopBase & { items: ShopItem[] })
    | ({ kind: "item"   } & ShopBase & { costItem: number;     items: ShopItem[] })
    | ({ kind: "point"  } & ShopBase & { costVariable: string; items: ShopItem[] })
    | ({ kind: "market" } & ShopBase & { items: MarketShopItem[] });

interface ShopBase {
    map: string; x: number; y: number; dir?: number;
    sprite: number;
    name: string;
}
export interface ShopItem        { itemId: number; price: number; discount?: number; }
export interface MarketShopItem  { itemId: number; price: number; stock: number; }

export interface WarpRegistration {
    from: { map: string; x: number; y: number };
    area: { xs: number; ys: number };
    to: { map: string; x: number; y: number };
    type?: "warp" | "warp2";                     // default "warp"
}

export interface SpawnRegistration {
    map: string;
    area?: { x: number; y: number; xs: number; ys: number };  // omit = anywhere walkable
    mobId: number;
    amount: number;
    respawn?: { baseMs: number; jitterMs?: number };
    boss?: boolean;
    name?: string;                               // display override
    onDeath?: string;                            // event label, Phase 5+
    size?: 0 | 1 | 2;
    ai?: number;
}

// === Context types (used by hook closures; Phase 2 wires the implementations) ===

export type NpcHandler = (ctx: NpcContext) => Promise<void> | void;

export interface NpcContext {
    npc: NpcInfo;
    player: PlayerContext | null;                // null for onInit/onTimer/onClock
    world: WorldOps;

    // Phase 2 implements these — defined in the contract now so author signatures don't churn
    mes(text: string): Promise<void>;
    next(): Promise<void>;
    menu(options: string[]): Promise<number>;    // 1-based
    select(options: string[]): Promise<number>;  // 1-based
    input(opts?: { min?: number; max?: number }): Promise<number>;
    inputString(opts?: { maxLength?: number }): Promise<string>;
    close(): Promise<void>;
    close2(): Promise<void>;
    sleep(ms: number): Promise<void>;
    progressBar(ms: number, color?: string): Promise<void>;
}

export interface NpcInfo {
    map: string; x: number; y: number; dir: number;
    name: string; sprite: number;
    vars: Record<string, unknown>;               // rAthena .var (NPC-local, in-memory)
}

export interface PlayerContext { /* fields land in Phase 3 */ }
export interface WorldOps      { /* surface lands in Phase 3 */ }
```

For Phase 1 the `NpcContext` / `PlayerContext` / `WorldOps` types exist so author signatures compile, but the closures are never invoked. Authors writing dialog code now get a green type-check and red-squiggle-free editor; the code just doesn't run yet.

### 3. `ScriptHost` + Jint engine

```csharp
public sealed class ScriptHost
{
    private readonly Engine _engine;
    private readonly INpcRegistry _registry;
    private readonly ScriptHostOptions _options;
    private readonly ILogger<ScriptHost> _logger;

    public ScriptHost(INpcRegistry registry, IOptions<ScriptHostOptions> options, ILogger<ScriptHost> logger)
    {
        _registry = registry;
        _options = options.Value;
        _logger = logger;
        _engine = new Engine(opts =>
        {
            opts.AllowClr(false);
            opts.LimitRecursion(100);
            opts.MaxStatements(10_000_000);   // generous; the registration pass is one-time
        });
        RegistrarBindings.Bind(_engine, _registry);
    }

    public void LoadEntryPoint()
    {
        var entry = Path.Combine(_options.ScriptsRoot, _options.EntryFile);
        var src = File.ReadAllText(entry);
        try
        {
            _engine.Execute(src, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script entry point failed to evaluate: {Entry}", entry);
            throw;
        }
        _logger.LogInformation(
            "Scripts loaded: {Npcs} NPCs / {Floating} floating / {Shops} shops / {Warps} warps / {Spawns} spawns",
            _registry.NpcCount, _registry.FloatingCount, _registry.ShopCount, _registry.WarpCount, _registry.SpawnCount);
    }
}
```

Wired into `Program.cs` after `IMapWorldRegistry` is populated and before `NpcSpawnService.SpawnInitial()`.

`ScriptHostOptions` lives in `appsettings.json`:
```json
"Scripting": {
  "ScriptsRoot": "../scripts/dist",
  "EntryFile": "main.js",
  "HotReload": true
}
```

### 4. Registrar bindings

Each `register*` is a `Action<JsValue>` (or `Delegate`) injected into the engine. It receives a Jint object, validates/marshals into the matching C# record, and pushes into the registry. **Closures (`onClick`, etc.) are stored as raw `JsValue` handles** — opaque to the registry, only meaningful to Phase 2's dispatcher.

```csharp
internal static class RegistrarBindings
{
    public static void Bind(Engine engine, INpcRegistry registry)
    {
        engine.SetValue("registerNpc",         new Action<JsValue>(v => RegisterNpcBinding.Invoke(v, registry)));
        engine.SetValue("registerFloatingNpc", new Action<JsValue>(v => RegisterFloatingNpcBinding.Invoke(v, registry)));
        engine.SetValue("registerShop",        new Action<JsValue>(v => RegisterShopBinding.Invoke(v, registry)));
        engine.SetValue("registerWarp",        new Action<JsValue>(v => RegisterWarpBinding.Invoke(v, registry)));
        engine.SetValue("registerSpawn",       new Action<JsValue>(v => RegisterSpawnBinding.Invoke(v, registry)));
    }
}

internal static class RegisterNpcBinding
{
    public static void Invoke(JsValue raw, INpcRegistry registry)
    {
        if (raw is not ObjectInstance obj)
            throw new ScriptValidationException("registerNpc() requires an object literal");

        var reg = new NpcRegistration
        {
            Map         = ReadRequiredString(obj, "map"),
            X           = (short)ReadRequiredInt(obj, "x"),
            Y           = (short)ReadRequiredInt(obj, "y"),
            Dir         = (byte)ReadOptionalInt(obj, "dir", 0),
            Sprite      = ReadRequiredInt(obj, "sprite"),
            Name        = ReadRequiredString(obj, "name"),
            TriggerArea = ReadOptionalTriggerArea(obj, "triggerArea"),
            Hooks       = ReadHooks(obj, NpcHookNames),
        };
        registry.AddNpc(reg);
    }

    private static readonly string[] NpcHookNames =
        { "onClick", "onTouch", "onInit", "onTimer", "onPCLogin", "onPCDeath", "onPCKill", "onNPCKill" };
}
```

The `ScriptHandle` record stores the `JsValue` and the source location (`obj.Engine.GetSourceLocation(...)`) for stack traces. `IsCallable` is checked at bind time so a bogus `onClick: "string"` fails loudly at registration, not at first click.

### 5. `NpcRegistry`

In-memory, populated during `ScriptHost.LoadEntryPoint`. Three indexes:

- `byName: Dictionary<string, NpcRegistration>` — used by `doevent` in Phase 5
- `byMap:  Dictionary<uint, List<NpcRegistration>>` — used by spawn service
- `byCell: Dictionary<(uint mapId, short x, short y), NpcRegistration>` — used by click resolution

Duplicate detection:
- Same name across the corpus → reject with file:line from the JsValue source location.
- Same (map, x, y) → reject (only one NPC per cell; rAthena enforces this too).

Floating NPCs go into a separate `byFloatingName` index.

### 6. `NpcSpawnService`

Mirrors [`MobSpawnService.SpawnInitial()`](../../../../Map.Server/Spawn/MobSpawnService.cs):

- For each `NpcRegistration` in the registry:
  - Resolve map via `IMapWorldRegistry`; skip + warn if not hosted.
  - Skip if `IsWalkable(x, y) == false`? **No** — NPCs commonly sit on cells flagged non-walkable (the cell carries `CELL_NPC` after this pass). Just place the entity at the requested cell.
  - Allocate `EntityId` via `EntityIdAllocator`.
  - Construct `NpcEntity`, store the `ScriptHandle` bundle on it (for Phase 2 dispatch).
  - Add to `IEntityRegistry`.
- No `NotifySpawnedToArea` — boot-time, no PCs connected. First PC's `SendCurrentViewToSelf` emits STANDENTRY when they enter AOI.
- Floating NPCs are NOT placed in the world; they live only in `byFloatingName`.

### 7. `NpcEntity` carries the hook bundle

```csharp
public sealed class NpcEntity : Entity
{
    public string Name { get; }
    public int SpriteId { get; }
    public TriggerArea? TriggerArea { get; }
    public NpcHooks Hooks { get; }

    public override EntityType Type => EntityType.Npc;

    public NpcEntity(EntityId id, NpcRegistration reg, uint mapId)
        : base(id, mapId, reg.X, reg.Y)
    {
        Name = reg.Name;
        SpriteId = reg.Sprite;
        TriggerArea = reg.TriggerArea;
        Hooks = reg.Hooks;
        Dir = reg.Dir;
    }
}

public sealed record NpcHooks(
    ScriptHandle? OnClick,
    ScriptHandle? OnTouch,
    ScriptHandle? OnInit,
    IReadOnlyDictionary<int, ScriptHandle>? OnTimer,
    ScriptHandle? OnPCLogin,
    ScriptHandle? OnPCDeath,
    ScriptHandle? OnPCKill,
    ScriptHandle? OnNPCKill);
```

### 8. Visibility integration

[`VisibilityService.BuildEnterViewPacket`](../../../../Map.Server/Visibility/VisibilityService.cs) currently throws on `NpcEntity` (line 207). Add the arm:

```csharp
NpcEntity n => new ZC_NOTIFY_STANDENTRY
{
    ObjectType = 6,            // BL_EVT_CLIF — verify against PACKETVER 20220401 dhxj capture
    AccountId = n.Id.Value,
    CharacterOrEntityId = n.Id.Value,
    Speed = 200,
    Job = (short)n.SpriteId,
    X = n.X, Y = n.Y, Dir = n.Dir,
    Name = n.Name,
},
```

`BuildExitViewPacket` already has the catch-all `ZC_NOTIFY_VANISH`, so vanish is automatic.

### 9. `ContactNpcHandler` stub

```csharp
[PacketHandler]
public sealed class ContactNpcHandler : IPacketHandler<MapSession, CZ_CONTACTNPC>
{
    public ValueTask HandleAsync(MapSession session, CZ_CONTACTNPC pkt)
    {
        var npc = _entities.Get(new EntityId(pkt.NpcId)) as NpcEntity;
        if (npc?.Hooks.OnClick == null) { /* log + close */ }
        else
        {
            _logger.LogInformation(
                "NPC click (Phase 1, dispatch stubbed): {Name} @ ({X},{Y}) by char {Char}",
                npc.Name, npc.X, npc.Y, session.CharId);
        }
        session.Enqueue(new ZC_CLOSE_DIALOG { NpcId = pkt.NpcId });
        return ValueTask.CompletedTask;
    }
}
```

### 10. Hot reload (dev-only)

`FileSystemWatcher` on `scripts/dist/`. On any change:
1. Snapshot current `NpcRegistry` size.
2. Clear the registry.
3. `ScriptHost.LoadEntryPoint()` again.
4. Diff old vs new registrations; for added/changed NPCs, allocate entities + broadcast; for removed, vanish + free.

Phase 1 ships a coarse version: log "scripts changed; restart to apply" and skip the live diff. Live-diff lands in Phase 2 once the dispatcher exists.

## Test NPCs (Phase 1 acceptance fixtures)

```ts
// scripts/npcs/_dev_test.ts
registerNpc({
    map: "prontera", x: 160, y: 160, dir: 4,
    sprite: 105, name: "Phase 1 Test",
    async onClick(ctx) {
        await ctx.mes("Click handled in Phase 2. You shouldn't see this yet.");
        await ctx.close();
    },
});

registerNpc({
    map: "prontera", x: 165, y: 160, dir: 4,
    sprite: 114, name: "Kafra Test",
});

registerFloatingNpc({
    name: "EventManager",
    onInit() {
        // Phase 5 will actually invoke this.
    },
});
```

## Tests

| Test | What it covers |
|---|---|
| `ScriptHostTests.LoadsEmptyEntry` | `main.js` with no register calls evaluates cleanly; registry is empty |
| `ScriptHostTests.RejectsSyntaxError` | Malformed JS surfaces a `ScriptEvaluationException` with file + line |
| `RegisterNpcBindingTests.MapsRequiredFields` | Minimal object → `NpcRegistration` with all required fields populated |
| `RegisterNpcBindingTests.RejectsMissingRequired` | Omit `sprite` → throws with field name |
| `RegisterNpcBindingTests.PreservesClosureHandles` | `onClick: () => {}` → `ScriptHandle.IsCallable == true` |
| `RegisterNpcBindingTests.RejectsNonCallableHook` | `onClick: "oops"` → throws at registration |
| `RegisterShopBindingTests.DiscriminatesKinds` | `kind: "item"` requires `costItem`; missing → throws |
| `RegisterFloatingNpcBindingTests.RejectsWorldFields` | Object with `map`/`x`/`y` → throws ("floating NPCs have no world position") |
| `NpcRegistryTests.RejectsDuplicateName` | Two NPCs same `name` → second throws |
| `NpcRegistryTests.RejectsDuplicateCell` | Two NPCs same `(map, x, y)` → second throws |
| `NpcSpawnServiceTests.PlacesAllNpcsOnRegisteredMaps` | Three NPCs across two maps → three entities in `IEntityRegistry` |
| `NpcSpawnServiceTests.SkipsUnknownMaps` | NPC on `notamap` → skipped + warning, doesn't abort spawn pass |
| `NpcSpawnServiceTests.SkipsFloatingNpcs` | `registerFloatingNpc` does NOT spawn an entity |
| `VisibilityServiceTests.EmitsStandEntryForNpc` | PC enters AOI of an NPC → STANDENTRY on PC's session |
| `ContactNpcHandlerTests.SendsCloseDialog` | Click → server enqueues `ZC_CLOSE_DIALOG`; no exception |
| `Phase1EndToEnd.PrototypeNpcRenders` | Boot Map.Server with the `_dev_test.ts` fixture, simulate a player at prontera (158, 160), assert STANDENTRY for "Phase 1 Test" + "Kafra Test" |

## Acceptance

- `cd scripts && npm install && npm run build` produces `dist/main.js`.
- `dotnet run --project Map.Server` boots, evaluates `dist/main.js`, logs `Scripts loaded: 2 NPCs / 1 floating / 0 shops / 0 warps / 0 spawns`.
- A player who logs in and spawns at prontera (150, 150) walks east and sees two NPCs at (160, 160) and (165, 160) with correct sprites and directions.
- Clicking either NPC: server logs the click, client receives `ZC_CLOSE_DIALOG`, no hang.
- VSCode opens `scripts/`, hovering `registerNpc` shows the JSDoc, autocomplete on the registration object literal shows all valid fields, an invalid field (`spritee: 105`) shows an error.
- `dotnet test` green; replay-baseline diff count does not regress.

## Open questions to resolve during implementation

- **`ObjectType` value for NPCs** — `BL_NPC_CLIF=1` vs `BL_EVT_CLIF=6`. Capture the byte sequence rAthena sends for a real prontera NPC under PACKETVER 20220401 (dhxj) and match.
- **TypeScript build pipeline** — manual `npm run build` for Phase 1; CI integration and a `tsc --watch` recipe in [Map (declarative-catalogs.md)](../declarative-catalogs.md)-style History entry. Production deploys ship the prebuilt `dist/`.
- **Module resolution** — `target: ES2022, module: ES2022` requires Jint 3.x with ES module support. Verify before locking; fall back to `module: CommonJS` if Jint's ESM support is incomplete (the `import` calls collapse to `require`, behavior unchanged from the runtime perspective).
- **Where does `scripts/` live in the repo?** Top-level alongside `Map.Server/`, or under `Map.Server/scripts/`? Lean: top-level — it's content, not server code; CI can build it as a separate job; future tools (translator, content editor) target it independently.
- **`dist/` gitignored or committed?** Lean: gitignored; deploy pipeline runs `tsc` before packaging.
- **Hot reload coarseness** — restart-message-only for Phase 1, live diff in Phase 2.

## History

- **2026-05-17** — Plan rewritten around Jint + TS modules. No implementation yet.
- **2026-05-17** — Original plan (rAthena `.txt` parser, brace-aware body reader, conf-import walker) discarded — superseded by the TS-authoring decision in [README.md](README.md).
