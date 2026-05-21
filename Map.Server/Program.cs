using System.Reflection;
using Core.Database;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Gm;
using Map.Server.Gm.Commands;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Scripting;
using Map.Server.Services;
using Map.Server.Session;
using Map.Server.Spawn;
using Map.Server.Visibility;
using Map.Server.Warps;
using Map.Server.World;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// Setup configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Setup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Setup gRPC and DI container
var builder = WebApplication.CreateBuilder(args);

// Create server configuration
var serverConfig = new MapServerConfiguration();
configuration.GetSection("Server").Bind(serverConfig);

// Configure services
builder.Services.AddSingleton<ServerConfiguration>(serverConfig);
builder.Services.AddSingleton(serverConfig);
builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<Program>>());

// Register decoupled services
builder.Services.AddSingleton<ServerConnectionService>();
builder.Services.AddSingleton<IServerConnectionService>(sp => sp.GetRequiredService<ServerConnectionService>());
builder.Services.AddSingleton<ICharServerIpcService, CharServerIpcService>();
// Each ICharServerIpcService* facet (Storage, Party, Guild, Mail, Auction,
// Quest, Pet, Homunculus, Mercenary, Elemental, Clan, Core, Inter) is
// implemented by the same singleton — wire each forwarder so per-facet
// consumers (StorageService, PartyService, etc.) can take a narrow
// dependency without re-injecting the umbrella interface.
builder.Services.AddSingleton<ICharServerIpcServiceCore>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceInter>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceParty>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceGuild>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceStorage>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceMail>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceAuction>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceQuest>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServicePet>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceHomunculus>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceMercenary>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceElemental>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<ICharServerIpcServiceClan>(sp => sp.GetRequiredService<ICharServerIpcService>());
builder.Services.AddSingleton<IPlayerMapService, PlayerMapService>();

// World data: load the configured maps once at startup and expose via IMapWorldRegistry.
builder.Services.AddSingleton<IMapWorldRegistry>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    return MapWorldRegistry.Load(serverConfig.MapDataPaths, serverConfig.Maps, logger);
});

// Static catalogs hydrated from Core.Database (rAthena use_sql_db: yes parity).
// IMobRepository / IItemRepository are Scoped; MobDb / IItemCatalog take
// IServiceScopeFactory and create scopes internally on load + Reload.
var dbConnectionString = configuration.GetConnectionString("GameDatabase")
    ?? throw new InvalidOperationException(
        "Missing connection string 'GameDatabase' in appsettings.json");
builder.Services.AddGameDatabase(dbConnectionString);
builder.Services.AddSingleton<IMobDb, MobDb>();
builder.Services.AddSingleton<IItemCatalog, ItemCatalog>();

// Register server state separately to avoid circular dependencies
builder.Services.AddSingleton<MapServerState>();
builder.Services.AddSingleton<IMapServerState>(sp => sp.GetRequiredService<MapServerState>());

// Register MapServerImpl
builder.Services.AddSingleton<MapServerImpl>();
builder.Services.AddSingleton<IServerReadiness>(sp => sp.GetRequiredService<MapServerImpl>());

// Entity infrastructure for MS1 gameplay (see .agents/migrations/map/entities.md).
builder.Services.AddSingleton<EntityIdAllocator>();
builder.Services.AddSingleton<IEntityRegistry, EntityRegistry>();

// Warps (see .agents/migrations/map/declarative-catalogs.md). At boot the
// service loads every warp row for hosted maps, marks NpcTrigger on each
// trigger-box cell (rAthena npc_setcells), and exposes O(1) lookup for the
// movement hot path. Must be registered before MovementService so the
// movement walk loop can resolve IWarpService.
builder.Services.AddSingleton<IWarpService, WarpService>();
builder.Services.AddSingleton<IWarpDispatcher, WarpDispatcher>();
// Mapflag gates. Reads INpcRegistry.AllMapFlags() lazily and caches a
// per-map bitmask. Consumers (DamageService, SkillCastService, the
// drop / throw paths) call IsSet(mapName, MapFlag.X) at the relevant
// gameplay gate to honor rAthena's mapflag rules.
builder.Services.AddSingleton<IMapFlagService, MapFlagService>();

// Movement (see .agents/migrations/map/movement.md). Walk steps are scheduled
// through Core.Timer's Scheduler; the service binds entities → walk timers.
builder.Services.AddSingleton<IMovementService, MovementService>();

// Visibility / AOI broadcast (see .agents/migrations/map/visibility.md).
builder.Services.AddSingleton<IPacketDispatcher, SessionPacketDispatcher>();
builder.Services.AddSingleton<IVisibilityService, VisibilityService>();

// Session lifecycle: detects dead TCP sessions, tears down the bound
// PlayerEntity, broadcasts vanish, and fires the char-server LeaveMap IPC.
// Polled once per map tick from MapServerImpl.UpdateGameLogicAsync.
builder.Services.AddSingleton<MapSessionLifecycle>();

// Mob spawn (see .agents/migrations/map/spawn.md). Registry is the
// collection of declared spawn entries; service drives initial spawn,
// idle wander, and respawn timing.
builder.Services.AddSingleton<IMobSpawnRegistry, MobSpawnRegistry>();
builder.Services.AddSingleton<MobSpawnService>();
builder.Services.AddSingleton<IMobSpawnService>(sp => sp.GetRequiredService<MobSpawnService>());
// Same instance, narrow seam — DamageService injects this to avoid the
// spawn → movement → warp → setpos → attack → damage → spawn DI cycle.
builder.Services.AddSingleton<IMobDeathSink>(sp => sp.GetRequiredService<MobSpawnService>());

// Scripting (see .agents/migrations/map/scripting/). At boot the host
// loads the esbuild bundle from scripts/dist/main.js; every register*()
// call in the bundle populates INpcRegistry. NpcSpawnService then places
// the scripted NPCs as entities. Phase 1 captures onClick/onTouch/...
// closures but does NOT invoke them — ContactNpcHandler logs and closes
// the dialog cleanly. Phase 2 wires the actual dispatcher.
var scriptOptions = new ScriptHostOptions();
configuration.GetSection("Scripting").Bind(scriptOptions);
builder.Services.AddSingleton(scriptOptions);
builder.Services.AddSingleton<INpcRegistry, NpcRegistry>();
builder.Services.AddSingleton<ScriptHost>();
builder.Services.AddSingleton<INpcSpawnService, NpcSpawnService>();
builder.Services.AddSingleton<Map.Server.Scripting.Dialog.IDialogDispatcher, Map.Server.Scripting.Dialog.DialogDispatcher>();

// Persistence: writes core character state (zeny / hp / sp / levels) and
// the three persistent var-reg scopes (perm / account / accountGlobal) to
// the DB. Map server writes directly via Core.Database; char-server IPC
// is no longer the route for player-state mutations.
builder.Services.AddSingleton<Map.Server.Persistence.IPlayerStateService, Map.Server.Persistence.PlayerStateService>();

// Inventory: loads each connecting character's inventory rows from the DB
// and emits the clif_inventorylist packet cascade so the client's bag UI
// populates. Slice 1 of the inventory/items work — see
// .agents/migrations/map/scripting/ for the broader plan.
builder.Services.AddSingleton<Map.Server.Inventory.IInventoryService, Map.Server.Inventory.InventoryService>();

// Item use (pc.cpp:6329 pc_useitem). Strategy-pattern dispatch via
// ItemEffectRegistry — one handler class per item (HealHp / HealSp /
// ApplyStatus / etc.). New items register without touching the
// service. Full item_db Script parser lands later.
builder.Services.AddSingleton<Map.Server.Inventory.ItemEffects.ItemEffectRegistry>();
builder.Services.AddSingleton<Map.Server.Inventory.IItemUseService, Map.Server.Inventory.ItemUseService>();

// Chat router (party_send_message / guild_send_message / clif_wis_message).
// Local fan-out + char-server IPC handoff for cross-map delivery.
builder.Services.AddSingleton<Map.Server.Chat.IChatIpcOutbound, Map.Server.Chat.ChatIpcOutbound>();
builder.Services.AddSingleton<Map.Server.Chat.IChatService, Map.Server.Chat.ChatService>();

// Equip / unequip (pc.cpp pc_equipitem / pc_unequipitem). Mediates
// the wear-state bits on inventory rows and triggers a status recalc
// through EquipBonusAggregator + IStatusCalcService.
builder.Services.AddSingleton<Map.Server.Inventory.IEquipService, Map.Server.Inventory.EquipService>();

