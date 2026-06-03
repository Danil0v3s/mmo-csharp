using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Resolvers;
using Map.Server.Status;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// First-slice port of rAthena <c>skill_use_id</c> + <c>skill_castend_*</c>
/// chain (skill.cpp). Drives:
/// <list type="number">
///   <item>Lookup the skill in <see cref="ISkillDb"/>.</item>
///   <item>Validate target type / range / sp / cooldown / level.</item>
///   <item>If cast time &gt; 0, schedule the resolution via <see cref="Tick"/>.
///         Otherwise resolve immediately (rAthena instant-cast).</item>
///   <item>On resolve: route through <see cref="ResolveSkill"/> which
///         dispatches by <see cref="SkillDamageKind"/>.</item>
/// </list>
///
/// Per-skill handlers do NOT live in a separate dispatch — for the
/// starter set the resolve switch covers each <see cref="SkillDamageKind"/>;
/// idiosyncratic skills (Pneuma, Storm Gust, etc.) plug in via
/// <see cref="ResolveSkill"/> overrides as they port.
/// </summary>
public sealed class SkillCastService : ISkillCastService
{
    private readonly ISkillDb _db;
    private readonly IEntityRegistry _entities;
    private readonly SkillResolverRegistry _resolvers;
    private readonly IMapFlagService? _mapFlags;
    private readonly IMapWorldRegistry? _maps;
    private readonly IStatusChangeService? _sc;
    private readonly ISkillCastTimingService? _timing;
    // T2.5 — per-skill plugin layer. Consults the registry before
    // falling back to the generic DamageKind dispatch. Optional so
    // the legacy ctor + tests that don't wire it keep working.
    private readonly SkillBehaviorRegistry? _behaviors;
    private readonly IBattleCalculator? _battleCalc;
    private readonly IDamageService? _damage;
    private readonly ISkillClientService? _client;
    // P0 plumbing — every cross-cutting helper a per-skill plugin
    // may need, threaded into SkillBehaviorContext.
    private readonly Map.Server.Party.IPartyMapService? _partyMap;
    private readonly Map.Server.Party.IPartyService? _party;
    private readonly IPlayerSkillService? _playerSkill;
    private readonly Map.Server.Status.IPlayerOrbService? _orbs;
    private readonly Map.Server.Inventory.IEquipService? _equip;
    private readonly Map.Server.Movement.UnitOps.IUnitOpsService? _unitOps;
    private readonly Map.Server.Movement.IPcSetposService? _setpos;
    private readonly Map.Server.Spawn.IMobSpawnService? _mobSpawn;
    private readonly Map.Server.Spawn.MobOps.IMobOpsService? _mobOps;
    private readonly ISkillAttackService? _skillAttack;
    private readonly ISkillSideEffectService? _sideEffect;
    private readonly Map.Server.Session.IMapSessionRegistry? _sessions;
    private readonly Map.Server.Status.IPlayerOptionService? _options;
    private readonly Map.Server.Pathing.IPathService? _paths;
    private readonly Map.Server.Status.IPlayerStealService? _steal;
    private readonly Map.Server.Combat.IPcDeathService? _death;
    private readonly Map.Server.Status.StatusOps.IStatusOpsService? _statusOps;
    private readonly ISkillUnitService? _skillUnits;
    private readonly Map.Server.Elemental.IElementalService? _elemental;
    private readonly Map.Server.Shop.Buying.IBuyingStoreService? _buyingStore;
    private readonly Map.Server.Pet.PetOps.IPetOpsService? _petOps;
    private readonly Map.Server.Homunculus.IHomunculusService? _homunculus;
    private readonly ISkillRequirementService? _requirements;
    private readonly Map.Server.Items.IItemGroupService? _itemGroups;
    private readonly Map.Server.Items.IItemCatalog? _catalog;
    private readonly Map.Server.Items.IItemDropService? _drops;
    private readonly ISkillProductionService? _production;
    private readonly IProduceRecipeService? _recipes;
    private readonly IAbraDatabase? _abra;
    private readonly Map.Server.Inventory.IInventoryService? _inventory;
    // COMBAT-58 — ammo gate/consume for ammo-using skills.
    private readonly Map.Server.Inventory.IAmmoService? _ammo;
    private readonly ILogger<SkillCastService> _logger;

