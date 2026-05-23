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
//
// IMPLEMENTATION STATUS: most methods on `ctx.*` below are STUBS — the
// surface is wired, but the internals log "not yet implemented" and
// return placeholder values. Authors can write scripts against the full
// API today; behavior lands in follow-up commits. Methods that are
// fully wired today: ctx.mes / next / menu / select / close, and on
// ctx.player: identity reads, str/agi/vit/int/dex/luk, hp/sp/zeny,
// session/perm/account/accountGlobal, heal(), message(). Everything
// else is stubbed.

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

    /** Declarative map flags. Mirrors rAthena's `npc/re/mapflag/*.txt`. */
    function registerMapFlag(...flags: MapFlagRegistration[]): void;

    /** Item scripts — onUse for consumables, onEquip/onUnequip for gear.
     *  Most entries are generated from rAthena's item_db by
     *  Tools.ItemScriptConvert; hand-written items override generated ones
     *  via the duplicate-id check in the registry. */
    function registerItem(...items: ItemRegistration[]): void;

    /** Combos — fire onActive when every listed aegis-named member item
     *  is equipped simultaneously. Generated from rAthena's
     *  item_combo_db + item_combo_member_db by Tools.ItemScriptConvert. */
    function registerCombo(...combos: ComboRegistration[]): void;
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
    /** Recurring timers keyed by interval in milliseconds. */
    onTimer?: Record<number, NpcHandler>;
    onPCLogin?: NpcHandler;
    onPCDeath?: NpcHandler;
    onPCKill?: NpcHandler;
    onNPCKill?: NpcHandler;
}

export interface FloatingNpcRegistration {
    /** Unique identifier. Used by `ctx.doevent("Name::OnFoo")` dispatch. */
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

export interface MapFlagRegistration {
    map: string;
    /** Flag name verbatim from rAthena: `pvp`, `gvg`, `nobranch`, `night`, … */
    flag: string;
    /** Optional value when the flag carries one (e.g. `"100"` for `restricted`). */
    value?: string;
}

export interface ItemRegistration {
    /** Numeric item id (rAthena item_db.id). Globally unique across the bundle.
     *
     *  This is the *only* required field — every other item-db column
     *  (name_aegis, name_english, type, weight, slots, …) already lives
     *  in SQL and is owned by IItemCatalog. The registrar attaches hooks;
     *  the catalog provides the rest. */
    id: number;

    /** Fires when a player uses the item via CZ_USE_ITEM (potions, scrolls,
     *  boxes). Async — the closure may await player ops. */
    onUse?: ItemUseHandler;
    /** Fires on equip success. Sync — the closure typically calls
     *  `ctx.bonus(...)` / `ctx.bonus2(...)` to populate the active
     *  EquipBonusBundle. Equip recalc runs on the game loop and must not suspend. */
    onEquip?: ItemEquipHandler;
    /** Fires on unequip. Sync, same contract as onEquip. */
    onUnequip?: ItemEquipHandler;
}

export interface ComboRegistration {
    /** Original combo_id from rAthena item_combo_db. Preserved for traceability. */
    comboId: number;
    /** Aegis names of every item that must be simultaneously equipped to fire. */
    members: string[];
    /** Fires during equip recalc when every member is equipped. Sync. */
    onActive?: ItemEquipHandler;
}

/** Async hook for item-use (potions, scrolls, boxes). */
export type ItemUseHandler = (ctx: ItemUseContext) => Promise<void> | void;

/** Sync hook for equip/unequip and combo activation. Must not return a Promise. */
export type ItemEquipHandler = (ctx: ItemEquipContext) => void;

// ===== Item runtime context ===============================================

export interface ItemUseContext {
    /** The acting player. Same surface as NpcContext.player. */
    player: PlayerContext;
    /** World ops (announce, spawn, etc.). Same surface as NpcContext.world. */
    world: WorldOps;
    /** The item triggering this hook (id, refine, slot, etc.). */
    item: ItemInfo;

    rand(max: number): number;
    randRange(min: number, max: number): number;
}

export interface ItemEquipContext {
    /** The acting player. Read-only during recalc; helpers like getrefine
     *  delegate through `ctx.*` instead of mutating PlayerContext. */
    player: PlayerContext;
    /** The item triggering this hook. For combo onActive, this is the
     *  first member item in the equipped set. */
    item: ItemInfo;

    // ===== Bonus mutation (writes to the active EquipBonusBundle) =====

    /** rAthena `bonus bKey,val;` — flat numeric bonus. */
    bonus(key: string, value: number): void;
    /** rAthena `bonus2 bKey,idx,val;` — indexed bonus (race, ele, class…). */
    bonus2(key: string, index: string | number, value: number): void;
    /** rAthena `bonus3`. */
    bonus3(key: string, a: string | number, b: string | number, value: number): void;
    /** rAthena `bonus4`. */
    bonus4(key: string, a: string | number, b: string | number, c: string | number, value: number): void;
    /** rAthena `bonus5`. */
    bonus5(key: string, a: string | number, b: string | number, c: string | number, d: string | number, value: number): void;