// Account storage (storage.cpp). Mediates inventory ↔ storage
// transfers; load/save via the existing P5 AccountStorageLoad/Save IPC.
builder.Services.AddSingleton<Map.Server.Storage.IStorageService, Map.Server.Storage.StorageService>();

// Player trade (trade.cpp). State machine over a TradeState per side;
// atomic commit when both sides reach LockedStage == 2.
builder.Services.AddSingleton<Map.Server.Trade.ITradeService, Map.Server.Trade.TradeService>();

// NPC shop (npc.cpp npc_buylist / npc_selllist). Buys validate against
// the script-registered shop catalog + buyer's zeny; sells use the
// rAthena 50% sell ratio.
builder.Services.AddSingleton<Map.Server.Shop.IShopService, Map.Server.Shop.ShopService>();

// Status broadcast cascade (post-handoff). See
// .agents/migrations/map/initial-status-broadcast.md. The broadcaster
// emits the rAthena pc_authok / status_calc_pc(SCO_FIRST) packet stream
// matching the captured wire order.
builder.Services.AddSingleton<Map.Server.Status.StatusBroadcaster>();

// Renewal stat recalc (status.cpp:status_calc_pc / status_calc_mob).
// Owns BattleStats hydration for both players (at session enter / equip
// change / SC apply) and mobs (at spawn). Consumed by combat, skill, AI.
builder.Services.AddSingleton<Map.Server.Status.IStatusCalcService, Map.Server.Status.StatusCalcService>();

// Floor-item drop / pickup (see .agents/migrations/map/adjacent/items.md).
// MS3 first slice: the entity-on-the-floor lifecycle (drop, pickup, TTL
// despawn). Inventory persistence + item_db catalog land later.
builder.Services.AddSingleton<IItemDropService, ItemDropService>();

// Combat (see .agents/migrations/map/adjacent/combat.md). BattleCalculator
// owns the renewal damage formula (battle.cpp:7635 battle_calc_weapon_attack
// trimmed first slice); DamageService is the calc-then-apply façade that
// the auto-attack loop, GM commands, and skill handlers funnel through.
builder.Services.AddSingleton<Map.Server.Combat.IBattleCardService, Map.Server.Combat.BattleCardService>();
builder.Services.AddSingleton<Map.Server.Combat.IBattleReflectService, Map.Server.Combat.BattleReflectService>();
builder.Services.AddSingleton<Map.Server.Combat.IZoneDamageService, Map.Server.Combat.ZoneDamageService>();
builder.Services.AddSingleton<Map.Server.Combat.IBattleTargetService, Map.Server.Combat.BattleTargetService>();
builder.Services.AddSingleton<Map.Server.Combat.IDelayedDamageService, Map.Server.Combat.DelayedDamageService>();
builder.Services.AddSingleton<Map.Server.Combat.IBattleEffectsService, Map.Server.Combat.BattleEffectsService>();
builder.Services.AddSingleton<Map.Server.Combat.IBattleElementService, Map.Server.Combat.BattleElementService>();
builder.Services.AddSingleton<Map.Server.Combat.IBattleConfigService, Map.Server.Combat.BattleConfigService>();
builder.Services.AddSingleton<Map.Server.Combat.IBattleZoneGateService, Map.Server.Combat.BattleZoneGateService>();
builder.Services.AddSingleton<IBattleCalculator>(sp =>
    new BattleCalculator(rng: null, cards: sp.GetRequiredService<Map.Server.Combat.IBattleCardService>()));
builder.Services.AddSingleton<IDamageService, DamageService>();

// PC death + respawn (pc.cpp:9633 pc_dead + pc.cpp:9515 pc_respawn).
// Wires the corpse-state lifecycle so DamageService.HandleDeath stops
// stripping the player from the registry — they linger as a dead body
// until the client sends CZ_RESTART(type=0) → IPcDeathService.Respawn.
builder.Services.AddSingleton<IPcDeathService, PcDeathService>();

// pc_setpos (pc.cpp:6949) — canonical PC teleport. Used by warps,
// savepoint respawn, GM @warp, item-warp scrolls. Same-map jumps stay
// in-process; cross-server map handoff lands later.
builder.Services.AddSingleton<IPcSetposService, PcSetposService>();

// Party EXP share (party.cpp:1238 party_exp_share). On mob kill,
// DamageService checks the killer's PartyId and routes through this
// service when set; falls back to single-player pc_gainexp otherwise.
builder.Services.AddSingleton<Map.Server.Party.IPartyShareService, Map.Server.Party.PartyShareService>();
// Auto-attack loop (rAthena unit.cpp:2615 unit_attack +
// unit.cpp:3056 unit_attack_timer). Driven from the map game loop;
// validates range/death/map every tick and chases via IMovementService.
builder.Services.AddSingleton<AttackService>();
builder.Services.AddSingleton<IAttackService>(sp => sp.GetRequiredService<AttackService>());
// Narrow seam — see IAttackStopper. Setpos / death paths inject this to
// avoid the attack → damage → setpos/death → attack DI cycle. The
// PcSetposService resolves this lazily through IServiceProvider at
// first call site (see comment there) so the construction-time chain
// doesn't recurse back through itself.
builder.Services.AddSingleton<IAttackStopper>(sp => sp.GetRequiredService<AttackService>());

// CZ_REQUEST_ACTION dispatch — one IActionHandler strategy per
// action code (single-attack, continuous-attack, sit, stand…).
// Same shape as SkillResolverRegistry.
builder.Services.AddSingleton<Map.Server.Handlers.Actions.IActionHandler, Map.Server.Handlers.Actions.SingleAttackAction>();
builder.Services.AddSingleton<Map.Server.Handlers.Actions.IActionHandler, Map.Server.Handlers.Actions.ContinuousAttackAction>();
builder.Services.AddSingleton<Map.Server.Handlers.Actions.IActionHandler, Map.Server.Handlers.Actions.SitAction>();
builder.Services.AddSingleton<Map.Server.Handlers.Actions.IActionHandler, Map.Server.Handlers.Actions.StandAction>();
builder.Services.AddSingleton<Map.Server.Handlers.Actions.IActionHandler, Map.Server.Handlers.Actions.PickupAction>();
builder.Services.AddSingleton<Map.Server.Handlers.Actions.ActionRegistry>();

// Mob hard AI (mob.cpp:1741 mob_ai_sub_hard). Aggressive target
// acquisition; chase + swings delegate to IAttackService. Mob skill
// conditions (mobskill_use trigger evaluation) use IMobSkillConditionEvaluator
// strategies — one class per rAthena MSC_* condition.
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.AlwaysCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.MyHpLessThanRateCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.MyHpInRateCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.RudeAttackedCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.CloseAttackedCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.LongRangeAttackedCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.GroundAttackedCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.SkillUsedCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.CastTargetedCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.DamagedGreaterCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.AttackerCountGreaterCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.AttackerCountGreaterEqCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.SpawnCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.SlaveLessThanCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.IMobSkillConditionEvaluator, Map.Server.Mob.Conditions.SlaveLessEqCondition>();
builder.Services.AddSingleton<Map.Server.Mob.Conditions.MobSkillConditionRegistry>();
builder.Services.AddSingleton<Map.Server.Mob.IMobAiService, Map.Server.Mob.MobAiService>();

// Summon AI (mob.cpp:2030 mob_ai_sub_lazy — slave / pet / homun / merc /
// elemental follow + assist on master). Driven from the map tick same
// as IMobAiService.
builder.Services.AddSingleton<Map.Server.Mob.ISummonAiService, Map.Server.Mob.SummonAiService>();

// Pet system (pet.cpp). Hunger / intimacy ticks; egg-hatch + capture.
builder.Services.AddSingleton<Map.Server.Pet.IPetService, Map.Server.Pet.PetService>();

// EXP service (pc.cpp:8314 pc_gainexp). Awards base + job EXP on mob
// kill, walks the level-up chain, full-heals + broadcasts SP_BASELEVEL
// etc. on each level up. Needs a session accessor to push the ZC_PAR
// packets back to the right client.
builder.Services.AddSingleton<Map.Server.Status.ISessionManagerAccessor, Map.Server.Session.MapSessionAccessor>();
builder.Services.AddSingleton<Map.Server.Status.IExpService, Map.Server.Status.ExpService>();

// Status change engine (status.cpp:9851 status_change_start +
// status.cpp:13732 status_change_timer). Per-SC handlers live in the
// effect registry — new SCs add a Register() call without touching
// the engine itself.
builder.Services.AddSingleton<Map.Server.Status.StatusEffectRegistry>();
builder.Services.AddSingleton<Map.Server.Status.IStatusChangeService, Map.Server.Status.StatusChangeService>();