    private readonly List<PendingCast> _pending = new();
    private readonly List<PendingPosCast> _pendingPos = new();
    private readonly Dictionary<(EntityId, ushort), long> _cooldowns = new();

    public SkillCastService(
        ISkillDb db,
        IEntityRegistry entities,
        SkillResolverRegistry resolvers,
        ILogger<SkillCastService> logger,
        IMapFlagService? mapFlags = null,
        IMapWorldRegistry? maps = null,
        IStatusChangeService? sc = null,
        ISkillCastTimingService? timing = null,
        SkillBehaviorRegistry? behaviors = null,
        IBattleCalculator? battleCalc = null,
        IDamageService? damage = null,
        ISkillClientService? client = null,
        Map.Server.Party.IPartyMapService? partyMap = null,
        Map.Server.Party.IPartyService? party = null,
        IPlayerSkillService? playerSkill = null,
        Map.Server.Status.IPlayerOrbService? orbs = null,
        Map.Server.Inventory.IEquipService? equip = null,
        Map.Server.Movement.UnitOps.IUnitOpsService? unitOps = null,
        Map.Server.Movement.IPcSetposService? setpos = null,
        Map.Server.Spawn.IMobSpawnService? mobSpawn = null,
        Map.Server.Spawn.MobOps.IMobOpsService? mobOps = null,
        ISkillAttackService? skillAttack = null,
        ISkillSideEffectService? sideEffect = null,
        Map.Server.Session.IMapSessionRegistry? sessions = null,
        Map.Server.Status.IPlayerOptionService? options = null,
        Map.Server.Pathing.IPathService? paths = null,
        Map.Server.Status.IPlayerStealService? steal = null,
        Map.Server.Combat.IPcDeathService? death = null,
        Map.Server.Status.StatusOps.IStatusOpsService? statusOps = null,
        ISkillUnitService? skillUnits = null,
        Map.Server.Elemental.IElementalService? elemental = null,
        Map.Server.Shop.Buying.IBuyingStoreService? buyingStore = null,
        Map.Server.Pet.PetOps.IPetOpsService? petOps = null,
        Map.Server.Homunculus.IHomunculusService? homunculus = null,
        ISkillRequirementService? requirements = null,
        Map.Server.Items.IItemGroupService? itemGroups = null,
        Map.Server.Items.IItemCatalog? catalog = null,
        Map.Server.Items.IItemDropService? drops = null,
        ISkillProductionService? production = null,
        IProduceRecipeService? recipes = null,
        IAbraDatabase? abra = null,
        Map.Server.Inventory.IInventoryService? inventory = null,
        Map.Server.Inventory.IAmmoService? ammo = null)
    {
        _db = db;
        _entities = entities;
        _resolvers = resolvers;
        _mapFlags = mapFlags;
        _maps = maps;
        _sc = sc;
        _timing = timing;
        _behaviors = behaviors;
        _battleCalc = battleCalc;
        _damage = damage;
        _client = client;
        _partyMap = partyMap;
        _party = party;
        _playerSkill = playerSkill;
        _orbs = orbs;
        _equip = equip;
        _unitOps = unitOps;
        _setpos = setpos;
        _mobSpawn = mobSpawn;
        _mobOps = mobOps;
        _skillAttack = skillAttack;
        _sideEffect = sideEffect;
        _sessions = sessions;
        _options = options;
        _paths = paths;
        _steal = steal;
        _death = death;
        _statusOps = statusOps;
        _skillUnits = skillUnits;
        _elemental = elemental;
        _buyingStore = buyingStore;
        _petOps = petOps;
        _homunculus = homunculus;
        _requirements = requirements;
        _itemGroups = itemGroups;
        _catalog = catalog;
        _drops = drops;
        _production = production;
        _recipes = recipes;
        _abra = abra;
        _inventory = inventory;
        _ammo = ammo;
        _logger = logger;
    }