    /** rAthena `autobonus "{ body }", rate, duration, [atkType];` */
    autobonus(body: string, rate: number, durationMs: number, atkType?: string | number): void;
    /** rAthena `autobonus2` — fires when hit. */
    autobonus2(body: string, rate: number, durationMs: number, atkType?: string | number): void;
    /** rAthena `autobonus3` — fires on a specific skill. */
    autobonus3(body: string, rate: number, durationMs: number, skillName: string): void;

    // ===== Equip queries =====

    getrefine(): number;
    getequiprefinerycnt(slot: string | number): number;
    getequipid(slot: string | number): number;
    getequipweaponlv(slot?: string | number): number;
    getenchantgrade(slot?: string | number): number;

    // ===== PC queries (subset useful in bonus context) =====

    readparam(name: string | number): number;
    /** rAthena getskilllv("AL_HEAL") — 0 until skill engine wires in. */
    getskilllv(skillName: string): number;

    rand(max: number): number;
}

export interface ItemInfo {
    readonly id: number;
    readonly nameAegis: string;
    readonly refine: number;
    readonly slot: number;
    readonly amount: number;
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
    /** Event label fired on death. */
    onDeath?: string;
    /** Size override. 0 = mob_db default, 1 = small, 2 = large. */
    size?: 0 | 1 | 2;
    /** AI mode override. */
    ai?: number;
}

// ===== Hook signature ======================================================

export type NpcHandler = (ctx: NpcContext) => Promise<void> | void;

// ===== Runtime context (ctx) ==============================================

export interface NpcContext {
    /** The NPC the hook is bound to. Read-only facts + display/movement ops. */
    npc: NpcInfo;
    /** Attached player. Null for `onInit`, `onTimer`, and `onClock` hooks. */
    player: PlayerContext | null;
    /** World-level queries, broadcasts, mob spawning, map flags, etc. */
    world: WorldOps;
    /** Party ops (create / leader / members). */
    party: PartyOps;
    /** Guild ops (info / master / members). */
    guild: GuildOps;
    /** Instance ops (create / enter / vars). */
    instance: InstanceOps;
    /** Battleground ops. */
    bg: BattlegroundOps;
    /** Channel ops. */
    channel: ChannelOps;

    // --- Dialog primitives. Each returns a Promise; author writes
    //     `await ctx.<method>(...)` to suspend until the client responds.

    /** Append a line of dialog text. Resolves immediately. */
    mes(text: string): Promise<void>;
    /** Show a "Next" button; resolves when the client clicks Next. */
    next(): Promise<void>;
    /** Show a menu of options. Resolves with the 1-based selection
     *  (0 if the player Escaped). */
    menu(options: string[]): Promise<number>;
    /** Alias of `menu`. Both render as the same `ZC_MENU_LIST` packet. */
    select(options: string[]): Promise<number>;
    /** Show a "Close" button; resolves when the client clicks Close. */
    close(): Promise<void>;

    // --- Flow utilities ----------------------------------------------------

    /** Numeric input dialog. Resolves with the entered value. */
    input(min?: number, max?: number, defaultValue?: number): Promise<number>;
    /** String input dialog. */
    inputString(defaultValue?: string): Promise<string>;
    /** Suspend the dialog for the given duration. */
    sleep(milliseconds: number): Promise<void>;
    /** Fire an event label on another NPC. Target: `"NpcName::OnLabel"`. */
    doevent(eventTarget: string): Promise<void>;
    /** Schedule a one-shot timer that fires the event label. */
    addTimer(milliseconds: number, eventTarget: string): Promise<void>;
    delTimer(eventTarget: string): Promise<void>;
    addPlayerTimer(charId: number, milliseconds: number, eventTarget: string): Promise<void>;
    /** Call another NPC's function. Stubbed — use JS imports in TS. */
    callfunc(functionName: string, ...args: unknown[]): Promise<unknown>;
    /** End the script early. */
    end(): Promise<void>;
    /** Clear current dialog content but keep the dialog open. */
    clear(): Promise<void>;

    /** RNG helper — `[0, max)`. */
    rand(max: number): number;
    /** RNG helper — `[min, max]` inclusive. */
    randRange(min: number, max: number): number;
}

// ===== NPC info + display ops ==============================================

export interface NpcInfo {
    readonly map: string;
    readonly x: number;
    readonly y: number;
    readonly dir: number;
    readonly name: string;
    readonly sprite: number;
    /** NPC-local variables. Memory-only; reset on script reload. */
    vars: Record<string, unknown>;

    // Display / movement (stubs).
    setDisplay(displayName: string, classId: number, size?: number): Promise<void>;
    speed(value: number): Promise<void>;
    walkTo(x: number, y: number): Promise<void>;
    stop(clearTarget?: boolean): Promise<void>;
    moveTo(x: number, y: number, dir?: number): Promise<void>;
    hide(): Promise<void>;
    show(): Promise<void>;
    disable(): Promise<void>;
    enable(): Promise<void>;
    duplicateDynamic(charId?: number): Promise<void>;

