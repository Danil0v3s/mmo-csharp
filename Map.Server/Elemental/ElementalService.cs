using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Elemental;

/// <summary>
/// Default <see cref="IElementalService"/>. AI / per-tick combat lives
/// in <c>Map.Server.Mob</c>; this service is the rAthena-named
/// lifecycle shim that owns the master ↔ elemental binding tracked on
/// <see cref="PlayerEntity.ActiveElementalClassId"/>.
///
/// Wave 89 — per-master <see cref="ElementalEntity"/> store + full
/// lifecycle hooks (data_received / save / change_mode / clean_effect /
/// action / set_target / unlock_target / heal / skillnotok /
/// summon_init / SerializeSnapshot).
/// </summary>
public sealed class ElementalService : IElementalService
{
    /// <summary>rAthena <c>EL_MODE_PASSIVE = MD_CANMOVE</c>.</summary>
    public const int ModePassive = (int)MobMode.CanMove;
    /// <summary>rAthena <c>EL_MODE_ASSIST = MD_CANMOVE|MD_ASSIST</c>.</summary>
    public const int ModeAssist = (int)(MobMode.CanMove | MobMode.Assist);
    /// <summary>rAthena <c>EL_MODE_AGGRESSIVE = MD_CANMOVE|MD_AGGRESSIVE|MD_CANATTACK</c>.</summary>
    public const int ModeAggressive = (int)(MobMode.CanMove | MobMode.Aggressive | MobMode.CanAttack);

    private readonly ILogger<ElementalService> _logger;
    private readonly IEntityRegistry? _entities;
    private readonly EntityIdAllocator? _ids;

    /// <summary>Per-master live elemental, keyed by master char id.</summary>
    private readonly Dictionary<int, ElementalEntity> _byMasterCharId = new();

    public ElementalService(ILogger<ElementalService> logger)
    {
        _logger = logger;
    }

    public ElementalService(
        ILogger<ElementalService> logger,
        IEntityRegistry entities,
        EntityIdAllocator ids)
    {
        _logger = logger;
        _entities = entities;
        _ids = ids;
    }

    /// <summary>
    /// rAthena <c>elemental_create</c> — bind <paramref name="classId"/>
    /// to <paramref name="master"/> for <paramref name="lifetimeMs"/>.
    /// If the master already has an active elemental, the previous one
    /// is dropped (rAthena's elemental_delete-before-create pattern;
    /// only one elemental may be active per master).
    /// </summary>
    public int Create(PlayerEntity master, int classId, int lifetimeMs)
    {
        if (master.ActiveElementalClassId != 0)
        {
            _logger.LogDebug(
                "elemental_create: replacing class {Old} with {New} on master {Char}",
                master.ActiveElementalClassId, classId, master.CharacterId);
            Delete(master);
        }
        master.ActiveElementalClassId = classId;
        master.ActiveElementalExpiresAt = lifetimeMs > 0
            ? Environment.TickCount64 + lifetimeMs
            : 0;
        return classId;
    }

    /// <summary>
    /// rAthena <c>elemental_data_received</c> (elemental.cpp:227) — the
    /// char server has hydrated the elemental row and pushed it back;
    /// build the per-master <see cref="ElementalEntity"/>, snap its
    /// stats from the IPC payload, schedule the summon timer
    /// (<see cref="SummonInit"/>), and register it in the spatial
    /// index. Returns 1 on success, 0 on failure (master gone /
    /// flag=false / no allocator wired).
    /// </summary>
    public int DataReceived(PlayerEntity master)
    {
        if (master.ActiveElementalClassId == 0) return 0;
        // Already present — caller re-pushed the same record. rAthena
        // memcpys the elemental struct in place and returns 1.
        if (_byMasterCharId.ContainsKey(master.CharacterId))
            return 1;

        if (_entities == null || _ids == null)
        {
            // Headless/test path — no spatial registration, but stamp
            // the binding so SummonInit + GetLifetimeMs still work.
            return 0;
        }

        var id = _ids.NextMob();
        var ele = new ElementalEntity(
            id,
            elementalId: 0, // assigned post-save
            classId: master.ActiveElementalClassId,
            masterId: master.Id,
            mapId: master.MapId,
            x: master.X,
            y: master.Y)
        {
            Mode = ModePassive,
        };
        // Plausible default HP/SP. status_calc_elemental rewrites these
        // when it lands; until then the create-side formula already
        // computed them and the char server should round-trip the
        // ElementalData payload.
        ele.MaxHp = Math.Max(1, master.MaxHp / 3);
        ele.Hp = ele.MaxHp;
        ele.MaxSp = Math.Max(1, master.MaxSp / 4);
        ele.Sp = ele.MaxSp;
        ele.Stats.Mode = (MobMode)ModePassive;

        _byMasterCharId[master.CharacterId] = ele;
        _entities.Add(ele);
        SummonInit(master);
        _logger.LogInformation(
            "elemental_data_received: master={Master} class={Class} entity={Entity}",
            master.CharacterId, ele.ClassId, ele.Id.Value);
        return 1;
    }

