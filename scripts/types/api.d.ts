// Ambient declarations for the host-injected scripting API.
//
// The C# map server (Map.Server/Scripting/) injects five registrars
// (registerNpc / registerFloatingNpc / registerShop / registerWarp /
// registerSpawn) into the global scope before evaluating dist/main.js.
// Side-effect imports starting at main.ts trigger every register*() call
// at module-evaluation time; the accumulated registrations populate
// INpcRegistry and drive the at-boot NpcSpawnService.
//
// This file is the source of truth for the author-facing API surface.
// Drift between this file and the C# Records/ records is the highest-
// priority bug in the scripting system.

declare global {
    // ===== Registrars =======================================================
    //
    // Every registrar takes *varargs*. The idiomatic pattern is:
    //
    //     // kafra.ts
    //     export const kafra: NpcRegistration = { map: "prontera", ... };
    //
    //     // index.ts
    //     import { kafra } from "./kafra";
    //     import { guards } from "./guards";           // NpcRegistration[]
    //     import { libraryCurator } from "./library_curator";
    //     registerNpc(kafra, libraryCurator, ...guards);
    //
    // The single-arg form `registerNpc(kafra)` still works (varargs with one
    // entry). Spreading arrays — `...guards` — registers every element.

    /** Scripted NPCs with world positions and event hooks. */
    function registerNpc(...npcs: NpcRegistration[]): void;

    /** Event-only scripts with no world position. Replaces rAthena's `-`
     *  map sentinel for floating script blocks. */
    function registerFloatingNpc(...npcs: FloatingNpcRegistration[]): void;

    /** Declarative shops. The `kind` discriminator selects payment mode. */
    function registerShop(...shops: ShopRegistration[]): void;

    /** Declarative warp portals. */
    function registerWarp(...warps: WarpRegistration[]): void;

    /** Declarative mob spawn points. */
    function registerSpawn(...spawns: SpawnRegistration[]): void;
}

// ===== Registration shapes =================================================

export interface NpcRegistration {
    /** Map name (rAthena map_index, no .gat suffix). */
    map: string;
    x: number;
    y: number;
    /** Facing direction, 0..7. 0 = north, clockwise. Default 0. */
    dir?: number;
    /** Numeric sprite class id, matches rAthena's NPC sprite ids. */
    sprite: number;
    /** Display name shown to clients. Must be globally unique. */
    name: string;
    /** When set, the NPC also fires onTouch when a player walks into the area. */
    triggerArea?: { xs: number; ys: number };

    onClick?: NpcHandler;
    onTouch?: NpcHandler;
    onInit?: NpcHandler;
    /** Recurring timers keyed by interval in milliseconds.
     *  `{ 5000: fn, 30000: fn }` fires fn at 5s and 30s after `addTimer` (Phase 5). */
    onTimer?: Record<number, NpcHandler>;
    onPCLogin?: NpcHandler;
    onPCDeath?: NpcHandler;
    onPCKill?: NpcHandler;
    onNPCKill?: NpcHandler;
}

export interface FloatingNpcRegistration {
    /** Unique identifier. Used by Phase 5's `doevent("Name::OnFoo")` dispatch. */
    name: string;
    onInit?: NpcHandler;
    onTimer?: Record<number, NpcHandler>;
    /** Clock-driven hooks. Key is a 24-hour `HHMM` string, e.g. `"0000"` = midnight. */
    onClock?: Record<string, NpcHandler>;
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
    map: string;
    x: number;
    y: number;
    dir?: number;
    sprite: number;
    name: string;
}

export interface ShopItem {
    itemId: number;
    price: number;
    /** Per-item discount percentage. itemshop / pointshop only. */
    discount?: number;
}

export interface MarketShopItem {
    itemId: number;
    price: number;
    /** Per-item stock count. Decrements on purchase. */
    stock: number;
}

export interface WarpRegistration {
    from: { map: string; x: number; y: number };
    /** Trigger half-extent. The active area is (x-xs..x+xs, y-ys..y+ys). */
    area: { xs: number; ys: number };
    to: { map: string; x: number; y: number };
    /** `warp2` triggers for hidden players too. Default `"warp"`. */
    type?: "warp" | "warp2";
}