    // Shop ops.
    shopSet(items: ShopItem[]): Promise<void>;
    shopAdd(items: ShopItem[]): Promise<void>;
    shopDel(itemIds: number[]): Promise<void>;
    shopAttach(flag?: boolean): Promise<void>;
    shopUpdate(itemId: number, price: number, stock?: number): Promise<void>;

    // Waiting room.
    createWaitingRoom(
        roomName: string,
        limit: number,
        opts?: { eventLabel?: string; trigger?: number; zeny?: number; minLv?: number; maxLv?: number }
    ): Promise<void>;
    removeWaitingRoom(): Promise<void>;
    enableWaitingRoom(): Promise<void>;
    disableWaitingRoom(): Promise<void>;
    getWaitingRoomState(infoType: number): number;
    warpWaitingPc(map: string, x: number, y: number, count?: number): Promise<void>;
    kickWaitingRoomUser(charName: string): Promise<void>;
    kickAllWaitingRoom(): Promise<void>;
    getWaitingRoomUsers(): number;

    npcTimer(infoType: number): number;
    getNpcId(type: number): number;
    npcInfo(type: number): string;
}

// ===== Player surface ======================================================

export interface PlayerContext {
    // Identity / loaded snapshot — read-only.
    readonly id: number;
    readonly charId: number;
    readonly accountId: number;
    readonly name: string;
    readonly sex: number;        // 0 = female, 1 = male
    readonly classId: number;
    readonly baseLevel: number;
    readonly jobLevel: number;
    readonly groupId: number;
    readonly gmLevel: number;
    readonly partyId: number;
    readonly guildId: number;
    readonly weight: number;
    readonly maxWeight: number;
    readonly mapName: string;
    readonly x: number;
    readonly y: number;
    readonly dir: number;

    // Stats — read-only.
    readonly str: number;
    readonly agi: number;
    readonly vit: number;
    readonly int: number;
    readonly dex: number;
    readonly luk: number;
    readonly statusPoint: number;
    readonly skillPoint: number;

    /** HP setter clamps to [0, maxHp] and broadcasts SP_HP. */
    hp: number;
    readonly maxHp: number;
    /** SP setter clamps to [0, maxSp] and broadcasts SP_SP. */
    sp: number;
    readonly maxSp: number;
    readonly ap: number;
    readonly maxAp: number;

    /** Currency. Setter broadcasts SP_ZENY; clamps to ≥ 0. */
    zeny: number;

    /** Memory-only variable bag (rAthena `@var`). */
    session: Record<string, unknown>;
    /** Per-character permanent (rAthena bare `var`). */
    perm: Record<string, number | string>;
    /** Per-account local (rAthena `#var`). */
    account: Record<string, number | string>;
    /** Per-account global (rAthena `##var`). */
    accountGlobal: Record<string, number | string>;

    // Sub-surfaces.
    quest: QuestOps;
    achievement: AchievementOps;
    storage: StorageOps;
    cart: CartOps;
    mail: MailOps;
    pet: PetOps;
    hom: HomOps;
    merc: MercOps;

    // ===== Heal / SP / AP =====
    heal(hp: number, sp?: number): Promise<void>;
    healAp(ap: number): Promise<void>;
    itemHeal(hp: number, sp: number): Promise<void>;
    percentHeal(hpPercent: number, spPercent: number): Promise<void>;
    recovery(type: number, opts?: { option?: number; reviveFlag?: number; mapName?: string }): Promise<void>;

    // ===== Experience / level =====
    giveExp(baseExp: number, jobExp: number, opts?: { quest?: boolean }): Promise<void>;
    baseExpRatio(percent: number, level?: number): number;
    jobExpRatio(percent: number, level?: number): number;

    // ===== Job / class =====
    jobChange(jobId: number, opts?: { upper?: number }): Promise<void>;
    changeBase(classId: number): Promise<void>;
    changeSex(): Promise<void>;
    jobName(jobId: number): string;

    // ===== Movement =====
    warp(map: string, x: number, y: number): Promise<void>;
    savePoint(map: string, x: number, y: number, rangeX?: number, rangeY?: number): Promise<void>;
    save(map: string, x: number, y: number): Promise<void>;
    getSavePoint(): { map: string; x: number; y: number } | null;
    pushPc(direction: number, cells: number): Promise<void>;
    warpPartner(map: string, x: number, y: number): Promise<void>;

