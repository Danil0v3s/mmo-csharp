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
 * NPC hook handlers are async functions. Author writes
 * `async onClick(ctx) { ... }` or `onClick: async (ctx) => { ... }` and
 * awaits each step of the dialog directly:
 *
 *     async onClick(ctx) {
 *         await ctx.mes("Welcome");
 *         await ctx.next();
 *         const choice = await ctx.select(["A", "B"]);
 *         if (choice === 1) await ctx.mes("Picked A");
 *         await ctx.close();
 *     }
 *
 * The runtime (ClearScript V8) marshals C# `Task<T>` to JS `Promise<T>`,
 * so each suspending host call is a real Promise the script awaits. The
 * dispatcher resolves the Promise when the client sends the matching
 * packet.
 */
export type NpcHandler = (ctx: NpcContext) => Promise<void> | void;

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

    // --- Dialog primitives. Each returns a Promise; author writes
    //     `await ctx.<method>(...)` to suspend until the client responds.

    /** Append a line of dialog text. Resolves immediately — author still
     *  `await`s for ordering, but no client round-trip. */
    mes(text: string): Promise<void>;
    /** Show a "Next" button; resolves when the client clicks Next. */
    next(): Promise<void>;
    /** Show a menu of options. Resolves with the 1-based selection
     *  (0 if the player Escaped). */
    menu(options: string[]): Promise<number>;
    /** Alias of `menu`. Both render as the same `ZC_MENU_LIST` packet. */
    select(options: string[]): Promise<number>;
    /** Show a "Close" button; resolves when the client clicks Close. The
     *  dialog ends after the script returns. */
    close(): Promise<void>;
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

/**
 * Player surface — what scripts can read and mutate about the player who
 * clicked the NPC. Reads project from the loaded char-state snapshot and
 * the in-memory entity. Writes broadcast a ZC_PAR_CHANGE so the client UI
 * updates immediately.
 *
 * Persistence: mutating fields that live on the loaded char snapshot
 * (zeny in this slice) take effect for the player's current session but
 * are NOT yet pushed back to the char-server / DB on save — the
 * SaveCharacterState IPC currently only flags Online. Persistence is a
 * follow-up slice. Authors should write against this surface; durability
 * comes later.
 */
export interface PlayerContext {
    // Identity / loaded snapshot — read-only this slice.
    readonly id: number;
    readonly accountId: number;
    readonly name: string;
    readonly sex: number;        // 0 = female, 1 = male (rAthena convention)
    readonly classId: number;    // job id, e.g. JOB_NOVICE = 0
    readonly baseLevel: number;
    readonly jobLevel: number;
    readonly str: number;
    readonly agi: number;
    readonly vit: number;
    readonly int: number;        // mapped to `intStat` on the host (TS reserves `int`)
    readonly dex: number;
    readonly luk: number;
    readonly hp: number;
    readonly maxHp: number;
    readonly sp: number;
    readonly maxSp: number;

    /** Currency. Setter broadcasts SP_ZENY; clamps to ≥ 0. */
    zeny: number;

    /**
     * Memory-only variable bag (rAthena `@var` scope). Properties survive
     * for the player's connected session and reset on disconnect.
     * Authors write `ctx.player.session.visits = 1` and read it back.
     */
    session: Record<string, unknown>;

    /** Restore HP and optionally SP. Both clamp to their max. */
    heal(hp: number, sp?: number): Promise<void>;

    /** Send a self-only system chat line to the player (debug / quick feedback). */
    message(text: string): Promise<void>;
}

/** World-level operations. Surface lands in Phase 3. */
export interface WorldOps {
    /** Server time in milliseconds since boot. */
    now(): number;
}