// Skill system (skill.cpp:skill_use_id + skill_castend_*). First slice:
// hand-built starter catalog (Bash / Heal / Increase AGI / Blessing /
// Fire Bolt / Cold Bolt). DB-backed skill_db loader lands later.
builder.Services.AddSingleton<Map.Server.Skills.ISkillDb, Map.Server.Skills.SkillDb>();
// Cast / delay / vfcast fix (skill.cpp:20193-20565). Wraps the
// DEX/AGI cast-time scaling + battle_config rates so the rest of the
// port reads a single canonical entry point.
builder.Services.AddSingleton<Map.Server.Skills.ISkillCastTimingService, Map.Server.Skills.SkillCastTimingService>();
// Skill cast pre/post-check + resource consume (skill.cpp:18347, 19417,
// 4397, 19685). HP/SP/AP path real; item / ammo / weapon-mask paths
// data-pending on the equip aggregator + skill_db require column.
builder.Services.AddSingleton<Map.Server.Skills.ISkillRequirementService, Map.Server.Skills.SkillRequirementService>();
// Castend dispatchers (skill.cpp castend_damage_id / nodamage_id /
// pos2 / map). Wraps the existing resolver registry + ground-unit
// service in the canonical rAthena entry-point names.
builder.Services.AddSingleton<Map.Server.Skills.ISkillCastEndService, Map.Server.Skills.SkillCastEndService>();
// Central skill-attack helper (skill.cpp:3561 + 20992 + 4188). The
// funnel offensive skills flow through to actually deal damage.
builder.Services.AddSingleton<Map.Server.Skills.ISkillAttackService, Map.Server.Skills.SkillAttackService>();
// Per-skill block + timer-skill helper (skill.cpp skill_blockpc_*,
// skill_addtimerskill / cleartimerskill).
builder.Services.AddSingleton<Map.Server.Skills.ISkillBlockService, Map.Server.Skills.SkillBlockService>();
// Map-flag + per-skill gating (skill.cpp skill_isNotOk family +
// skill_pos_maxcount_check).
builder.Services.AddSingleton<Map.Server.Skills.ISkillGateService, Map.Server.Skills.SkillGateService>();
// Post-hit additional effect + counter effect + OnUseSkill bonus
// hook (skill.cpp skill_additional_effect / counter_additional_effect).
builder.Services.AddSingleton<Map.Server.Skills.ISkillEffectService, Map.Server.Skills.SkillEffectService>();
// Heal calc + AutoSpell + BreakEquip + StripEquip side effects.
builder.Services.AddSingleton<Map.Server.Skills.ISkillSideEffectService, Map.Server.Skills.SkillSideEffectService>();
// Production / arrow / refine / identify (skill.cpp:20571+).
builder.Services.AddSingleton<Map.Server.Skills.ISkillProductionService, Map.Server.Skills.SkillProductionService>();
// Combo / partner / banding helpers.
builder.Services.AddSingleton<Map.Server.Skills.ISkillComboService, Map.Server.Skills.SkillComboService>();
// One-off misc skill commands (Greed, Frost Joke, Magic Decoy, …).
builder.Services.AddSingleton<Map.Server.Skills.ISkillMiscService, Map.Server.Skills.SkillMiscService>();
// usave + layout init.
builder.Services.AddSingleton<Map.Server.Skills.ISkillUsaveService, Map.Server.Skills.SkillUsaveService>();
builder.Services.AddSingleton<Map.Server.Skills.ISkillLayoutService, Map.Server.Skills.SkillLayoutService>();
// Auxiliary YAML-backed databases (Abra / Magic Mushroom / Spell Book / Arrow).
builder.Services.AddSingleton<Map.Server.Skills.IAbraDatabase, Map.Server.Skills.AbraDatabase>();
builder.Services.AddSingleton<Map.Server.Skills.IMagicMushroomDatabase, Map.Server.Skills.MagicMushroomDatabase>();
builder.Services.AddSingleton<Map.Server.Skills.IReadingSpellbookDatabase, Map.Server.Skills.ReadingSpellbookDatabase>();
builder.Services.AddSingleton<Map.Server.Skills.ISkillArrowDatabase, Map.Server.Skills.SkillArrowDatabase>();

// Small-file services (date / duel / clan / mapreg / searchstore /
// pc_groups / npc_chat) — canonical entry points for the rAthena
// functions, with audit docs in .agents/migrations/map/.
builder.Services.AddSingleton<Map.Server.Time.IDateService, Map.Server.Time.DateService>();
builder.Services.AddSingleton<Map.Server.Duel.IDuelService, Map.Server.Duel.DuelService>();
builder.Services.AddSingleton<Map.Server.Clan.IClanService, Map.Server.Clan.ClanService>();
builder.Services.AddSingleton<Map.Server.Scripting.MapReg.IMapRegService, Map.Server.Scripting.MapReg.MapRegService>();
builder.Services.AddSingleton<Map.Server.Shop.SearchStore.ISearchStoreService, Map.Server.Shop.SearchStore.SearchStoreService>();
builder.Services.AddSingleton<Map.Server.Gm.Groups.IPlayerGroupsService, Map.Server.Gm.Groups.PlayerGroupsService>();
builder.Services.AddSingleton<Map.Server.Scripting.NpcChat.INpcChatService, Map.Server.Scripting.NpcChat.NpcChatService>();
builder.Services.AddSingleton<Map.Server.Chat.Rooms.IChatRoomService, Map.Server.Chat.Rooms.ChatRoomService>();
builder.Services.AddSingleton<Map.Server.Pathing.IPathService, Map.Server.Pathing.PathService>();
builder.Services.AddSingleton<Map.Server.Mail.IMailService, Map.Server.Mail.MailService>();
builder.Services.AddSingleton<Map.Server.Shop.Cash.ICashShopService, Map.Server.Shop.Cash.CashShopService>();
builder.Services.AddSingleton<Map.Server.Trade.Wire.ITradeWireService, Map.Server.Trade.Wire.TradeWireService>();
builder.Services.AddSingleton<Map.Server.Navi.INaviService, Map.Server.Navi.NaviService>();
builder.Services.AddSingleton<Map.Server.Logging.IGameLogService, Map.Server.Logging.GameLogService>();

// Mid-tier files (vending / buyingstore / mercenary / quest /
// elemental / guild storage / achievement / instance / channel /
// party booking / homunculus / battleground).
builder.Services.AddSingleton<Map.Server.Shop.Vending.IVendingService, Map.Server.Shop.Vending.VendingService>();
builder.Services.AddSingleton<Map.Server.Shop.Buying.IBuyingStoreService, Map.Server.Shop.Buying.BuyingStoreService>();
builder.Services.AddSingleton<Map.Server.Mercenary.IMercenaryService, Map.Server.Mercenary.MercenaryService>();
builder.Services.AddSingleton<Map.Server.Quest.IQuestService, Map.Server.Quest.QuestService>();
builder.Services.AddSingleton<Map.Server.Elemental.IElementalService, Map.Server.Elemental.ElementalService>();
builder.Services.AddSingleton<Map.Server.Storage.Guild.IGuildStorageService, Map.Server.Storage.Guild.GuildStorageService>();
builder.Services.AddSingleton<Map.Server.Achievement.IAchievementService, Map.Server.Achievement.AchievementService>();
builder.Services.AddSingleton<Map.Server.Instance.IInstanceService, Map.Server.Instance.InstanceService>();
builder.Services.AddSingleton<Map.Server.Chat.Channels.IChannelService, Map.Server.Chat.Channels.ChannelService>();
builder.Services.AddSingleton<Map.Server.Party.Booking.IPartyBookingService, Map.Server.Party.Booking.PartyBookingService>();
builder.Services.AddSingleton<Map.Server.Homunculus.IHomunculusService, Map.Server.Homunculus.HomunculusService>();
builder.Services.AddSingleton<Map.Server.BattleGround.IBattlegroundService, Map.Server.BattleGround.BattlegroundService>();