    // ===== Items: give / take / count =====
    giveItem(itemId: number, amount?: number, opts?: ItemOpts): Promise<void>;
    giveRentItem(itemId: number, seconds: number, opts?: ItemOpts): Promise<void>;
    giveNamedItem(itemId: number, inscribeName: string): Promise<void>;
    giveRandomGroupItem(groupId: number, qty?: number, opts?: { subGroup?: number; identify?: boolean }): Promise<void>;
    giveGroupItem(groupId: number, opts?: { identify?: boolean }): Promise<void>;
    delItem(itemId: number, amount?: number, opts?: ItemOpts): Promise<void>;
    delItemAtIndex(index: number, amount?: number): Promise<void>;
    countItem(itemId: number, opts?: ItemOpts): number;
    countBound(boundType?: number): number;
    hasItem(itemId: number, amount?: number, opts?: ItemOpts): boolean;
    clearItems(): Promise<void>;
    consumeItem(itemId: number): Promise<void>;
    searchItem(namePart: string): number[];
    getInventory(): InventoryEntry[];
    mergeItems(itemId?: number): Promise<void>;
    identifyAll(type?: number): Promise<void>;
    checkWeight(itemId: number, amount: number, more?: Array<{ itemId: number; amount: number }>): boolean;

    // ===== Equipment =====
    getEquipId(slot: number): number;
    getEquipName(slot: number): string;
    getEquipUniqueId(slot: number): number;
    getEquipRefine(slot: number): number;
    getEquipWeaponLv(slot?: number): number;
    getEquipArmorLv(slot?: number): number;
    getEquipCardCount(slot: number): number;
    getEquipCardId(slot: number, cardSlot: number): number;
    getEnchantGrade(slot?: number): number;
    isEquipped(slot: number): boolean;
    isEquipEnableRef(slot: number): boolean;
    getItemPos(slot: number): number;
    equip(itemId: number): Promise<void>;
    autoEquip(itemId: number, enable: boolean): Promise<void>;
    unequip(slot: number): Promise<void>;
    delEquip(slot: number): Promise<void>;
    breakEquip(slot: number): Promise<void>;
    successRefine(slot: number, count?: number): Promise<void>;
    failRefine(slot: number): Promise<void>;
    downRefine(slot: number, count?: number): Promise<void>;
    repair(brokenIndex: number): Promise<void>;
    repairAll(): Promise<void>;
    removeCards(slot: number, success: boolean, type?: number): Promise<void>;
    getBrokenId(number: number): number;

    // ===== Skills =====
    skillLv(skillId: number): number;
    addSkill(skillId: number, level: number, opts?: { permanent?: boolean }): Promise<void>;
    itemSkill(skillId: number, level: number, keepRequirement?: boolean): Promise<void>;
    getSkillList(): SkillEntry[];
    skillPointCount(): number;
    basicSkillCheck(): boolean;

    // ===== Looks / mounts =====
    setLook(type: number, value: number): Promise<void>;
    changeLook(type: number, value: number): Promise<void>;
    getLook(type: number): number;
    setFont(font: number): Promise<void>;
    setCart(type?: number): Promise<void>;
    setFalcon(flag?: boolean): Promise<void>;
    setRiding(flag?: boolean): Promise<void>;
    setDragon(color?: number): Promise<void>;
    setMadogear(flag?: boolean, type?: number): Promise<void>;
    setMounting(): Promise<void>;
    checkCart(): boolean;
    checkFalcon(): boolean;
    checkRiding(): boolean;
    checkDragon(): boolean;
    checkMadogear(): boolean;
    checkWug(): boolean;
    isMounting(): boolean;

    // ===== Options / status =====
    setOption(option: number, flag?: boolean): Promise<void>;
    checkOption(option: number): boolean;
    checkOption1(option: number): boolean;
    checkOption2(option: number): boolean;
    scStart(type: number, durationMs: number, opts?: { val1?: number; val2?: number; val3?: number; val4?: number }): Promise<void>;
    scEnd(type?: number): Promise<void>;
    getStatus(effectType: number, infoType?: number): number;
    isDead(): boolean;
    recalculateStat(): Promise<void>;
    needStatusPoint(statType: number, value: number): number;

    // ===== Reset =====
    resetStatus(): Promise<void>;
    resetSkill(): Promise<void>;
    resetFeel(): Promise<void>;
    resetHate(): Promise<void>;

    // ===== Display effects =====
    message(text: string): Promise<void>;
    dispBottom(text: string, color?: number): Promise<void>;
    showScript(text: string, flag?: number): Promise<void>;
    cutin(filename: string, position: number): Promise<void>;
    emotion(emoNum: number, target?: number): Promise<void>;
    miscEffect(effectNum: number): Promise<void>;
    soundEffect(filename: string, type?: number): Promise<void>;
    playBgm(filename: string): Promise<void>;
    viewpoint(action: number, x: number, y: number, point: number, color: number): Promise<void>;
    showDigit(value: number, type?: number): Promise<void>;
    hatEffect(hatEffectId: number, state: boolean): Promise<void>;

