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

builder.Services.AddSingleton<Map.Server.Skills.ISkillCastService, Map.Server.Skills.SkillCastService>();

// Skill ground units (skill.cpp:skill_unitsetting +
// skill_unit_onplace_timer). Starter set: Magnus Exorcismus, Storm
// Gust. Defensive units (Safety Wall / Pneuma) layer in via the
// same lifecycle once the damage-interception hook lands.
builder.Services.AddSingleton<Map.Server.Skills.ISkillUnitService, Map.Server.Skills.SkillUnitService>();

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