// Big-file services (status / clif / script / mob / npc / unit /
// map / guild / pet / itemdb / chrif / intif). Canonical rAthena-
// name entry points; most ops forward to dedicated services or
// document data-pending.
builder.Services.AddSingleton<Map.Server.Items.Db.IItemDbService, Map.Server.Items.Db.ItemDbService>();
builder.Services.AddSingleton<Map.Server.Status.StatusOps.IStatusOpsService, Map.Server.Status.StatusOps.StatusOpsService>();
builder.Services.AddSingleton<Map.Server.Handlers.ClifWire.IClifWireService, Map.Server.Handlers.ClifWire.ClifWireService>();
builder.Services.AddSingleton<Map.Server.Scripting.ScriptApi.IScriptApiService, Map.Server.Scripting.ScriptApi.ScriptApiService>();
builder.Services.AddSingleton<Map.Server.Spawn.MobOps.IMobOpsService, Map.Server.Spawn.MobOps.MobOpsService>();
builder.Services.AddSingleton<Map.Server.Spawn.NpcOps.INpcOpsService, Map.Server.Spawn.NpcOps.NpcOpsService>();
builder.Services.AddSingleton<Map.Server.Movement.UnitOps.IUnitOpsService, Map.Server.Movement.UnitOps.UnitOpsService>();
builder.Services.AddSingleton<Map.Server.World.MapOps.IMapOpsService, Map.Server.World.MapOps.MapOpsService>();
builder.Services.AddSingleton<Map.Server.Guild.IGuildService, Map.Server.Guild.GuildService>();
builder.Services.AddSingleton<Map.Server.Pet.PetOps.IPetOpsService, Map.Server.Pet.PetOps.PetOpsService>();
builder.Services.AddSingleton<Map.Server.Services.Chrif.IChrifService, Map.Server.Services.Chrif.ChrifService>();
builder.Services.AddSingleton<Map.Server.Services.Intif.IIntifService, Map.Server.Services.Intif.IntifService>();

// T2.3 — per-skill behavior plugins (SkillImpl hierarchy mirroring
// rathena-fork). Each plugin is a class in its job-class subdirectory
// (Skills/Behaviors/Swordman/, Mage/, …), deriving from SkillImpl
// or one of its specialized subclasses (WeaponSkillImpl /
// StatusSkillImpl / RecursiveDamageSplashSkillImpl). Adding a skill
// = one new file + one AddSingleton line.
//
// (C# `using` aliases must be file-scoped, so the long base-type name
// repeats per line below. A code-generation pass could collapse it
// later; the explicit form keeps grep-by-skill-name trivial.)
// Swordman
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Bash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Provoke>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.MagnumBreak>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Endure>();
// Knight
// Mage
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FireBolt>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ColdBolt>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.LightningBolt>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SoulStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FrostDiver>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.StoneCurse>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.NapalmBeat>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Fireball>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Thunderstorm>();
// Acolyte
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.HolyLight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.SignumCrucis>();
// Priest
// Wizard
// Merchant / Blacksmith
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.Mammonite>();
// Archer / Hunter
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.DoubleStrafe>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ArrowShower>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.OwlsEye>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ImproveConcentration>();
// Thief / Assassin
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Hiding>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Poison>();
// Monk
// Bard / Dancer
// T2.3 transcend classes (LK / HP / HW / PA / CH / ASC / SN / WS)
// Lord Knight
// High Priest / High Wizard / Paladin
// Champion / Assassin Cross
// Sniper / Whitesmith
// T2.3 3rd class (Renewal)
// T2.3 4th class (Renewal+)