    // ===== UI windows =====
    openStorage(mode?: number): Promise<void>;
    openBank(): Promise<void>;
    openMail(): Promise<void>;
    openAuction(): Promise<void>;
    openRefineUi(): Promise<void>;
    openStylist(): Promise<void>;
    openDressRoom(): Promise<void>;
    openRoulette(): Promise<void>;
    openQuestUi(questId?: number): Promise<void>;
    openEnchantGrade(): Promise<void>;
    openLaphineSynthesis(itemId?: number): Promise<void>;
    openLaphineUpgrade(): Promise<void>;
    openItemEnchant(luaIndex: number): Promise<void>;
    openItemReform(itemId?: number): Promise<void>;
    specialPopup(popupId: number): Promise<void>;
    openTips(tipId: number): Promise<void>;
    readBook(bookId: number, page?: number): Promise<void>;

    // ===== Spirit balls =====
    addSpiritBall(count: number, durationMs: number): Promise<void>;
    delSpiritBall(count: number): Promise<void>;
    countSpiritBall(): number;

    // ===== Reputation / fame =====
    getReputation(type: number): number;
    setReputation(type: number, points: number): Promise<void>;
    addReputation(type: number, points: number): Promise<void>;
    getFame(): number;
    addFame(amount: number): Promise<void>;
    getFameRank(): number;

    // ===== Marriage / family =====
    marry(spouseName: string): Promise<void>;
    divorce(): Promise<void>;
    adopt(parentName: string, babyName: string): Promise<void>;
    getPartnerId(): number;
    getMotherId(): number;
    getFatherId(): number;
    getChildId(): number;
    isPartnerOn(): boolean;

    // ===== Permissions =====
    permissionCheck(permission: string): boolean;
    permissionAdd(permission: string): Promise<void>;
    permissionRemove(permission: string): Promise<void>;
    guildHasPermission(permission: string): boolean;

    // ===== VIP / macro =====
    vipStatus(type: number): number;
    vipTime(seconds: number): Promise<void>;
    macroDetector(): Promise<void>;

    // ===== Misc =====
    charInfo(type: number): string;
    readParam(paramNumber: number): number;
    charId4Type(type: number): number;
    charIp(): string;
    kick(): Promise<void>;
    ignoreTimeout(flag: boolean): Promise<void>;
    autoLoot(rate?: number): number;
    hasAutoLoot(): boolean;
    jobCanEnterMap(map: string, jobId?: number): boolean;
    checkVending(): boolean;
    checkChatting(): boolean;
    checkIdle(): boolean;
    navigateTo(map: string, x?: number, y?: number, flag?: number, hideWindow?: boolean, monsterId?: number): Promise<void>;
    clanJoin(clanId: number): Promise<void>;
    clanLeave(): Promise<void>;
    cameraInfo(range: number, rotation: number, latitude: number): unknown;
}

export interface ItemOpts {
    identify?: boolean;
    refine?: number;
    attribute?: number;
    cards?: [number, number, number, number];
    bound?: number;
    grade?: number;
    randomOptions?: Array<{ id: number; value: number; param: number }>;
}

export interface InventoryEntry {
    index: number;
    itemId: number;
    amount: number;
    identified: boolean;
    refine: number;
    cards: [number, number, number, number];
    bound: number;
    grade: number;
    expireTime?: number;
}

export interface SkillEntry {
    id: number;
    level: number;
    flag: number;
}

// ===== Sub-surfaces ========================================================

export interface QuestOps {
    add(questId: number): Promise<void>;
    complete(questId: number): Promise<void>;
    erase(questId: number): Promise<void>;
    change(fromId: number, toId: number): Promise<void>;
    /** Mode: "any" | "playtime" | "hunting". */
    check(questId: number, mode?: "any" | "playtime" | "hunting"): number;
    isBegin(questId: number): boolean;
    showEvent(icon: number, markColor?: number): Promise<void>;
    refreshInfo(): Promise<void>;
    showInfo(icon: number, markColor?: number, condition?: string): Promise<void>;
}

export interface AchievementOps {
    add(achievementId: number): Promise<void>;
    remove(achievementId: number): Promise<void>;
    complete(achievementId: number): Promise<void>;
    exists(achievementId: number): boolean;
    info(achievementId: number, type: number): number;
    update(achievementId: number, type: number, value: number): Promise<void>;
}

export interface StorageOps {
    open(mode?: number): Promise<void>;
    openExtra(storageId: number, mode?: number): Promise<void>;
    countItem(itemId: number, opts?: ItemOpts): number;
    delItem(itemId: number, amount: number, opts?: ItemOpts): Promise<void>;
    openGuildStorage(): Promise<void>;
    countGuildItem(itemId: number, opts?: ItemOpts): number;
    delGuildItem(itemId: number, amount: number, opts?: ItemOpts): Promise<void>;
    guildLog(): unknown[];
}

export interface CartOps {
    isEnabled(): boolean;
    countItem(itemId: number, opts?: ItemOpts): number;
    delItem(itemId: number, amount: number, opts?: ItemOpts): Promise<void>;
}

export interface MailOps {
    open(): Promise<void>;
}

export interface PetOps {
    catchPet(itemId: number, flag?: number): Promise<void>;
    makePet(petId: number): Promise<void>;
    birthPet(): Promise<void>;
    openIncubator(): Promise<void>;
    info(type: number): unknown;
    skillBonus(bonusType: number, value: number, durationMs: number, delayMs: number): Promise<void>;
    skillSupport(skillId: number, skillLv: number, delayMs: number, hpPct: number, spPct: number): Promise<void>;
    skillAttack(skillId: number, skillLv: number, rate: number, bonusRate: number): Promise<void>;
    skillAttack2(skillId: number, damage: number, attacks: number, rate: number, bonusRate: number): Promise<void>;
    recovery(statusType: number, delayMs: number): Promise<void>;
    loot(maxItems: number): Promise<void>;
}

export interface HomOps {
    exists(): boolean;
    isCalled(): boolean;
    info(type: number): unknown;
    evolve(): Promise<void>;
    morph(): Promise<void>;
    mutate(id?: number): Promise<void>;
    shuffle(): Promise<void>;
    addIntimacy(amount: number): Promise<void>;
}

export interface MercOps {
    create(classId: number, contractTimeSec: number): Promise<void>;
    delete(reply?: number): Promise<void>;
    heal(hp: number, sp?: number): Promise<void>;
    scStart(type: number, durationMs: number, val1: number): Promise<void>;
    getCalls(guildType: number): number;
    setCalls(guildType: number, value: number): Promise<void>;
    getFaith(guildType: number): number;
    setFaith(guildType: number, value: number): Promise<void>;
    info(type: number): unknown;
    elementalInfo(type: number): unknown;
}

// ===== World ops ==========================================================

export interface WorldOps {
    /** Server time in milliseconds since boot. */
    now(): number;