    /// <summary>
    /// Test-only ctor — wires the five standard resolvers from concrete
    /// services. Keeps the existing test surface working without DI.
    /// </summary>
    public SkillCastService(
        ISkillDb db,
        IEntityRegistry entities,
        IDamageService damage,
        IBattleCalculator battle,
        IStatusChangeService scService,
        ILogger<SkillCastService> logger)
        : this(db, entities,
            new SkillResolverRegistry(new ISkillResolver[]
            {
                new WeaponSkillResolver(battle, damage),
                new MagicSkillResolver(damage),
                new HealSkillResolver(),
                new StatusSkillResolver(scService),
                new MiscSkillResolver(damage),
            }),
            logger)
    {
    }

    public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return SkillCastResult.UnknownSkill;
        if (skillLevel < 1 || skillLevel > def.MaxLevel) return SkillCastResult.LevelOutOfRange;

        // rAthena status_check_skilluse: STONE / FREEZE / STUN / SLEEP /
        // SILENCE / CONFUSION refuse skill casts (status.cpp:1763).
        // Mob and NPC sources bypass — their skill scripts run on engine
        // authority.
        if (source is PlayerEntity && !source.CanCastSkill(_sc))
        {
            return SkillCastResult.CannotAct;
        }

        // rAthena skill.cpp:skill_check_condition_castbegin: `noskill`
        // mapflag refuses all skill casts on this map. Mobs / NPC scripts
        // skip the check — they're authoritative content.
        if (_mapFlags != null && _maps != null && source is PlayerEntity)
        {
            var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == source.MapId);
            if (map != null && _mapFlags.IsSet(map.Name, Map.Server.World.MapFlag.NoSkill))
            {
                return SkillCastResult.MapRefused;
            }
        }

        // Players must have learned the skill at the requested level
        // (rAthena pc_checkskill). Mobs / NPCs bypass — they always know
        // their skills, configured via mob_skill_db / npc scripts.
        if (source is PlayerEntity pcSource)
        {
            var learned = pcSource.LearnedSkills.GetValueOrDefault(skillId);
            if (learned < skillLevel) return SkillCastResult.LevelOutOfRange;
        }

        var target = _entities.Get(targetId);
        if (target == null) return SkillCastResult.TargetUnknown;
        if (!IsAlive(target)) return SkillCastResult.TargetDead;
        if (target.MapId != source.MapId) return SkillCastResult.TargetUnknown;

        if (def.Target == SkillTargetMode.TargetEnemy && target is not (MobEntity or PlayerEntity))
            return SkillCastResult.InvalidTargetType;

        // Range — Chebyshev distance same as the auto-attack path.
        var dist = Math.Max(Math.Abs(source.X - target.X), Math.Abs(source.Y - target.Y));
        if (dist > def.Range) return SkillCastResult.OutOfRange;

        // SP cost.
        var spCost = def.SpCost.Length > skillLevel ? def.SpCost[skillLevel] : 0;
        if (source is PlayerEntity pc && pc.Sp < spCost) return SkillCastResult.NotEnoughSp;

        // COMBAT-58 — ammo gate (rAthena skill_check_condition_castbegin ammo arm).
        if (AmmoGateFails(source, def, skillId, skillLevel)) return SkillCastResult.NeedAmmo;

        // Cooldown.
        var cdKey = (source.Id, skillId);
        var now = Environment.TickCount64;
        if (_cooldowns.TryGetValue(cdKey, out var readyAt) && readyAt > now) return SkillCastResult.OnCooldown;

        // Consume SP now (rAthena: pre-cast SP deduction) — UNLESS this is a menuskill whose
        // requirement is deferred to the destination pick (SKILL_NOCONSUME_REQ; COMBAT-86), so
        // cancelling the chooser costs nothing.
        if (source is PlayerEntity pc2 && !IsDeferredConsumeMenuSkill(skillId))
        {
            pc2.Sp -= spCost;
        }

        // Cast time pipeline: pull the raw value from skill_db, then run
        // through the canonical castfix path so DEX scaling + config
        // rates apply (rAthena skill_castfix, skill.cpp:20193). If the
        // timing service isn't wired (test ctor) we fall back to raw.
        var castTime = _timing != null
            ? _timing.CastFix(source, skillId, skillLevel)
            : (def.CastTimeMs.Length > skillLevel ? def.CastTimeMs[skillLevel] : 0);
        if (castTime <= 0)
        {
            ResolveSkill(source, target, skillId, skillLevel);
        }
        else
        {
            // T5.3a — broadcast cast-start so clients render the
            // casting bar. Instant casts skip the bar (rAthena does
            // the same: clif_skillcasting is only called when
            // skill_castfix returns > 0).
            _client?.BroadcastSkillCasting(source, target, target.X, target.Y,
                skillId, skillLevel, castTime);
            _pending.Add(new PendingCast
            {
                Source = source.Id,
                Target = target.Id,
                SkillId = skillId,
                Level = skillLevel,
                ResolveAt = now + castTime,
            });
        }

        // rAthena skill_delayfix (skill.cpp:20456) — the after-cast
        // delay belongs on the unit's CanAct gate, but we approximate
        // by stacking it onto the cooldown floor today. When the
        // PlayerEntity canact_tick lands the delay flows there.
        var cooldown = def.CooldownMs.Length > skillLevel ? def.CooldownMs[skillLevel] : 0;
        var afterDelay = _timing?.DelayFix(source, skillId, skillLevel) ?? 0;
        var lock_ = Math.Max(cooldown, afterDelay);
        if (lock_ > 0) _cooldowns[cdKey] = now + lock_;

        return SkillCastResult.Started;
    }

    /// <summary>
    /// T4.9g — real ground-cell cast path. Mirrors rAthena
    /// <c>unit_skilluse_pos2</c> + <c>skill_castend_pos2</c>
    /// (unit.cpp / skill.cpp). Validates the skill exists, applies the
    /// same SP / cooldown / map-flag gates as <see cref="StartCast"/>
    /// (target validation is skipped — the target is a cell), then either
    /// schedules a deferred resolve or routes immediately to
    /// <see cref="Behaviors.SkillImpl.CastendPos2"/> when a plugin is
    /// registered. Skills with no plugin fall back to a generic
    /// "no-op" resolution: the parity audit ⚠️ row covers the missing
    /// resolver chain, individual ports plug in via their own SkillImpl.
    /// </summary>
    public SkillCastResult StartCastAt(Entity source, short x, short y, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return SkillCastResult.UnknownSkill;
        if (skillLevel < 1 || skillLevel > def.MaxLevel) return SkillCastResult.LevelOutOfRange;

        if (source is PlayerEntity && !source.CanCastSkill(_sc))
            return SkillCastResult.CannotAct;
        if (_mapFlags != null && _maps != null && source is PlayerEntity)
        {
            var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == source.MapId);
            if (map != null && _mapFlags.IsSet(map.Name, Map.Server.World.MapFlag.NoSkill))
                return SkillCastResult.MapRefused;
        }
        if (source is PlayerEntity pcSource)
        {
            var learned = pcSource.LearnedSkills.GetValueOrDefault(skillId);
            if (learned < skillLevel) return SkillCastResult.LevelOutOfRange;
        }

        // Range — Chebyshev distance from source to cell.
        var dist = Math.Max(Math.Abs(source.X - x), Math.Abs(source.Y - y));
        if (dist > def.Range) return SkillCastResult.OutOfRange;

        var spCost = def.SpCost.Length > skillLevel ? def.SpCost[skillLevel] : 0;
        if (source is PlayerEntity pc && pc.Sp < spCost) return SkillCastResult.NotEnoughSp;

        // COMBAT-58 — ammo gate for ground-targeted ammo skills (e.g. Arrow Shower).
        if (AmmoGateFails(source, def, skillId, skillLevel)) return SkillCastResult.NeedAmmo;

        var cdKey = (source.Id, skillId);
        var now = Environment.TickCount64;
        if (_cooldowns.TryGetValue(cdKey, out var readyAt) && readyAt > now) return SkillCastResult.OnCooldown;

        // COMBAT-86 — defer the SP consume for menuskills (AL_WARP/AL_TELEPORT) to the pick.
        if (source is PlayerEntity pc2 && !IsDeferredConsumeMenuSkill(skillId)) pc2.Sp -= spCost;

        var castTime = _timing != null
            ? _timing.CastFix(source, skillId, skillLevel)
            : (def.CastTimeMs.Length > skillLevel ? def.CastTimeMs[skillLevel] : 0);
        if (castTime <= 0)
        {
            ResolveSkillAt(source, x, y, skillId, skillLevel);
        }
        else
        {
            // T5.3a — broadcast ground-targeted cast-start.
            _client?.BroadcastSkillCasting(source, target: null, x, y,
                skillId, skillLevel, castTime);
            _pendingPos.Add(new PendingPosCast
            {
                Source = source.Id,
                X = x,
                Y = y,
                SkillId = skillId,
                Level = skillLevel,
                ResolveAt = now + castTime,
            });
        }

        var cooldown = def.CooldownMs.Length > skillLevel ? def.CooldownMs[skillLevel] : 0;
        var afterDelay = _timing?.DelayFix(source, skillId, skillLevel) ?? 0;
        var lock_ = Math.Max(cooldown, afterDelay);
        if (lock_ > 0) _cooldowns[cdKey] = now + lock_;

        return SkillCastResult.Started;
    }

    /// <summary>
    /// rAthena <c>skill_castend_pos2</c> (skill.cpp). Routes a
    /// ground-targeted cast to the registered SkillImpl plugin's
    /// <see cref="Behaviors.SkillImpl.CastendPos2"/> hook, or returns
    /// false when no plugin is registered (the per-skill port wave fills
    /// the gaps).
    /// </summary>
    public bool ResolveSkillAt(Entity source, short x, short y, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;

        if (_behaviors != null && _battleCalc != null && _damage != null)
        {
            var plugin = _behaviors.Get(skillId);
            if (plugin != null)
            {
                var ctx = new Behaviors.SkillBehaviorContext(_entities, _damage, _battleCalc, _sc, _client, _partyMap, _party, _playerSkill, _orbs, _equip, _unitOps, _setpos, _mobSpawn, _mobOps, _skillAttack, _sideEffect, _sessions, _maps, _mapFlags, _options, _paths, _steal, _death, _statusOps, _skillUnits, _elemental, this, _buyingStore, _petOps, _homunculus, _requirements, _itemGroups, _catalog, _drops, _production, _recipes, _abra, _inventory);
                plugin.CastendPos2(source, x, y, skillLevel, ctx);
                return true;
            }
        }
        // No SkillImpl plugin registered — generic ground resolvers
        // (e.g. simple AOE-on-cell heal) are not yet a registry; per-skill
        // ports are the canonical path. Returning false lets the caller
        // log / fall back without crashing.
        return false;
    }

    // COMBAT-76 — rAthena ammo-type bits (e_ammo_type) for packet selection / weapon fallback.
    private const int AmmoArrow = 1 << 1, AmmoBullet = 1 << 3, AmmoShell = 1 << 4,
        AmmoGrenade = 1 << 5, AmmoKunai = 1 << 7;

    // COMBAT-58/76 — a skill consumes ammo when the skill_db carries an explicit ammo
    // requirement (skill_get_ammotype != 0), OR — rAthena's `!req.ammo && skill_isammotype`
    // fallback (skill.cpp:19925) — it is a weapon-damage skill cast with an ammo-using
    // weapon (bow/gun). The explicit per-skill mask + qty come from the COMBAT-76 overlay.
    private bool SkillUsesAmmo(PlayerEntity pc, SkillDefinition def, ushort skillId)
        => _db.GetAmmoType(skillId) != 0
           || (def.DamageKind == SkillDamageKind.Weapon
               && Map.Server.Inventory.WeaponTypeCodes.UsesAmmo(pc.WeaponType));

    // rAthena battle_consume_ammo / skill_get_requirement: per-skill qty (≥1 when ammo is
    // used), plus the NW_MAGAZINE_FOR_ONE + W_GATLING special (+4). `gate` adds the 2016
    // renewal "extra ammo" (+1) charged at cast-begin only — the consume removes the base qty.
    private int SkillAmmoQty(ushort skillId, ushort skillLevel, PlayerEntity pc, bool gate)
    {
        var qty = Math.Max(1, _db.GetAmmoQty(skillId, skillLevel));
        if (skillId == SkillIds.NW_MAGAZINE_FOR_ONE
            && pc.WeaponType == Map.Server.Inventory.WeaponTypeCodes.Gatling)
            qty += 4;
        if (gate && IsExtraAmmoSkill(skillId)) qty += 1;
        return qty;
    }

    // skill.cpp:19602 (RENEWAL) — these four skills require one extra ammo to cast.
    private static bool IsExtraAmmoSkill(ushort skillId) => skillId is
        SkillIds.WM_SEVERE_RAINSTORM or SkillIds.RL_FIREDANCE
        or SkillIds.RL_R_TRIP or SkillIds.RL_FIRE_RAIN;

    /// <summary>True (and emits the fail packet) when an ammo-using skill lacks ammo.</summary>
    private bool AmmoGateFails(Entity source, SkillDefinition def, ushort skillId, ushort skillLevel)
    {
        if (source is not PlayerEntity pc || !SkillUsesAmmo(pc, def, skillId)) return false;
        var skillMask = _db.GetAmmoType(skillId);
        if (_ammo?.HasUsableAmmo(pc, SkillAmmoQty(skillId, skillLevel, pc, gate: true), skillMask) != false) return false;

        // rAthena gate packet selection (skill.cpp:19618-19635): bullet/grenade/shell →
        // NEED_MORE_BULLET; kunai → NEED_EQUIPMENT_KUNAI; otherwise (arrows, no/wrong ammo)
        // → clif_arrow_fail. Use the explicit skill ammo mask, falling back to the weapon's
        // own ammo type for skills that ride the weapon heuristic.
        var mask = skillMask != 0 ? skillMask : WeaponAmmoMask(pc.WeaponType);

        if ((mask & (AmmoBullet | AmmoShell | AmmoGrenade)) != 0)
            _client?.BroadcastSkillFail(pc, skillId, Core.Server.Packets.Out.ZC.SkillFailCause.NeedMoreBullet);
        else if ((mask & AmmoKunai) != 0)
            _client?.BroadcastSkillFail(pc, skillId, Core.Server.Packets.Out.ZC.SkillFailCause.NeedEquipmentKunai);
        else
            _client?.BroadcastArrowFail(pc);
        return true;
    }

    // COMBAT-86 — rAthena SKILL_NOCONSUME_REQ menuskills: the SP/item requirement is NOT consumed
    // when the destination chooser opens — only when the player picks a destination (skill_castend_map).
    // Cancelling the chooser therefore costs nothing.
    internal static bool IsDeferredConsumeMenuSkill(ushort skillId)
        => skillId is SkillIds.AL_WARP or SkillIds.AL_TELEPORT;

    // rAthena ammo-type a weapon fires (skill_isammotype / the EQI_AMMO subtype gate):
    // bow → arrow, every gun (Revolver..Grenade) → bullet.
    private static int WeaponAmmoMask(int weaponType) => weaponType switch
    {
        Map.Server.Inventory.WeaponTypeCodes.Bow => AmmoArrow,
        >= Map.Server.Inventory.WeaponTypeCodes.Revolver
            and <= Map.Server.Inventory.WeaponTypeCodes.Grenade => AmmoBullet,
        _ => 0,
    };

    public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        if (!IsAlive(target)) return false;

        // COMBAT-58/76 — consume ammo at castend (rAthena battle_consume_ammo). The gate
        // already ran at cast-begin; ConsumeAmmo no-ops for non-ammo weapons. Consume the
        // base per-skill qty (the renewal +1 extra is a gate-only charge, not consumed).
        if (source is PlayerEntity ammoPc && SkillUsesAmmo(ammoPc, def, skillId))
            _ammo?.ConsumeAmmo(ammoPc, SkillAmmoQty(skillId, skillLevel, ammoPc, gate: false), _db.GetAmmoType(skillId));

        // T2.3 refactor — per-skill SkillImpl plugin first. When a
        // plugin is registered for this skill id, we route through its
        // hook chain (CastendDamageId / CastendNoDamageId based on
        // damage kind) and DO NOT fall back to the generic resolver.
        // The plugin's specialized base (WeaponSkillImpl /
        // StatusSkillImpl / RecursiveDamageSplashSkillImpl) owns the
        // full pipeline. If the plugin needs the generic resolver's
        // behavior, it constructs its own WeaponSkillImpl that runs
        // the standard swing.
        if (_behaviors != null && _battleCalc != null && _damage != null)
        {
            var plugin = _behaviors.Get(skillId);
            if (plugin != null)
            {
                var ctx = new Behaviors.SkillBehaviorContext(_entities, _damage, _battleCalc, _sc, _client, _partyMap, _party, _playerSkill, _orbs, _equip, _unitOps, _setpos, _mobSpawn, _mobOps, _skillAttack, _sideEffect, _sessions, _maps, _mapFlags, _options, _paths, _steal, _death, _statusOps, _skillUnits, _elemental, this, _buyingStore, _petOps, _homunculus, _requirements, _itemGroups, _catalog, _drops, _production, _recipes, _abra, _inventory);
                if (def.DamageKind == SkillDamageKind.None)
                    plugin.CastendNoDamageId(source, target, skillLevel, ctx);
                else
                    plugin.CastendDamageId(source, target, skillLevel, ctx);
                return true;
            }
        }

        // Strategy-pattern dispatch — resolvers keyed by DamageKind.
        // Used for any skill without a SkillImpl plugin (the long tail
        // of vanilla skills that just run base damage / heal / status).
        var resolver = _resolvers.Get(def.DamageKind);
        resolver?.Resolve(source, target, def, skillLevel);
        return true;
    }

    public void Tick(long nowTick)
    {
        if (_pending.Count > 0)
        {
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                var pc = _pending[i];
                if (pc.ResolveAt > nowTick) continue;

                _pending.RemoveAt(i);
                var src = _entities.Get(pc.Source);
                var tgt = _entities.Get(pc.Target);
                if (src != null && tgt != null && IsAlive(src) && IsAlive(tgt))
                {
                    ResolveSkill(src, tgt, pc.SkillId, pc.Level);
                }
            }
        }

        // T4.9g — deferred ground casts use the same expiry sweep.
        if (_pendingPos.Count > 0)
        {
            for (var i = _pendingPos.Count - 1; i >= 0; i--)
            {
                var pc = _pendingPos[i];
                if (pc.ResolveAt > nowTick) continue;

                _pendingPos.RemoveAt(i);
                var src = _entities.Get(pc.Source);
                if (src != null && IsAlive(src))
                {
                    ResolveSkillAt(src, pc.X, pc.Y, pc.SkillId, pc.Level);
                }
            }
        }
    }

    // Per-DamageKind resolution lives in ISkillResolver implementations
    // under Skills/Resolvers/. SkillResolverRegistry dispatches.

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => true,
    };

    /// <inheritdoc />
    public bool CancelCast(EntityId entityId)
    {
        var dropped = _pending.RemoveAll(p => p.Source == entityId)
                    + _pendingPos.RemoveAll(p => p.Source == entityId);
        if (dropped == 0) return false;
        // rAthena clif_skillcastcancel(src) broadcasts the bar abort —
        // _client.BroadcastSkillCasting with castTime=0 is the equivalent.
        // The cancelling caller usually emits the same packet itself, so
        // we don't double-broadcast here.
        return true;
    }

    /// <inheritdoc />
    public bool IsCasting(EntityId entityId)
        => _pending.Any(p => p.Source == entityId) || _pendingPos.Any(p => p.Source == entityId);

    /// <inheritdoc />
    public (ushort skillId, ushort skillLevel) GetCurrentCast(EntityId entityId)
    {
        var p = _pending.Find(x => x.Source == entityId);
        if (p != null) return (p.SkillId, p.Level);
        var pp = _pendingPos.Find(x => x.Source == entityId);
        if (pp != null) return (pp.SkillId, pp.Level);
        return ((ushort)0, (ushort)0);
    }

    private sealed class PendingCast
    {
        public EntityId Source;
        public EntityId Target;
        public ushort SkillId;
        public ushort Level;
        public long ResolveAt;
    }

    private sealed class PendingPosCast
    {
        public EntityId Source;
        public short X;
        public short Y;
        public ushort SkillId;
        public ushort Level;
        public long ResolveAt;
    }
}