// T2.3 bulk: auto-generated DI registrations for ~1,187 skill stubs
// from rathena-fork. Each stub lives under Behaviors/<Class>/<Skill>.cs
// and derives from SkillImpl. The bodies are TODOs pending per-skill
// implementation — registering them all here ensures the registry
// indexes them by id, but the stub's CastendDamageId / NoDamageId
// no-ops until the body is filled in.
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.AbsorbSpiritSphere>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Adoramus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Ancilla>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Angelus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Arbitrium>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Aspersio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.AssimilatePower>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Assumptio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.AsuraStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Basilica>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.BenedictioSanctissimiSacramenti>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Blessing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.CantoCandidus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.ChainCrushCombo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Clearance>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.ColuceoHeal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Competentia>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Convenio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Crementia>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Crucis>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Cure>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.CursedCircle>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.DecreaseAgi>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.DilectioHeal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.DragonCombo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.DupleLightMagic>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.EarthShaker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Effligo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Epiclesis>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.ExplosionBlaster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.FallenEmpire>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.FirstBrand>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.FlashCombo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Framen>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.GateOfHell>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.GentleTouchCure>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.GentleTouchQuiet>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.GlacierFist>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Gloria>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Heal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.HighnessHeal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.HolyWater>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.HowlingOfLion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.ImpositioManus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.IncreaseAgi>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Judex>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.KiExplosion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.KiTranslation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.KnuckleArrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.KyrieEleison>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.LaudaAgnus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.LaudaRamus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.LexDivina>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Magnificat>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.MagnusExorcismus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.MassiveFlameBlaster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.MedialeVotum>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.OccultImpaction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.OleumSanctum>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Oratio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Petitio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Pneuma>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.PneumaticusProcella>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.PowerVelocity>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Praefatio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.RagingPalmStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.RagingQuadrupleBlow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.RagingThrust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.RagingTrifectaBlow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.RaisingDragon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.RampageBlaster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Redemptio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Renovatio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Reparatio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Resurrection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.RideInLightening>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Ruwach>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Sanctuary>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.SecondFaith>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.SecondFlame>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.SecondJudgement>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Silentium>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.SkyNetBlow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Snap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.StatusRecovery>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Suffragium>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.SummoningSpiritSphere>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Teleport>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.ThirdConsecration>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.ThirdFlameBomb>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.ThirdPunish>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.ThrowSpiritSphere>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.TigerCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.TurnUndead>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Vituperatum>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.WarpPortal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Windmill>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Acolyte.Zen>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.AcousticRhythm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.AimedBolt>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.AinRhapsody>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Amp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.AnkleSnare>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ArrowStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.BattleTheme>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.BeastStrafing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.BlastMine>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.BlitzBeat>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Camouflage>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ChargeArrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.CircleOfNaturesSound>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ClassicalPluck>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ClaymoreTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ClusterBomb>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.CobaltTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Concentration>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.CresciveBolt>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.DanceWithAWarg>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Dazzler>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.DeepBlindTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.DeepSleepLullaby>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Detect>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Detonator>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.DominionImpulse>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.DownTempo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.EchoSong>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ElectricShocker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Encore>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.FalconAssault>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.FearBreeze>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.FiringTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.FlameTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Flasher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.FocusBallet>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.FocusedArrowStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.FreezingTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.FriggsSong>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.GaleStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.GeffeniaNocturn>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.GloomyDay>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.GreatEcho>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.GypsysKiss>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.HarmonicLick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Harmonize>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.HawkBoomerang>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.HawkMastery>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.HawkRush>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.HipShaker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.IceboundTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ImpressiveRiff>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ImprovisedSong>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.JawaiiSerenade>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.LadyLuck>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.LandMine>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.LeradsDew>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Lullaby>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MagentaTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MagicStrings>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MaizeTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MakingArrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MarionetteControl>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MelodyOfSink>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MelodyStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MentalSensing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MetallicFury>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MetallicSound>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MoonlitSerenade>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.MusicalInterlude>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.NipelheimRequiem>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.PangVoice>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.PerfectTablature>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.PhantasmicArrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.PoemOfTheNetherworld>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.PowerChord>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.PronMarch>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.RemoveTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Retrospection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Reverberation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.RhythmShooting>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.RokiCapriccio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.RoseBlossom>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.Sandman>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SaturdayNightFever>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SensitiveKeen>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SevereRainstorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ShelteringBliss>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ShockwaveTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SkidTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SkilledSpecialSinger>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SlingingArrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SlowGrace>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SolidTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SongOfMana>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SongofLutie>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SoundBlend>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SoundOfDestruction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SpringTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SwiftTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SwingDance>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.SymphonyOfLovers>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.TalkieBox>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.TarotCardOfFate>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.UnbarringOctave>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.UnchainedSerenade>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.UnlimitedHummingVoice>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.ValleyOfDeath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.VerdureTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.VoiceOfSiren>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.VulcanArrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WandOfHermode>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WarcryOfBeyond>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WargBite>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WargDash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WargMastery>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WargRider>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WargStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WildWalk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WindWalker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WindmillRushAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Archer.WinkofCharm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.AgeOfIce>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.AquaPlay>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Avalanche>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Blast>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.CircleOfFire>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.ColdForce>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.CoolAir>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Cooler>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.CrystalArmor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.CursedSoil>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.DeadlyPoison>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.DeepPoisoning>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.EarthCare>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.EyesOfStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.FireArrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.FireBomb>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.FireCloak>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.FireMantle>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.FireWave>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.FlameArmor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.FlameRock>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.FlameTechnic>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.GraceBreeze>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Gust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Heater>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.HurricaneRage>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.IceNeedle>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Petrology>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.PoisonShield>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.PowerOfGaia>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Pyrotechnic>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.RockLauncher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.SolidSkin>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.StoneHammer>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.StoneRain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.StoneShield>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.StormWind>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.StrongProtection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.TidalWeapon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Tropic>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.TyphoonMissile>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Upheaval>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.WaterBarrier>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.WaterDrop>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.WaterScreen>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.WaterScrew>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.WildStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.WindCurtain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.WindSlasher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.WindStep>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.ElementalNpc.Zephyr>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.AntiMaterialBlast>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.BanishingBuster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.BasicGrenade>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.BindTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Bullseye>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.ChainAction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Cracker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.CrimsonMarker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Desperado>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Disarm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.DragonTail>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Dust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.FallenAngel>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.FireDance>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.FireRain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Flicker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Fling>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.FullBuster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Gatlingfever>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Glittering>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.GrenadeFragment>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.GrenadesDropping>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.GroundDrift>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.HammerOfGod>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.HastyFireInTheHole>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.HowlingMine>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.IntensiveAim>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.MagazineForOne>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.MassSpiral>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.MissionBombard>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.OnlyOneBullet>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.PiercingShot>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.QuickDrawShot>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.RapidShower>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.RichsCoin>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.RoundTrip>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.ShatterStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.SlugShot>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.SpiralShooting>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.SpreadAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.TheVigilanteAtNight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.Tracking>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.TripleAction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Gunslinger.WildFire>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.AbsoluteZephyr>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.Avoid>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.BenedictionOfChaos>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.BioExplosion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.BlastForge>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.BlazingAndFurious>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.Caprice>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.Castling>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.Change>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.ContinualBreakCombo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.Defense>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.EraserCutter>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.EternalQuickCombo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.GlanzenSpies>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.GoldeneTone>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.GraniticArmor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.HealingTouch>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.HeiligePferd>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.HolyPole>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.LavaSlide>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.LightOfRegene>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.MagmaFlow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.MidnightFrenzy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.Moonlight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.NeedleOfParalyze>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.NeedleStinger>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.OveredBoost>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.PainKiller>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.PoisonMist>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.Pyroclastic>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.SBR44>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.SilentBreeze>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.SilverVeinRush>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.SonicClaw>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.SteelHorn>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.StoneWall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.StyleChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.SummonLegion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.Tempering>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.TheOneFighterRises>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.TinderBreaker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.ToxinOfMandara>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.TwisterCutter>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.VolcanicAsh>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Homunculus.XenoSlasher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ActivityBurn>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.AllBloom>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Arrullo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.AstralStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.BeastlyHypnosis>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.BlindingMist>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.CastCancel>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ChainLightning>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ClassChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.CloudKill>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Coma>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Comet>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Conflagration>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.CreateElementalConverter>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.CrimsonArrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.CrimsonRock>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.CrystalImpact>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.DeadlyProjection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Deluge>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.DestructiveHurricane>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.DiamondDust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.DiamondStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Dispell>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.DrainLife>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EarthGrave>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EarthInsignia>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EarthSpike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EarthStrain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElectricWalk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElementalAction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElementalBuster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElementalChangeEarth>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElementalChangeFire>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElementalChangeWater>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElementalChangeWind>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElementalShield>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ElementalVeil>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EndowBlaze>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EndowQuake>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EndowTornado>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EndowTsunami>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.EnergyCoat>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FiberLock>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FireInsignia>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FirePillar>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FireWalk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FireWall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FloralFlareRoad>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FourSpiritAnalysis>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FrostNova>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FrostyMisty>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.FrozenSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Ganbantein>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.GoldDigger>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.GravitationField>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Gravity>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.GrimReaper>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.HeavensDrive>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.HellInferno>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Hindsight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.HocusPocus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.IceWall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.IncreasingActivity>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Indulge>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.JackFrost>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.JupitelThunder>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Leveling>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.LightningLand>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.LordOfVermilion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.MagicRod>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.MagneticEarth>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.MeteorStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.MindBreaker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Monocell>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.MonsterChant>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.MysteryIllusion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.NapalmVulcan>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.PoisonBuster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.PsychicWave>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Quagmire>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Questioning>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.RainOfCrystal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ReadingSpellbook>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Rejuvenation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Release>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.RockDown>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SafetyWall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Sense>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SiennaExecrate>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Sight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SightBlaster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SightRasher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SoulExhale>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SoulExpansion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SoulSiphon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SoulVulcanStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SpellBreaker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SpellFist>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SpiritControl>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SpiritRecovery>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Stasis>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.StormCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.StormGust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.StrantumTremor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Striking>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Suicide>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonEarthSpiritTera>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonElementalArdor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonElementalDiluvio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonElementalProcella>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonElementalSerpens>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonElementalTerremotus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonFireBall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonFireSpiritAgni>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonLightningBall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonStone>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonWaterBall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonWaterSpiritAqua>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.SummonWindSpiritVentus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.TerraDrive>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.TetraVortex>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.TornadoStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.VacuumExtreme>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.VaretyrSpear>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.VenomSwamp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.ViolentQuake>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Volcano>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Warmer>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.WaterBall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.WaterInsignia>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.Whirlwind>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.WhiteImprison>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Mage.WindInsignia>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryArrowRepel>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryArrowShower>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryBash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryBenediction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryBlessing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryBowlingBash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryBrandishSpear>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryCompress>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryCrash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryDecreaseAgi>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryDoubleStrafe>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryFocusedArrowStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryFreezingTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryIncreaseAgility>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryKyrieEleison>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryLandMine>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryLexDivina>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryMagnificat>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryMagnumBreak>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryMentalCure>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryMindBlaster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryPierce>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryProvoke>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryRecuperate>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryRegain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryRemoveTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenarySacrifice>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenarySandman>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryScapegoat>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenarySense>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryShieldReflect>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenarySight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenarySkidTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenarySpiralPierce>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.MercenaryNpc.MercenaryTender>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AbrBattleWarrior>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AbrDualCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AbrInfinity>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AbrMotherNet>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AcidDemonstration>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AcidTerror>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AcidifiedZoneFire>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AcidifiedZoneGround>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AcidifiedZoneWater>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AcidifiedZoneWind>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AdrenalineRush>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AdvanceProtection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AdvancedAdrenalineRush>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AidBerserkPotion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AidCondensedPotion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AidPotion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AlchemicalWeapon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.Analyze>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.ArmCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AttackMachine>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AxeBoomerang>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AxeStomp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.AxeTornado>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.BackSideSlide>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.BiochemicalHelm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.BionicPharmacy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.Bomb>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.BoostKnuckle>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.CallHomunculus>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.CartCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.CartRevolution>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.CartTermination>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.CartTornado>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.ChangeCart>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.ChangeMaterial>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.ColdSlower>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.CrazyUproar>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.CrazyWeed>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.CreateBomb>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.Creeper>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.DecorateCart>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.DemonicFire>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.DustExplosion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.EmergencyCool>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.EnergyCannonade>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.ExplosivePowder>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.FawMagicDecoy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.FawRemoval>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.FawSilverSniper>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.FireExpansion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.FlameLauncher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.FrontSideSlide>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.FullProtection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.Greed>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.HammerFall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.HellTree>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.HellsPlant>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.HomunculusResurrection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.HowlingOfMandragora>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.IllusionDoping>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.InfraredScan>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.ItemAppraisal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.MagmaEruption>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.MagneticField>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.ManufactureMachine>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.MayhemicThorns>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.MightySmash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.MixCooking>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.MysteryPowder>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.NeutralBarrier>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.PileBunker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.PlantCultivation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.PowerSwing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.PowerThrust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.PowerfulSwing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.PreparePotion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.Repair>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.RushQuake>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.RushStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SelfDestruction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SlingItem>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SparkBlaster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SpecialPharmacy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SporeExplosion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.StealthField>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SummonFlora>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SummonMarineSphere>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SynthesizedShield>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.SyntheticArmor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.TheWholeProtection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.ThornTrap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.TripleLaser>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.TwilightAlchemy1>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.TwilightAlchemy2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.TwilightAlchemy3>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.UpgradeWeapon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.Vaporize>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.Vending>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.VulcanArm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.WallOfThorns>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.WeaponPerfection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.WeaponRepair>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.WoodenFairy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Merchant.WoodenWarrior>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.CastNinjaSpell>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ColdBloodedCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.CrimsonFireFormation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.CrimsonFirePetal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.DarkDragonNightmare>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.DarkeningCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.DistortedCrescent>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.EarthCharm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.EmptyShadow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.FinalStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.FireCharm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.GoldenDragonCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.HiddenWater>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.HuumaShurikenConstruct>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.HuumaShurikenGrasp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.IceCharm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.IceMeteor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.IllusionBewitch>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.IllusionDeath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.IllusionShadow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.IllusionShock>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ImprovisedDefense>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.Infiltrate>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.Kamaitachi>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.KoCrossSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.KunaiDistortion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.KunaiExplosion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.KunaiNightmare>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.KunaiRefraction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.KunaiRotation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.KunaiSplash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.LightningStrikeOfDestruction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.Makibishi>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.MeltAway>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.Mirage>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.MirrorImage>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.MoonlightFantasy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.NightmareErasion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.OminousMoonlight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.RagingFireDragon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.RapidThrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.RedFlameCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ReleaseNinjaSpell>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowDance>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowFlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowHiding>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowHunting>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowLeap>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowNightmare>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowTrampling>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ShadowWarrior>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.SoulCutter>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.SpearOfIce>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.SwirlingPetal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ThrowHuumaShuriken>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ThrowKunai>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ThrowShuriken>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ThrowZeny>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.ThunderingCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.VanishingSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.WindBlade>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Ninja.WindCharm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.DoubleBowlingBash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.FirstAid>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.GroundGravitation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.HellsDrive>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.HelpAngel>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.JackFrostNova>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.JupitelThunderstorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.MegaSonicBlow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.MeteorStormBuster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.NapalmVulcanStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.ShieldChainRush>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Novice.SpiralPierceMax>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.AcidBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.AgilityUp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.AntiMagic>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.AttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Bleeding2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Bleeding>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.BlindAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.BreakArmor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.BreakHelm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.BreakShield>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.CaneOfEvilEye>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.ChangeLocation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Comet2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.CriticalWounds>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.CrossOfDarkness>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.CurseAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DancingBlade>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DarkBlessing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DarkBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DarkPiercing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DarknessBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DarknessJupitel>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DeadlyCurse2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DeadlyCurse>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DeathSummon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DecreaseAllStats>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DemonShockAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.DragonFear>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.EarthAttributeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.EarthAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Earthquake>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Emotion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.EmotionOn>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.EnergyDrain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.EvilLand>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Expulsion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.FireAttributeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.FireAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.FireBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.FireStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.FlameCross>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.FollowerSummons>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.FullHeal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.GhostAttributeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.GhostAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.GrandCrossOfDarkness>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.GroundDrive>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Hallucination>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.HellBurning>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.HellDignity>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.HellPower>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.HellsJudgement2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.HellsJudgement>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.HolyAttributeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.HolyAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.IceBreath2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.IceBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.IceMine>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.IncreasedGravity>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.InvincibleOff>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Invisible>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.JackFrost2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Leash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.LexAeterna2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Lick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Metamorphosis>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.MilleniumShield2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.MonsterSummons>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.MultiStageAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcArrowStorm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcCloudKill>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcColuceoHeal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcCursedCircle>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcDragonBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcElectricWalk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcFatalMenace>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcFireWalk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcHowlingOfMandragora>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcIgnitionBreak>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcMagmaEruption>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcPhantomThrust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcPoisonBuster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcPsychicWave>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcRayOfGenesis>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcRun>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcSuicide>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.NpcVenomImpress>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PetrifyAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PiercingAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PoisonAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PoisonAttributeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PoisonAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PowerUp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PropertyImmune>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Provocation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PulseStrike2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.PulseStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.RainOfMeteor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.RandomAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.RandomMove>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Rebirth>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.RecallSlaves>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Revenge>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Reverberation2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.ShadowAttributeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.ShadowAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SiegeMode>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SilenceAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SleepAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SlowCast>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Smoking>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SoulStrikeOfDarkness>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SpeedUp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SpiritDestruction>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SplashAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.StoneSkin>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Stop>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.StormGust2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.StunAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SuckingBlood>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.SuicideBombing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Talk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.ThunderBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.Transformation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.UndeadAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.UndeadElementAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.VampireGift>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.VenomFog>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WaterAttributeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WaterAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideBleeding2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideBleeding>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideConfusion2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideConfusion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideCriticalWounds>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideCurse2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideCurse>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideFreeze2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideFreeze>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideLeash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WidePetrify2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WidePetrify>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideSight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideSilence2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideSilence>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideSleep2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideSleep>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideSoulDrain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideStun2>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideStun>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideSuck>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WideWeb>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WindAttributeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Npc.WindAttributeChange>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.Baby>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.BattleBuster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.CallAllFamily>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.CallBaby>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.CallParent>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.CatCry>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.CheerUp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.ChristmasCarol>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.DualCannonFire>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.EquipSwitch>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.GmSandman>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.GuardiansRecall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.ILookUpToYou>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.IMissYou>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.IWillProtectYou>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.InfinityBuster>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.NetRepair>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.NetSupport>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.NiflheimRecall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.OdinsRecall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.OneForever>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.OpenBuyingStore>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.PartyAssumptio>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.PartyBlessing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.PartyFlee>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.PartyIncreaseAgi>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.PeonyMamy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.PronteraRecall>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.RayOfProtection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.ReturnToEclage>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.ReturnToEldicastes>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.ReturnToGlastHeim>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.ReturnToLighthalzen>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.ReturnToThanatos>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.Ro20thAnniversaryFirecracker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.Sadagui>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.SequoiaDust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.SnowFlip>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.SummerNightDream>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Other.WeaponEnchantment>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.Bite>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.BlessingofMysticalCreatures>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.BunchofShrimp>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.CatnipMeteor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.CatnipPowdering>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.Chattering>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.ChulhoSonicClaw>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.ColorsofHyunrok>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.Grooming>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.Hiss>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.HogogongStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.HowlingofChulho>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.HyunrokBreeze>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.HyunrokCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.KisulRampage>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.KisulWaterSpraying>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.Lope>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.LunaticCarrotBeat>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.MarineFestivalofKisul>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.MeowMeow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.NyangGrass>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.PickyPeck>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.PowerofFlock>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.Purring>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.SandyFestivalofKisul>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.ScarofTarou>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.Scratch>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.SilvervineRootTwist>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.SilvervineStemSpear>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.SpiritofSavage>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.TastyShrimpParty>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.TunaBelly>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Summoner.TunaParty>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Abundance>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.AutoBerserk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Banding>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.BanishingPoint>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.BattleChant>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.BowlingBash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.BrandishSpear>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.CannonSpear>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ChargeAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.CounterAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.CrossRain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.CrushStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.DragonBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.DragonHowling>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.DragonicAura>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.DragonicBreath>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.EarthDrive>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.EnchantBlade>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.FightingSpirit>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ForceOfVanguard>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.GiantGrowth>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.GloriaDomini>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.GrandCross>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.GrandJudgement>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.GuardianShield>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.HackAndSlasher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.HesperusLit>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.HolyCross>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.HundredSpear>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.IgnitionBreak>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ImperialCross>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.JudgementCross>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.KingsGrace>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.LuxAnima>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.MadnessCrusher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.MartyrsReckoning>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.MilleniumShield>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.MoonSlasher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.OverBrand>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.OverSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.PhantomThrust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Pierce>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Piety>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.PinpointAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ProvokeSelf>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.RadiantSpear>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.RageBurst>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.RayOfGenesis>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Refresh>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Relax>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ResistantSouls>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Sacrifice>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ServantWeapon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ServantWeaponDemolition>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ServantWeaponPhantom>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ServantWeaponSign>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ShieldBoomerang>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ShieldChain>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ShieldPress>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ShieldReflect>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ShieldShooting>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.ShieldSpell>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Smite>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.SonicWave>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.SpearBoomerang>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.SpearStab>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.SpiralPierce>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.StoneHardSkin>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.StormBlast>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.StormSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.Trample>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.TraumaticBlow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.UltimateSacrifice>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.VitalStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.VitalityActivation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Swordman.WindCutter>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.AllInTheSky>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.BookofCreatingStar>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.CircleOfDirectionsAndElementals>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Counter>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.CurseExplosion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.DawnBreak>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.DocumentofSunMoonAndStar>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.DownKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Esha>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Eska>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Eske>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Esma>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Espa>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Estin>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Estun>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Eswhoo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Eswoo>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.ExorcismOfMaliciousSoul>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.FairysSoul>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.FalconsSoul>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.FallingStar>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.FeelingtheSunMoonandStars>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.FlashKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.FullMoonKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.GolemsSoul>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.GravityControl>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.HatredoftheSunMoonandStars>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.HighJump>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.JumpKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Kaahi>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Kaite>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Kaizel>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Kaupe>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Kaute>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.MidnightKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Mission>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.NewMoonKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.NoonBlast>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.NovaExplosion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.ProminenceKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.RisingMoon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.RisingSun>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.Run>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SevenWind>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.ShadowsSoul>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SolarBurst>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SoulCollect>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SoulCurse>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SoulDivision>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SoulExplosion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SoulGathering>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SoulOfHeavenAndEarth>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SoulRevolution>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SoulUnity>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritofRebirth>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheAlchemist>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheArtist>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheAssasin>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheBlacksmith>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheCrusader>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheHunter>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheKnight>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheMonk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritofthePriest>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheRogue>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheSage>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheSoulLinker>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheStarGladiator>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheSupernovice>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SpiritoftheWizard>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.StarBurst>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.StarCannon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.StarEmperorAdvent>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.StormKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.SunsetBlast>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfBlackTortoise>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfBlueDragon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfFiveElements>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfFourBearingGod>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfMagician>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfProtection>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfRedPhoenix>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfSoulStealing>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfWarrior>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TalismanOfWhiteTiger>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TotemOfTutelary>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TurnKick>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.TwinklingGalaxy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.WarmthoftheMoon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.WarmthoftheStars>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Taekwon.WarmthoftheSun>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.AbyssDagger>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.AbyssSquare>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Antidote>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.AutoShadowSpell>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.BackSlide>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.BackStab>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.BloodyLust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.BodyPainting>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.ChainReactionShot>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.ChaosPanic>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Cloaking>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CloakingExceed>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CloseConfine>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CounterInstinct>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CounterSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CreateDeadlyPoison>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CreateNewPoison>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CrossImpact>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CrossRipperSlasher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.CrossSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DancingKnife>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DarkClaw>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DarkIllusion>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DeftStab>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Detoxify>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DimensionDoor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DivestAll>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DivestArmor>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DivestHelm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DivestShield>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DivestWeapon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.DoubleAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.EmergencyEscape>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.EnchantDeadlyPoison>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.EnchantPoison>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Envenom>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.EternalSlash>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.FatalMenace>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.FatalShadowCrow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.FeintBomb>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.FindStone>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.FrenzyShot>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.FromTheAbyss>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Grimtooth>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.HallucinationWalk>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.ImpactCrater>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Invisibility>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Maelstrom>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.ManHole>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.MasqueradeEnervation>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.MasqueradeGloomy>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.MasqueradeIgnorance>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.MasqueradeLaziness>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.MasqueradeUnlucky>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.MasqueradeWeakness>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.MeteorAssault>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Mug>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.OmegaAbyssStrike>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.PhantomMenace>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.PoisonSmoke>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.PoisoningWeapon>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Remover>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Reproduce>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.RollingCutter>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.SandAttack>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.SavageImpact>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Scribble>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.ShadowForm>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.ShadowStab>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.SightlessMind>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Snatch>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.SonicBlow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.SoulDestroyer>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Steal>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.Stealth>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.StoneFling>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.StripAccessory>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.StripShadow>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.ThrowVenomKnife>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.TriangleShot>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.UnluckyRush>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.VenomDust>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.VenomPressure>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.VenomSplasher>();
builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillImpl, Map.Server.Skills.Behaviors.Thief.WeaponCrush>();