    // Announce family.
    announce(message: string, opts?: AnnounceOpts): Promise<void>;
    mapAnnounce(map: string, message: string, opts?: AnnounceOpts): Promise<void>;
    areaAnnounce(map: string, x1: number, y1: number, x2: number, y2: number, message: string, opts?: AnnounceOpts): Promise<void>;
    globalMessage(message: string, fromNpcName?: string): Promise<void>;
    debugMessage(message: string): Promise<void>;
    errorMessage(message: string): Promise<void>;
    logMessage(message: string): Promise<void>;

    // Sound / BGM.
    soundEffectAll(filename: string, type?: number, map?: string, x0?: number, y0?: number, x1?: number, y1?: number): Promise<void>;
    playBgmAll(filename: string, map?: string, x0?: number, y0?: number, x1?: number, y1?: number): Promise<void>;

    // Monster / unit spawning.
    spawnMob(map: string, x: number, y: number, displayName: string, mobId: number, amount?: number, onDeathEvent?: string): Promise<number>;
    spawnAreaMob(map: string, x1: number, y1: number, x2: number, y2: number, displayName: string, mobId: number, amount?: number, onDeathEvent?: string): Promise<number>;
    spawnGuardian(map: string, x: number, y: number, displayName: string, mobId: number, onDeathEvent?: string, guardianIndex?: number): Promise<number>;
    guardianInfo(map: string, guardianIndex: number, type: number): unknown;
    killMonster(map: string, eventLabel: string): Promise<number>;
    killMonsterAll(map: string): Promise<number>;
    mobCount(map: string, eventLabel: string): number;
    respawnGuildOwned(map: string, guildId: number, flag?: number): Promise<void>;
    getRandomMobId(type: number, flag?: number, level?: number): number;
    getMonsterInfo(mobId: number, type: number): unknown;
    getMobDrops(mobId: number): unknown[];
    mobInfo(type: number, mobId: number): string;

    // Unit-level ops (any GID).
    unitWalk(gid: number, x: number, y: number, onArriveEvent?: string): Promise<void>;
    unitWalkToTarget(gid: number, targetGid: number, onArriveEvent?: string): Promise<void>;
    unitAttack(gid: number, targetGid: number, actionType?: number): Promise<void>;
    unitKill(gid: number): Promise<void>;
    unitWarp(gid: number, map: string, x: number, y: number): Promise<void>;
    unitTalk(gid: number, text: string, flag?: number): Promise<void>;
    unitSkillUseId(gid: number, skillId: number, skillLv: number, opts?: UnitSkillOpts): Promise<void>;
    unitSkillUsePos(gid: number, skillId: number, skillLv: number, x: number, y: number, opts?: UnitSkillOpts): Promise<void>;
    unitStopAttack(gid: number): Promise<void>;
    unitStopWalk(gid: number, flag?: number): Promise<void>;
    unitExists(gid: number): boolean;
    getUnitType(gid: number): number;
    getUnitName(gid: number): string;
    setUnitName(gid: number, name: string): Promise<void>;
    getUnitTitle(gid: number): string;
    setUnitTitle(gid: number, title: string): Promise<void>;
    getUnitData(gid: number): unknown;
    setUnitData(gid: number, parameter: number, value: unknown): Promise<void>;
    getUnits(type: number): number[];
    getMapUnits(type: number, map: string): number[];
    getAreaUnits(type: number, map: string, x1: number, y1: number, x2: number, y2: number): number[];