export interface SpawnRegistration {
    map: string;
    /** Spawn area. Omit for "anywhere walkable on the map". */
    area?: { x: number; y: number; xs: number; ys: number };
    mobId: number;
    amount: number;
    respawn?: { baseMs: number; jitterMs?: number };
    boss?: boolean;
    /** Display name override. Empty / undefined uses mob_db name. */
    name?: string;
    /** Event label fired on death. Phase 5+. */
    onDeath?: string;
    /** Size override. 0 = mob_db default, 1 = small, 2 = large. */
    size?: 0 | 1 | 2;
    /** AI mode override. */
    ai?: number;
}

// ===== Hook signature ======================================================

/**
 * NPC hook handlers are JS generator functions. Author writes
 * `function* onClick(ctx) { ... }` (or `*onClick(ctx) { ... }` in object
 * shorthand) and yields each step of the dialog.
 *
 * Why generators instead of async/await: Jint 4.0.3's Promise event-loop
 * is experimental and hangs `Engine.Execute` when a script awaits a
 * Promise the host hasn't resolved yet. Generators sidestep that machinery
 * — the host calls `iter.next()` to advance one step at a time.
 *
 * The yielded value is the return of a `ctx.<method>(...)` call. Each
 * method returns a tagged step descriptor; the dispatcher reads `kind`
 * and sends the matching packet.
 *
 * Note: do NOT write `const choice = yield ctx.select(...)`. Jint drops
 * the yielded value when `yield` is the RHS of an assignment. Instead,
 * yield the step, then read `ctx.lastSelection` in a separate statement:
 *
 *     yield ctx.select(["A", "B"]);
 *     const choice = ctx.lastSelection;  // 1-based; 0 if Escaped
 */
export type NpcHandler = (ctx: NpcContext) => Generator<DialogStep, void, void>;

/** Discriminated union returned by ctx.<method>(...). Opaque to authors. */
export type DialogStep =
    | { kind: "mes"; text: string }
    | { kind: "next" }
    | { kind: "menu"; options: string[] }
    | { kind: "close" };

// ===== Runtime context types ===============================================
//
// These types describe the *shape* of the context object the host passes to
// hook closures. Phase 1 captures the closures but never invokes them, so
// these types are aspirational — they let authors write Phase-2-ready code
// today without touching anything when the runtime catches up.

export interface NpcContext {
    /** The NPC the hook is bound to. */
    npc: NpcInfo;
    /** Attached player. Null for `onInit`, `onTimer`, and `onClock` hooks. */
    player: PlayerContext | null;
    /** World-level queries and broadcasts. */
    world: WorldOps;

    /**
     * Result of the most recent `yield ctx.select(...)` / `yield ctx.menu(...)`.
     * 1-based; 0 if the player closed the menu (Escape). Read this AFTER the
     * yield — never inside the yield expression (see DialogStep / NpcHandler
     * for the Jint quirk that motivates this shape).
     */
    lastSelection: number;

    // --- Yielding dialog primitives (Phase 2) ---
    //
    // Each method returns a tagged DialogStep that the author yields:
    //   yield ctx.mes("hello");
    //   yield ctx.next();
    //   yield ctx.select(["A", "B"]);
    //   const choice = ctx.lastSelection;
    //   yield ctx.close();

    /** Append a line of dialog text. Does NOT suspend on its own — yields
     *  return immediately. Multi-line dialog is several `mes` yields. */
    mes(text: string): DialogStep;
    /** Show a "Next" button; suspends until the client clicks Next. */
    next(): DialogStep;
    /** Show a menu of options. After the yield, `ctx.lastSelection` holds
     *  the 1-based pick (0 if Escaped). */
    menu(options: string[]): DialogStep;
    /** Alias of `menu`. Both yield a `{ kind: "menu" }` step. */
    select(options: string[]): DialogStep;
    /** Show a "Close" button; suspends, then ends the dialog. */
    close(): DialogStep;
}

export interface NpcInfo {
    map: string;
    x: number;
    y: number;
    dir: number;
    name: string;
    sprite: number;
    /** NPC-local variables (rAthena `.var` scope). Memory-only; reset on
     *  script reload. Phase 1 exposes the field; Phase 4 wires persistence. */
    vars: Record<string, unknown>;
}

/** Player surface. Field list lands in Phase 3 alongside the corresponding
 *  builtins; for now authors should treat this as opaque. */
export interface PlayerContext {
    readonly id: number;
    readonly accountId: number;
    name: string;
}

/** World-level operations. Surface lands in Phase 3. */
export interface WorldOps {
    /** Server time in milliseconds since boot. */
    now(): number;
}