builder.Services.AddSingleton<Map.Server.Skills.Behaviors.SkillBehaviorRegistry>();

// T2.3-H1/H2/H3 — central broadcaster for skill-result packets
// (clif_skill_nodamage / clif_skill_damage / clif_skill_fail). Each
// SkillImpl body resolves through this façade instead of building
// raw ZC packets — keeps per-skill code on the high-level intent
// ("this cast healed N HP", "this hit dealt M damage") rather than
// the wire format.
builder.Services.AddSingleton<Map.Server.Skills.ISkillClientService, Map.Server.Skills.SkillClientService>();

// T2.3-H4 — deferred per-skill callback scheduler (rAthena
// skill_addtimerskill). Multi-hit skills (Sonic Blow, Storm Gust,
// Adoramus, Tetra Vortex) capture a closure with their post-delay
// logic and the scheduler fires it at the target tick. Ticked in
// MapServerImpl.OnTick after SkillCastService.
builder.Services.AddSingleton<Map.Server.Skills.ISkillTimerService, Map.Server.Skills.SkillTimerService>();

// Standard SkillResolverRegistry now hand-wired here (was previously
// only in SkillCastService's test ctor). The five generic resolvers
// run when no plugin claims the cast.
builder.Services.AddSingleton<Map.Server.Skills.Resolvers.ISkillResolver, Map.Server.Skills.Resolvers.WeaponSkillResolver>();
builder.Services.AddSingleton<Map.Server.Skills.Resolvers.ISkillResolver, Map.Server.Skills.Resolvers.MagicSkillResolver>();
builder.Services.AddSingleton<Map.Server.Skills.Resolvers.ISkillResolver, Map.Server.Skills.Resolvers.HealSkillResolver>();
builder.Services.AddSingleton<Map.Server.Skills.Resolvers.ISkillResolver, Map.Server.Skills.Resolvers.StatusSkillResolver>();
builder.Services.AddSingleton<Map.Server.Skills.Resolvers.ISkillResolver, Map.Server.Skills.Resolvers.MiscSkillResolver>();
builder.Services.AddSingleton<Map.Server.Skills.Resolvers.SkillResolverRegistry>();