    // User / area queries.
    getMapUsers(map: string): number;
    getAreaUsers(map: string, x1: number, y1: number, x2: number, y2: number): number;
    getServerUsers(type?: number): number;
    isLoggedIn(accountId: number, charId?: number): boolean;
    ridToName(rid: number): string;
    getAreaDropItem(map: string, x1: number, y1: number, x2: number, y2: number, itemId?: number): unknown[];

    // World map / location.
    mapIdToName(mapId: number): string;
    getMapXY(gid: number, type?: number): { map: string; x: number; y: number } | null;
    distance(x0: number, y0: number, x1: number, y1: number): number;
    setCell(map: string, x1: number, y1: number, x2: number, y2: number, type: number, flag: boolean): Promise<void>;
    checkCell(map: string, x: number, y: number, type: number): number;
    getFreeCell(map: string, x?: number, y?: number, rangeX?: number, rangeY?: number, flag?: number): { x: number; y: number } | null;
    setWall(map: string, x: number, y: number, size: number, dir: number, shootable: boolean, name: string): Promise<void>;
    delWall(name: string): Promise<void>;
    checkWall(name: string): boolean;
    makeItem(itemId: number, amount: number, map: string, x: number, y: number, effect?: boolean, opts?: ItemOpts): Promise<void>;
    cleanArea(map: string, x1: number, y1: number, x2: number, y2: number): Promise<void>;
    cleanMap(map: string): Promise<void>;
    warpPortal(srcX: number, srcY: number, toMap: string, toX: number, toY: number): Promise<void>;
    mapWarp(fromMap: string, toMap: string, x: number, y: number, type?: number, id?: number): Promise<void>;
    areaWarp(fromMap: string, x1: number, y1: number, x2: number, y2: number, toMap: string, toX: number, toY: number, toX2?: number, toY2?: number): Promise<void>;
    warpParty(toMap: string, x: number, y: number, partyId: number, fromOpts?: { map?: string; rangeX?: number; rangeY?: number }): Promise<void>;
    warpGuild(toMap: string, x: number, y: number, guildId: number): Promise<void>;
    areaPercentHeal(map: string, x1: number, y1: number, x2: number, y2: number, hp: number, sp: number): Promise<void>;
    attachRid(accountId: number, force?: boolean): Promise<void>;
    addRid(type: number, flag?: number, parameters?: unknown): Promise<void>;
    playerAttached(): number;
    getAttachedRid(): number;

    // Map flags.
    setMapFlag(map: string, flag: number, zone?: string, type?: number): Promise<void>;
    removeMapFlag(map: string, flag: number, zone?: string): Promise<void>;
    getMapFlag(map: string, flag: number, type?: number): number;
    setMapFlagNoSave(map: string, altMap: string, x: number, y: number): Promise<void>;

    // Day / night / pvp / gvg / agit.
    day(): Promise<void>;
    night(): Promise<void>;
    isDay(): boolean;
    isNight(): boolean;
    pvpOn(map: string): Promise<void>;
    pvpOff(map: string): Promise<void>;
    gvgOn(map: string): Promise<void>;
    gvgOff(map: string): Promise<void>;
    gvgOn3(map: string): Promise<void>;
    gvgOff3(map: string): Promise<void>;
    agitStart(era?: number): Promise<void>;
    agitEnd(era?: number): Promise<void>;
    agitCheck(era?: number): boolean;
    flagEmblem(guildId: number): Promise<void>;
    castleName(map: string): string;
    castleData(map: string, type: number): number;

    // Time / weather.
    getTime(type: number): number;
    getTimeTick(tickType: number): number;
    getTimeStr(format: string, maxLength: number, tick?: number): string;

    // Battle / config flags.
    setBattleFlag(flagName: string, value: number, reload?: boolean): Promise<void>;
    getBattleFlag(flagName: string): number;

    // At-commands.
    atCommand(command: string): Promise<void>;
    charCommand(command: string): Promise<void>;
    useAtCommand(command: string): Promise<void>;
    bindAtCommand(command: string, eventTarget: string, atLevel?: number, charLevel?: number): Promise<void>;
    unbindAtCommand(command: string): Promise<void>;

