using Map.Server.Combat;
using Map.Server.Elemental;
using Map.Server.Entities;
using Map.Server.Homunculus;
using Map.Server.Inventory;
using Map.Server.Movement;
using Map.Server.Movement.UnitOps;
using Map.Server.Party;
using Map.Server.Pathing;
using Map.Server.Pet.PetOps;
using Map.Server.Session;
using Map.Server.Shop.Buying;
using Map.Server.Spawn;
using Map.Server.Spawn.MobOps;
using Map.Server.Status;
using Map.Server.Status.StatusOps;
using Map.Server.World;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// Carrier for the shared services a <see cref="SkillImpl"/>
/// subclass needs. Passed by <see cref="SkillCastService.ResolveSkill"/>
/// so each plugin avoids the boilerplate of constructor-injecting
/// the same handful of services.
///
/// Mirrors the role of the global function pointers + map_session
/// access used inside rAthena's switch arms (caster's <c>sd</c>,
/// <c>battle_calc_attack</c>, <c>status_damage</c>, <c>status_change_*</c>,
/// <c>map_foreachinrange</c>, the three <c>clif_skill_*</c> helpers).
///
/// <para>P0 extension: every cross-cutting helper from
/// <c>PARITY-REMAINING.md §P0.1</c> is plumbed here so P1 per-skill
/// bodies can consume them without per-plugin DI. All P0.1-shaped
/// services are optional (null in unit tests that don't exercise the
/// pipe); the per-skill plugin null-checks before calling.</para>
/// </summary>
/// <param name="Entities">Spatial registry — lookup by id, splash iteration.</param>
/// <param name="Damage">HP-mutation pipeline (mirrors <c>status_damage</c>).</param>
/// <param name="Battle">Renewal damage formula entry point.</param>
/// <param name="Sc">Status-change service. Null in unit tests that
///     don't exercise SCs.</param>
/// <param name="Client">Skill-result broadcaster — wraps the three
///     <c>clif_skill_*</c> packets. Null in unit tests that don't
///     exercise the visibility layer.</param>
/// <param name="PartyMap">P0.1 — <c>party_foreachsamemap</c> enumerator
///     used by every party-broadcast skill (Angelus, Magnificat,
///     Suffragium, Impositio, Renovatio, …).</param>
/// <param name="PlayerSkill">P0.1 — <c>pc_checkskill</c> reader; lets
///     a skill body branch on the caster's other learned skills.</param>
/// <param name="Orbs">P0.1 — <c>pc_addspiritcharm</c> / sphere /
///     servant / abyss orb counters.</param>
/// <param name="Equip">P0.1 — <c>pc_checkequip</c> reader. Needed by
///     any skill that branches on equipped slot (Sage Endow gates,
///     Hunter ammo type, etc.).</param>
/// <param name="UnitOps">P0.1 — <c>unit_movepos</c> +
///     <c>skill_check_unit_movepos</c> + <c>unit_blown</c>. Skills
///     that teleport-near (Charge Attack, Shadow Leap) or knockback
///     route here.</param>
/// <param name="Setpos">P0.1 — <c>pc_setpos</c> warp helper.</param>
/// <param name="MobSpawn">P0.1 — <c>mob_once_spawn</c>; skills that
///     summon ad-hoc mobs (NPC_SUMMONSLAVE variants).</param>
/// <param name="MobOps">P0.1 — <c>mob_class_change</c> + the rest of
///     the mob lifecycle surface.</param>
/// <param name="SkillAttack">P0.1 — <c>skill_attack</c> /
///     <c>skill_area_sub</c>. Standard offensive skill dispatch.</param>
/// <param name="SideEffect">P0.1 — <c>skill_break_equip</c> /
///     <c>skill_autospell</c> / <c>skill_strip_equip</c>.</param>
/// <param name="Sessions">P0.1 — <c>PlayerEntity</c> →
///     <c>MapSessionData</c> bridge. Needed when a skill body must
///     read the caster's session-side inventory (e.g. for
///     <c>pc_checkequip</c> follow-ups).</param>
public sealed record SkillBehaviorContext(
    IEntityRegistry Entities,
    IDamageService Damage,
    IBattleCalculator Battle,
    IStatusChangeService? Sc,
    ISkillClientService? Client = null,
    IPartyMapService? PartyMap = null,
    /// <summary>Wave 87 — party-cache facade (party_search / party_isleader /
    /// party_skill_check / party_send_levelup). Consumed by leader-gated
    /// skills (AB_CONVENIO) and trigger-class-gated passives (TK_COUNTER,
    /// MO_COMBOFINISH, AM_TWILIGHT2/3).</summary>
    IPartyService? Party = null,
    IPlayerSkillService? PlayerSkill = null,
    Map.Server.Status.IPlayerOrbService? Orbs = null,
    IEquipService? Equip = null,
    IUnitOpsService? UnitOps = null,
    IPcSetposService? Setpos = null,
    IMobSpawnService? MobSpawn = null,
    IMobOpsService? MobOps = null,
    ISkillAttackService? SkillAttack = null,
    ISkillSideEffectService? SideEffect = null,
    IMapSessionRegistry? Sessions = null,
    /// <summary>Map-name → MapData registry. Lets plugins resolve the
    /// caster's current map name from its hashed <see cref="Entity.MapId"/>
    /// for Setpos / warp dispatch.</summary>
    IMapWorldRegistry? World = null,
    /// <summary>Map-flag service (no-teleport / no-skill / no-pvp / etc.).
    /// Plugins gate fail-early when a forbidden flag is set.</summary>
    IMapFlagService? MapFlags = null,
    /// <summary>Player option mask service (<c>pc_setoption</c>) — toggles
    /// Falcon / Warg / WargRider / Cart / Riding / Madogear flags and
    /// broadcasts <c>ZC_STATE_CHANGE3</c>. Wired so the Archer falcon /
    /// Ranger warg skills mutate the option mask through a single
    /// canonical path instead of inline <c>pc.Option</c> writes.</summary>
    IPlayerOptionService? Options = null,
    /// <summary>Path service — <c>path_search</c> (A* walkable),
    /// <c>path_search_long</c> (Bresenham LoS), distance / direction
    /// primitives. Skills that gate on line-of-sight or wall-clear
    /// (Charge Attack, Sight, Storm Gust spawn) consult this.</summary>
    IPathService? Paths = null,
    /// <summary>Steal service — <c>pc_steal_item</c> + <c>pc_steal_coin</c>.
    /// TF_STEAL / RG_STEALCOIN / GC_PHANTOMMENACE route here.</summary>
    IPlayerStealService? Steal = null,
    /// <summary>PC death + savepoint service. <c>pc_setpos(SavePoint)</c>
    /// and the dead-respawn flow live here; ALL_ODINS_RECALL lv 2/3,
    /// AL_TELEPORT lv 2, NV_BREAKTHROUGH dispatch through it.</summary>
    IPcDeathService? Death = null,
    /// <summary>Status-ops service — <c>status_heal</c>, <c>status_zap</c>,
    /// <c>status_percent_damage</c>, <c>status_revive</c>. Skills that
    /// need percent-based HP/SP mutation (KO_JYUSATSU, NPC_PERCENT_DRAIN)
    /// route here instead of inline Hp arithmetic.</summary>
    IStatusOpsService? StatusOps = null,
    /// <summary>Ground-unit service — <c>skill_unitsetting</c> +
    /// <c>map_foreachincell(BL_SKILL)</c> enumerator + lifecycle helpers
    /// (<c>skill_delunit</c>, <c>skill_delunitgroup_</c>). Skills that
    /// trip / dispel / chain off existing traps (SpringTrap, RemoveTrap,
    /// Detonator, Trample) and Land Protector overlap checks (SafetyWall)
    /// route here.</summary>
    ISkillUnitService? Units = null,
    /// <summary>Bound-elemental service — <c>elemental_create</c> /
    /// <c>elemental_delete</c> + master binding accessor. Sorcerer
    /// summons (SO_SUMMON_AGNI / AQUA / VENTUS / TERA) and the EM-tier
    /// promotions (Diluvio / Ardor / Procella / Terremotus / Serpens)
    /// dispatch through this.</summary>
    IElementalService? Elemental = null,
    /// <summary>Skill-cast service — used by cancel-cast plugins
    /// (SA_SPELLBREAKER, MindBreaker, NPC_CASTCANCEL) and any skill
    /// that recursively dispatches another skill. Self-reference to
    /// the running <see cref="ISkillCastService"/>.</summary>
    ISkillCastService? Cast = null,
    /// <summary>Buying-store service — <c>buyingstore_open</c> +
    /// trade / search / autotrade lifecycle. Used by
    /// ALL_BUYING_STORE / ALL_GUARDIAN_RECALL / vending plugins.</summary>
    IBuyingStoreService? BuyingStore = null,
    /// <summary>Pet operations service — <c>pet_catch_process_start</c> /
    /// <c>pet_catch_process_end</c>, autobonus + support bonus lifecycle.
    /// Used by SA_TAMINGMONSTER and pet-related script bonuses.</summary>
    IPetOpsService? PetOps = null,
    /// <summary>Homunculus service — <c>hom_call</c> / <c>hom_vaporize</c>
    /// / <c>hom_ressurect</c> + the wider lifecycle. Used by AM_CALLHOMUN,
    /// AM_RESURRECTHOMUN, AM_REST.</summary>
    IHomunculusService? Homunculus = null,
    /// <summary>Skill-requirement service — pre/post-cast condition
    /// gates plus the <c>SKILL_NOCONSUME_REQ</c> refund path. Plugins
    /// that self-dispel mid-resolve (PA_GOSPEL recast, MG_SAFETYWALL on
    /// Land Protector overlap) call <see cref="ISkillRequirementService.RefundRequirement"/>
    /// to credit the spent pools back.</summary>
    ISkillRequirementService? Requirements = null,
    /// <summary>Item-group catalog — weighted random pick + membership
    /// query. Used by GN_SLINGITEM (BOMB / THROWABLE groups), pet-feed,
    /// drop-from-group helpers.</summary>
    Map.Server.Items.IItemGroupService? ItemGroups = null,
    /// <summary>Item catalog (item_db snapshot) — id ↔ aegis lookup,
    /// per-item flag reads. Plugins that branch on the equipped /
    /// targeted item's catalog row (SlingItem ammo lookup, weapon-mask
    /// gates) consult this.</summary>
    Map.Server.Items.IItemCatalog? Catalog = null,
    /// <summary>Floor-item service — <c>TryPickup</c> for skill-driven
    /// auto-loot. The splash itself rides on
    /// <see cref="IEntityRegistry.ForEachInRange"/> filtered to
    /// <see cref="EntityType.Item"/>; this service handles the actual
    /// pickup mutation. Used by BS_GREED.</summary>
    Map.Server.Items.IItemDropService? Drops = null,
    /// <summary>Skill-production service — <c>skill_produce_mix</c> +
    /// <c>skill_arrow_create</c> + identify / repair / refine. Plugins
    /// that produce items (ASC_CDP, GC_CREATENEWPOISON, AM_PHARMACY,
    /// Twilight Alchemy 1/2/3) call <c>ProduceMix</c> here.</summary>
    ISkillProductionService? Production = null,
    /// <summary>Produce-recipe catalog (<c>produce_db.txt</c>) —
    /// look up recipes by id or by require-skill. Used by skills
    /// that need to enumerate the recipes a caster qualifies for
    /// (open the produce-mix picker UI).</summary>
    IProduceRecipeService? Recipes = null,
    /// <summary>Abracadabra random-skill pool (<c>abra_db</c>) —
    /// drives SA_ABRACADABRA's per-cast random spell pick. Backed by
    /// the SQL <c>abra_db</c> seed; falls back to a no-op when the
    /// table is empty.</summary>
    Map.Server.Skills.IAbraDatabase? Abra = null,
    /// <summary>Inventory service — rAthena <c>pc_additem</c> +
    /// <c>pc_delitem</c> wrapper. Plugins that refund a deploy item
    /// (HT_REMOVETRAP, RA_REMOVETRAP) or grant a script-produced item
    /// dispatch through this.</summary>
    Map.Server.Inventory.IInventoryService? Inventory = null);