builder.Services.AddSingleton<Map.Server.Skills.ISkillCastService, Map.Server.Skills.SkillCastService>();

// Skill ground units (skill.cpp:skill_unitsetting +
// skill_unit_onplace_timer). T3.4 — per-skill behavior moved out of
// SkillUnitService into ISkillUnitTickHandler plugins, indexed by
// SkillUnitTickRegistry. Add a handler line per ground-unit skill.
builder.Services.AddSingleton<Map.Server.Skills.Units.ISkillUnitTickHandler, Map.Server.Skills.Units.Handlers.MagnusExorcismusUnit>();
builder.Services.AddSingleton<Map.Server.Skills.Units.ISkillUnitTickHandler, Map.Server.Skills.Units.Handlers.StormGustUnit>();
builder.Services.AddSingleton<Map.Server.Skills.Units.ISkillUnitTickHandler, Map.Server.Skills.Units.Handlers.PneumaUnit>();
builder.Services.AddSingleton<Map.Server.Skills.Units.ISkillUnitTickHandler, Map.Server.Skills.Units.Handlers.SafetyWallUnit>();
builder.Services.AddSingleton<Map.Server.Skills.Units.ISkillUnitTickHandler, Map.Server.Skills.Units.Handlers.SanctuaryUnit>();
builder.Services.AddSingleton<Map.Server.Skills.Units.SkillUnitTickRegistry>();
builder.Services.AddSingleton<Map.Server.Skills.Units.ISkillUnitContext, Map.Server.Skills.Units.SkillUnitContext>();
builder.Services.AddSingleton<Map.Server.Skills.ISkillUnitService, Map.Server.Skills.SkillUnitService>();

// T3.5 — splash iteration helper. Per-skill behaviors call
// IMapForeachInRangeService.ForEachEnemyInSplash(...) instead of
// reimplementing map_foreachinrange + BCT_* filtering inline.
builder.Services.AddSingleton<Map.Server.Skills.Splash.IMapForeachInRangeService, Map.Server.Skills.Splash.MapForeachInRangeService>();

// Natural HP/SP regen (status.cpp:status_natural_heal). Baseline
// out-of-combat recovery for both players and mobs; walking gates
// HP for PCs, dead/full pools skip.
builder.Services.AddSingleton<Map.Server.Status.INaturalHealService, Map.Server.Status.NaturalHealService>();

// GM commands. Each IGmCommand is registered as a singleton; the registry
// indexes them by Name at construction. ChatMessageHandler discovers them
// via DI.
builder.Services.AddSingleton<IGmCommand, WhereCommand>();
builder.Services.AddSingleton<IGmCommand, KillMobCommand>();
builder.Services.AddSingleton<IGmCommand, WarpCommand>();
builder.Services.AddSingleton<IGmCommand, DamageCommand>();
builder.Services.AddSingleton<IGmCommand, StorageCommand>();
// Quick-win admin commands ported from rAthena atcommand.cpp:
//   @heal — atcommand_heal
//   @item — atcommand_item
//   @level — atcommand_baselevelup
//   @reloaddb — atcommand_reloaditemdb / mobdb / skilldb collapsed.
builder.Services.AddSingleton<IGmCommand, HealCommand>();
builder.Services.AddSingleton<IGmCommand, ItemCommand>();
builder.Services.AddSingleton<IGmCommand, LevelCommand>();
builder.Services.AddSingleton<IGmCommand, ReloadDbCommand>();
// Meta atcommands — @help / @commands / @charcommands read from
// conf/atcommands.yml and conf/groups.yml.
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.HelpCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.CommandsCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.CharCommandsCommand>();