    // Game-info queries.
    itemName(itemId: number): string;
    itemSlots(itemId: number): number;
    itemInfo(itemId: number, type: number): unknown;
    setItemInfo(itemId: number, type: number, value: number): Promise<void>;
    setItemScript(itemId: number, script: string, type?: number): Promise<void>;
    gmLevel(charId?: number): number;
    groupId(charId?: number): number;
    itemLink(itemId: number, opts?: ItemOpts): string;
}

export interface AnnounceOpts {
    flag?: number;
    color?: number;
    fontSize?: number;
    fontType?: number;
    fontAlign?: number;
    fontY?: number;
}

export interface UnitSkillOpts {
    targetId?: number;
    castTime?: number;
    cancel?: number;
    lineId?: number;
    ignoreRange?: boolean;
}

// ===== Party / guild / instance / bg / channel ============================

export interface PartyOps {
    getName(partyId: number): string;
    getMembers(partyId: number, type?: number): unknown[];
    getLeader(partyId: number, type?: number): number;
    isLeader(partyId?: number): boolean;
    create(name: string, leaderCharId?: number, itemShare?: boolean, itemShareType?: number): Promise<number>;
    destroy(partyId: number): Promise<void>;
    addMember(partyId: number, charId: number): Promise<void>;
    delMember(charId: number, partyId?: number): Promise<void>;
    changeLeader(partyId: number, charId: number): Promise<void>;
    changeOption(partyId: number, option: number, flag: boolean): Promise<void>;
}

export interface GuildOps {
    getName(guildId: number): string;
    getMaster(guildId: number): string;
    getMasterId(guildId: number): number;
    info(guildId: number, type: number): number;
    getMembers(guildId: number, type?: number): unknown[];
    getSkillLv(guildId: number, skillId: number): number;
    getAlliance(g1: number, g2: number): number;
    getMapUsers(map: string, guildId: number): number;
    changeMaster(guildId: number, newMasterName: string): Promise<void>;
    requestInfo(guildId: number, eventLabel?: string): Promise<void>;
}

export interface InstanceOps {
    create(name: string, mode?: number, ownerId?: number): Promise<number>;
    destroy(instanceId?: number): Promise<void>;
    enter(name: string, x?: number, y?: number, charId?: number, instanceId?: number): Promise<number>;
    npcName(npcName: string, instanceId?: number): string;
    mapName(map: string, instanceId?: number): string;
    id(mode?: number): number;
    warpAll(map: string, x: number, y: number, instanceId?: number, flag?: number): Promise<void>;
    announce(instanceId: number, text: string, flag?: number, opts?: AnnounceOpts): Promise<void>;
    checkParty(partyId: number, amount?: number, minLv?: number, maxLv?: number): boolean;
    checkGuild(guildId: number, amount?: number, minLv?: number, maxLv?: number): boolean;
    checkClan(clanId: number, amount?: number, minLv?: number, maxLv?: number): boolean;
    info(name: string, infoType: number, mapIndex?: number): unknown;
    liveInfo(infoType: number, instanceId?: number): unknown;
    list(map: string, mode?: number): unknown[];
    getVar(name: string, instanceId: number): unknown;
    setVar(name: string, value: unknown, instanceId: number): Promise<void>;
}

export interface BattlegroundOps {
    create(map: string, x: number, y: number, onQuitEvent?: string, onDeathEvent?: string): Promise<number>;
    join(battleGroup: number, map?: string, x?: number, y?: number, charId?: number): Promise<number>;
    setTeamXY(battleGroup: number, x: number, y: number): Promise<void>;
    reserve(map: string, ended?: boolean): Promise<number>;
    unbook(map: string): Promise<void>;
    desert(charId?: number): Promise<void>;
    warp(battleGroup: number, map: string, x: number, y: number): Promise<void>;
    spawnMonster(battleGroup: number, map: string, x: number, y: number, displayName: string, mobId: number, eventLabel: string): Promise<number>;
    setMonsterTeam(gid: number, battleGroup: number): Promise<void>;
    leave(charId?: number): Promise<void>;
    destroy(battleGroup: number): Promise<void>;
    waitingRoomToBgSingle(battleGroup: number, map: string, x: number, y: number, npcName?: string): Promise<void>;
    waitingRoomToBg(map: string, x: number, y: number, onQuitEvent?: string, onDeathEvent?: string, npcName?: string): Promise<void>;
    getData(battleGroup: number, type: number): number;
    areaUsers(battleGroup: number, map: string, x0: number, y0: number, x1: number, y1: number): number;
    updateScore(map: string, guillaumeScore: number, croixScore: number): Promise<void>;
    info(bgName: string, type: number): unknown;
}

export interface ChannelOps {
    create(name: string, alias: string, password?: string, option?: number, delay?: number, color?: number, charId?: number): Promise<void>;
    join(name: string, charId?: number): Promise<void>;
    setOption(name: string, option: number, value: number): Promise<void>;
    getOption(name: string, option: number): number;
    setColor(name: string, color: number): Promise<void>;
    setPassword(name: string, password: string): Promise<void>;
    setGroups(name: string, groupIds: number[]): Promise<void>;
    chat(name: string, message: string, color?: number): Promise<void>;
    ban(name: string, charId: number): Promise<void>;
    unban(name: string, charId: number): Promise<void>;
    kick(name: string, charId: number): Promise<void>;
    delete(name: string): Promise<void>;
}