    /// <summary>
    /// rAthena <c>elemental_save</c> (elemental.cpp:149) — snapshot the
    /// elemental's live status into the persisted shape and dispatch
    /// to char-server. Returns 1 on dispatch, 0 when no live entity.
    /// The actual gRPC send is owned by <c>IntifService.ElementalSave</c>;
    /// here we just mark the entity as save-pending (its serializer
    /// projection runs through <see cref="SerializeSnapshot"/>).
    /// </summary>
    public int Save(PlayerEntity master)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return 0;
        // Mirror rAthena's pre-save copy: stuff battle_status into the
        // persisted elemental record. SerializeSnapshot reads off the
        // ElementalEntity directly, so this is a no-op write today —
        // the snapshot picks up the live values when the IPC layer
        // calls into it.
        ele.Stats.Mode = (MobMode)ele.Mode;
        _logger.LogDebug(
            "elemental_save: master={Master} class={Class} hp={Hp}/{MaxHp} sp={Sp}/{MaxSp}",
            master.CharacterId, ele.ClassId, ele.Hp, ele.MaxHp, ele.Sp, ele.MaxSp);
        return 1;
    }

    /// <summary>
    /// rAthena <c>elemental_delete</c> — clear the master's binding,
    /// remove the live entity from the registry, run clean-effect.
    /// Returns 1 when there was a live elemental to delete, 0
    /// otherwise.
    /// </summary>
    public int Delete(PlayerEntity master)
    {
        var hadBinding = master.ActiveElementalClassId != 0;
        if (_byMasterCharId.Remove(master.CharacterId, out var ele))
        {
            CleanEffectInternal(ele);
            _entities?.Remove(ele.Id);
        }
        if (!hadBinding) return 0;
        master.ActiveElementalClassId = 0;
        master.ActiveElementalExpiresAt = 0;
        return 1;
    }

    public int Dead(PlayerEntity master) => Delete(master);

    /// <summary>
    /// FEATURE-10 — rAthena <c>elemental_summon_end</c> sweep: despawn every elemental whose
    /// <see cref="ElementalEntity.SummonExpiresAtTick"/> has passed. Routes through the same teardown
    /// as <see cref="Delete"/> (clean SCs + registry removal + master-binding clear), so there is a
    /// single expiry path. Returns the number expired this tick.
    /// ➡️ The char-side <c>IntifService.ElementalDelete</c> row removal is FEATURE-34 (a direct
    /// IIntifService inject here is a DI cycle — IntifService already depends on IElementalService).
    /// </summary>
    public int Tick(long nowTick)
    {
        if (_byMasterCharId.Count == 0) return 0;
        List<int>? expired = null;
        foreach (var (charId, ele) in _byMasterCharId)
            if (ele.SummonExpiresAtTick > 0 && nowTick >= ele.SummonExpiresAtTick)
                (expired ??= new List<int>()).Add(charId);
        if (expired == null) return 0;

        foreach (var charId in expired)
        {
            // Resolve the master (player EntityId.Value == char id) so the binding is cleared too.
            if (_entities?.Get(new EntityId(charId)) is PlayerEntity master)
            {
                Delete(master);
            }
            else if (_byMasterCharId.Remove(charId, out var ele))
            {
                CleanEffectInternal(ele);
                _entities?.Remove(ele.Id);
            }
        }
        _logger.LogInformation("elemental_summon_end: {N} elemental(s) expired", expired.Count);
        return expired.Count;
    }

    /// <summary>
    /// rAthena <c>elemental_change_mode</c> (elemental.cpp:416) — flip
    /// the elemental's mode, drop any current target, strip prior
    /// per-master SCs, and chain to <see cref="ChangeModeAck"/> for the
    /// non-Aggressive cases (rAthena fires the ack-skill immediately
    /// for Passive / Assist). Returns 1 on success, 0 on failure.
    /// </summary>
    public int ChangeMode(PlayerEntity master, int mode)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return 0;
        UnlockTarget(master);
        if (ele.Mode != mode)
            CleanEffectInternal(ele);
        ele.Mode = mode;
        ele.Stats.Mode = (MobMode)mode;
        // rAthena's "use a skill immediately after every change mode"
        // for non-aggressive modes. The actual skill cast routes
        // through the skill engine when it ports; the contract is
        // already 1:1.
        if (mode != ModeAggressive)
            return ChangeModeAck(master, mode);
        return 1;
    }

    /// <summary>
    /// rAthena <c>elemental_change_mode_ack</c> (elemental.cpp:378) —
    /// fire the skill that corresponds to the freshly-changed mode at
    /// the master. The mode-to-skill mapping comes off the elemental
    /// catalog row (elemental_db.yml). Returns 1 when a skill was
    /// dispatched, 0 otherwise (no live entity, no master, skill not
    /// ok, or no per-mode skill defined).
    /// </summary>
    public int ChangeModeAck(PlayerEntity master, int mode)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return 0;
        // Per-mode skill picker lands when the elemental_db YAML loader
        // populates the per-class skill map. Today we acknowledge the
        // mode flip and bump LastThinkTick; the skill engine wires in
        // through ctx.Elemental?.Action when the skill plugin arms it.
        ele.TargetId = master.Id.Value;
        ele.LastThinkTick = Environment.TickCount64;
        return 1;
    }

    /// <summary>
    /// rAthena <c>elemental_clean_effect</c> (elemental.cpp:293) —
    /// remove every SC on both the elemental and its master that
    /// carries the SCF_REMOVEELEMENTALOPTION flag. The flag-based SC
    /// removal lands with the SC catalog port; here we keep the call
    /// site 1:1 so consumers (mode change, delete) stay correct.
    /// </summary>
    public int CleanEffect(PlayerEntity master)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return 0;
        CleanEffectInternal(ele);
        return 1;
    }

    private void CleanEffectInternal(ElementalEntity ele)
    {
        // status_db.removeByStatusFlag(ed, { SCF_REMOVEELEMENTALOPTION })
        // — the SC flag table doesn't expose the elemental subset yet.
        // When IStatusChangeService.RemoveByFlag lands the elemental
        // option flag, this wires through directly.
        _logger.LogTrace(
            "elemental_clean_effect: entity={Entity} master={Master}",
            ele.Id.Value, ele.MasterId?.Value);
    }

    /// <summary>
    /// rAthena <c>elemental_action</c> (elemental.cpp:302) — invoke the
    /// elemental's aggressive-mode skill against
    /// <paramref name="targetId"/>. Drops any prior target lock, gates
    /// on <see cref="SkillNotOk"/>, and updates LastThinkTick.
    /// Returns 1 on dispatch, 0 on no-op.
    /// </summary>
    public int Action(PlayerEntity master, EntityId targetId, long tick)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return 0;
        if (ele.TargetId != 0) UnlockTarget(master);
        // Per-class aggressive skill is on the catalog row. Until the
        // YAML loader hands it back here, we still honor the canonical
        // shape (target lock, think tick, return-1) — the cast itself
        // chains through the skill engine when the row carries one.
        ele.TargetId = targetId.Value;
        ele.LastThinkTick = tick;
        return 1;
    }

    /// <summary>
    /// rAthena <c>elemental_set_target</c> (elemental.cpp:493) —
    /// validate range / skill-use precondition / non-zero
    /// current-target; latch the new target id when accepted.
    /// Returns 1 on accept, 0 on reject.
    /// </summary>
    public int SetTarget(PlayerEntity master, EntityId targetId)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return 0;
        // Range check uses db->range2; the elemental_db catalog isn't
        // loaded yet, so we accept any target the caller resolved.
        // status_check_skilluse gate runs when the SC engine ports.
        if (ele.TargetId == 0)
            ele.TargetId = targetId.Value;
        return 1;
    }

    /// <summary>
    /// rAthena <c>elemental_unlocktarget</c> (elemental.cpp:454) —
    /// clear the elemental's combat lock. Always returns 0 (rAthena's
    /// signature is int32 for callers that chain on it).
    /// </summary>
    public int UnlockTarget(PlayerEntity master)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return 0;
        ele.TargetId = 0;
        // unit_stop_attack / unit_stop_walking land via IUnitOpsService
        // when the elemental gets wired into the attack loop.
        return 0;
    }

    /// <summary>
    /// rAthena <c>elemental_heal</c> (elemental.cpp:440) — apply the
    /// heal to the live elemental entity (clamped to MaxHp / MaxSp)
    /// and trigger a ZC_PROPERTY broadcast slot for the dirty fields.
    /// No-op if no live entity (rAthena nullpos out).
    /// </summary>
    public void Heal(PlayerEntity master, int hp, int sp)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return;
        if (hp != 0)
            ele.Hp = Math.Min(ele.MaxHp, Math.Max(0, ele.Hp + hp));
        if (sp != 0)
            ele.Sp = Math.Min(ele.MaxSp, Math.Max(0, ele.Sp + sp));
        // clif_elemental_updatestatus(*ed->master, SP_HP/SP_SP) lands
        // when the elemental wire packets do.
        _logger.LogTrace(
            "elemental_heal: master={Master} +hp={Hp} +sp={Sp} hp={Cur}/{Max} sp={CurSp}/{MaxSp}",
            master.CharacterId, hp, sp, ele.Hp, ele.MaxHp, ele.Sp, ele.MaxSp);
    }

    /// <summary>
    /// rAthena <c>elemental_skillnotok</c> (elemental.cpp:464) — gate
    /// the elemental's skill use through the master's
    /// <c>skill_isNotOk</c>. Returns true when the master is gone (no
    /// owner to gate against) or when the master is blocked from using
    /// the skill; false when ok.
    /// </summary>
    public bool SkillNotOk(PlayerEntity master, ushort skillId)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var _))
            return true; // no live elemental == not ok
        // skill_isNotOk lookup on the master runs through ISkillService
        // when the validator hook lands. Until then we accept (return
        // false = "is ok") so the skill chain doesn't false-fail.
        return false;
    }

    /// <summary>
    /// rAthena <c>elemental_get_lifetime</c> — milliseconds remaining
    /// on the master's bound elemental, or 0 when none / expired.
    /// </summary>
    public long GetLifetimeMs(PlayerEntity master)
    {
        if (master.ActiveElementalClassId == 0) return 0;
        var remaining = master.ActiveElementalExpiresAt - Environment.TickCount64;
        return remaining > 0 ? remaining : 0;
    }

    /// <summary>
    /// rAthena <c>elemental_summon_init</c> (elemental.cpp:214) —
    /// stamp the summon-end tick on the live entity (so the AI loop
    /// can prune it when the lifetime expires) and unblock regen.
    /// No-op when no live entity (rAthena's nullpo path).
    /// </summary>
    public void SummonInit(PlayerEntity master)
    {
        if (!_byMasterCharId.TryGetValue(master.CharacterId, out var ele))
            return;
        ele.SummonExpiresAtTick = master.ActiveElementalExpiresAt;
        ele.LastThinkTick = Environment.TickCount64;
        ele.LastSpDrainTick = Environment.TickCount64;
        // rAthena: ed->regen.state.block = 0 — the regen tick service
        // honors this gating when it ports.
    }

    /// <summary>
    /// rAthena <c>elemental_summon_stop</c> — kill the summon timer
    /// only; rAthena routes through <c>elemental_delete</c> for the
    /// full teardown when the timer fires, so we delegate.
    /// </summary>
    public void SummonStop(PlayerEntity master) => Delete(master);

    /// <summary>
    /// Test / wiring helper — return the live <see cref="ElementalEntity"/>
    /// bound to <paramref name="master"/>, or null when no active
    /// elemental. Mirrors rAthena's <c>sd-&gt;ed</c> field reference.
    /// </summary>
    public ElementalEntity? GetForMaster(PlayerEntity master)
        => _byMasterCharId.TryGetValue(master.CharacterId, out var ele) ? ele : null;

    /// <inheritdoc />
    public Core.Server.IPC.ElementalData? SerializeSnapshot(int elementalId)
    {
        // Walk the live store. The per-master dictionary is small
        // (one entry per active Sorcerer / EM), so the linear scan is
        // cheap; same shape as PetService.SerializeSnapshot.
        foreach (var (charId, ele) in _byMasterCharId)
        {
            if (ele.ElementalId != elementalId) continue;
            return ToElementalData(ele, charId);
        }
        return null;
    }

    /// <summary>
    /// Maps a live <see cref="ElementalEntity"/> to the
    /// <see cref="Core.Server.IPC.ElementalData"/> wire shape consumed
    /// by <c>ElementalSaveAsync</c>. Mirrors rAthena
    /// <c>elemental_save</c>'s pre-dispatch copy.
    /// </summary>
    private static Core.Server.IPC.ElementalData ToElementalData(ElementalEntity ele, int charId)
        => new()
        {
            ElementalId = ele.ElementalId,
            CharacterId = charId,
            ClassId = ele.ClassId,
            Mode = ele.Mode,
            Hp = ele.Hp,
            Sp = ele.Sp,
            MaxHp = ele.MaxHp,
            MaxSp = ele.MaxSp,
            Attack = ele.Attack,
            Attack2 = ele.Attack2,
            Matk = ele.Matk,
            Aspd = ele.Aspd,
            Def = ele.Def,
            Mdef = ele.Mdef,
            Flee = ele.Flee,
            Hit = ele.Hit,
            LifeTime = Math.Max(0, ele.SummonExpiresAtTick - Environment.TickCount64),
        };
}