// Wave A atcommands — backend already exists.
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.AliveCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.KillCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.SpeedCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.ZenyCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.JobLevelCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.MapInfoCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.UsersCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.TimeCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.RefreshCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.MeCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.VersionCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.UptimeCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.WhoCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.JumpCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.JumpToCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.RecallCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.SaveCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.LoadCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.HideCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.MonsterCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.BroadcastCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.LocalBroadcastCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.PvpOnCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.PvpOffCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.GvgOnCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.GvgOffCommand>();

// PC-* phase: player option / appearance / orb services + GM commands.
// rAthena pc_setoption / pc_setcart / pc_setriding / pc_changelook
// (pc.cpp:8702 / 8851 / 8810 / clif.cpp:3929) + the pc_addspiritball
// family. See [pc-parity.md] for the full subsystem audit.
builder.Services.AddSingleton<Map.Server.Status.IPlayerOptionService, Map.Server.Status.PlayerOptionService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerLookService, Map.Server.Status.PlayerLookService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerOrbService, Map.Server.Status.PlayerOrbService>();
builder.Services.AddSingleton<Map.Server.Skills.IPlayerSkillService, Map.Server.Skills.PlayerSkillService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerFameService, Map.Server.Status.PlayerFameService>();
builder.Services.AddSingleton<Map.Server.Scripting.Vars.IPlayerVarService, Map.Server.Scripting.Vars.PlayerVarService>();
builder.Services.AddSingleton<Map.Server.Status.IJobChangeService, Map.Server.Status.JobChangeService>();

// pc.cpp Wave 5-13 helpers — full surface for the remaining
// canonical rAthena entry points. Stubbed where the backend
// subsystem hasn't ported yet (documented inline in each impl).
builder.Services.AddSingleton<Map.Server.Inventory.IPlayerEquipHelpers, Map.Server.Inventory.PlayerEquipHelpers>();
builder.Services.AddSingleton<Map.Server.Inventory.IPlayerInventoryHelpers, Map.Server.Inventory.PlayerInventoryHelpers>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerBonusService, Map.Server.Status.PlayerBonusService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerTimerService, Map.Server.Status.PlayerTimerService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerBgQueueTimerService, Map.Server.Status.PlayerBgQueueTimerService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerHateService, Map.Server.Status.PlayerHateService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerJailService, Map.Server.Status.PlayerJailService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerRelationService, Map.Server.Status.PlayerRelationService>();
builder.Services.AddSingleton<Map.Server.Status.AttendanceYmlLoader>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerAttendanceService>(sp =>
{
    var log = sp.GetRequiredService<ILogger<Map.Server.Status.PlayerAttendanceService>>();
    var svc = new Map.Server.Status.PlayerAttendanceService(log);
    var loader = sp.GetRequiredService<Map.Server.Status.AttendanceYmlLoader>();
    svc.SetSchedule(loader.Load(ResolveConfigPath("attendance.yml")));
    return svc;
});
builder.Services.AddSingleton<Map.Server.Status.IPlayerQuestMarkerService, Map.Server.Status.PlayerQuestMarkerService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerStealService, Map.Server.Status.PlayerStealService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerReputationService, Map.Server.Status.PlayerReputationService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerVersionDisplayService, Map.Server.Status.PlayerVersionDisplayService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerReviveItemService, Map.Server.Status.PlayerReviveItemService>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerMacroDetectorService, Map.Server.Status.PlayerMacroDetectorService>();
builder.Services.AddSingleton<Map.Server.Movement.IPlayerPositionHelpers, Map.Server.Movement.PlayerPositionHelpers>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerLifecycleHelpers, Map.Server.Status.PlayerLifecycleHelpers>();
builder.Services.AddSingleton<Map.Server.Status.IPlayerStatHelpers, Map.Server.Status.PlayerStatHelpers>();

builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.MountCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.JobCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.CartCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.OptionCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.SpiritballCommand>();
builder.Services.AddSingleton<IGmCommand, Map.Server.Gm.Commands.SoulballCommand>();

// Wave B atcommand stubs — backend subsystem pending. Each is a
// well-formed registry entry so @commands / @help still see the
// rAthena name, but invocation replies "not yet implemented".
// Real implementations supersede these as the backend lands.
{
    var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Names already registered above. The stub list intentionally
        // omits these so we don't double-register the same command name.
        "where","killmob","warp","damage","storage","heal","item","level","reloaddb",
        "help","commands","charcommands",
        "alive","kill","speed","zeny","joblevelup","mapinfo","users","servertime",
        "refresh","me","version","uptime","who","jump","jumpto","recall","save",
        "load","hide","monster","broadcast","localbroadcast","pvpon","pvpoff",
        "gvgon","gvgoff",
        // PC-* phase
        "mount","cart","option","spiritball","soulball","jobchange",
    };
    foreach (var spec in Map.Server.Gm.Commands.StubCommandKinds.Specs)
    {
        if (!existing.Add(spec.Name)) continue;
        builder.Services.AddSingleton<IGmCommand>(sp =>
            new Map.Server.Gm.Commands.StubCommand(
                spec.Name, spec.Subsystem,
                sp.GetRequiredService<Map.Server.Visibility.IVisibilityService>()));
    }
}

// atcommands.yml + groups.yml — rAthena conf/* parity. Loaded once at
// startup. Path discovery follows the appsettings convention used by
// MapDataPaths: try local config dir first, fall back to the rathena
// reference tree so a dev box without a copy still boots.
builder.Services.AddSingleton<Map.Server.Gm.Config.IAtCommandConfig>(sp =>
{
    var log = sp.GetRequiredService<ILogger<Map.Server.Gm.Config.AtCommandConfig>>();
    return new Map.Server.Gm.Config.AtCommandConfig(ResolveConfigPath("atcommands.yml"), log);
});
builder.Services.AddSingleton<Map.Server.Gm.Config.IPlayerGroupConfig>(sp =>
{
    var log = sp.GetRequiredService<ILogger<Map.Server.Gm.Config.PlayerGroupConfig>>();
    return new Map.Server.Gm.Config.PlayerGroupConfig(ResolveConfigPath("groups.yml"), log);
});
builder.Services.AddSingleton<Map.Server.Gm.Config.IPermissionService, Map.Server.Gm.Config.PermissionService>();
builder.Services.AddSingleton<IAtCommandLogger, AtCommandLogger>();

builder.Services.AddSingleton<IGmCommandRegistry, GmCommandRegistry>();

static string ResolveConfigPath(string name)
{
    var local = Path.Combine(AppContext.BaseDirectory, "config", name);
    if (File.Exists(local)) return local;
    var rathena = Path.Combine("/Volumes/1TB/Projetos/rathena/conf", name);
    return rathena;
}

// Core services
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<PacketSystem>();
builder.Services.AddSingleton<IPacketFactory>(sp => sp.GetRequiredService<PacketSystem>().Factory);
builder.Services.AddSingleton<IPacketSizeRegistry>(sp => sp.GetRequiredService<PacketSystem>().Registry);

// Auto-register all packet handlers from assembly
var handlerTypes = typeof(MapServerImpl).Assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract)
    .Where(t => t.GetCustomAttribute<PacketHandlerAttribute>() != null);

foreach (var handlerType in handlerTypes)
{
    builder.Services.AddTransient(handlerType);
}

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Configure gRPC
builder.Services.AddGrpc();
builder.WebHost.ConfigureKestrel(options =>
{
    // gRPC over cleartext (h2c) requires an HTTP/2-only endpoint.
    options.ListenAnyIP(serverConfig.GrpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();
app.MapGrpcService<MapGrpcService>();

// Get server instance from DI
var server = app.Services.GetRequiredService<MapServerImpl>();

// Start gRPC server in background
_ = Task.Run(async () => await app.RunAsync());

// Setup graceful shutdown
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await server.StartAsync(cts.Token);
    
    Log.Information("MapServer is running at {FPS} FPS. Press Ctrl+C to stop.", serverConfig.TargetFPS);
    
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Log.Information("Shutdown requested...");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
}
finally
{
    await server.StopAsync();
    await app.StopAsync();
    Log.CloseAndFlush();
}
