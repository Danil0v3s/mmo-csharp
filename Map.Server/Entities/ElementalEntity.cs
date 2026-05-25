namespace Map.Server.Entities;

/// <summary>
/// Live Sorcerer / Elemental Master companion. Per-master summon
/// (one elemental at a time, bound via <see cref="Entity.MasterId"/>),
/// not registered against any global by-id table.
///
/// Mirrors rAthena <c>struct s_elemental_data</c> (elemental.hpp:75)
/// — only the runtime fields the C# port actually reads. The
/// persisted shape lives in <see cref="Core.Database.Entities.ElementalEntity"/>;
/// gRPC pushes its hydrated form (<see cref="Core.Server.IPC.ElementalData"/>)
/// to the map server on load, which then constructs this entity via
/// <see cref="Elemental.ElementalService.DataReceived"/>.
/// </summary>
public sealed class ElementalEntity : Entity
{
    /// <summary>Persistent elemental id (PK on <c>elemental</c> table). 0 = unsaved.</summary>
    public int ElementalId { get; init; }

    /// <summary>rAthena <c>elemental.class_</c> — Aegis class id (Agni/Aqua/Ventus/Tera tiers + EM elementals).</summary>
    public int ClassId { get; init; }

    /// <summary>
    /// rAthena <c>battle_status.mode</c> — one of EL_MODE_PASSIVE (1),
    /// EL_MODE_ASSIST (1+MD_ASSIST), or EL_MODE_AGGRESSIVE
    /// (MD_CANMOVE|MD_AGGRESSIVE|MD_CANATTACK). Drives AI behavior in
    /// <see cref="Elemental.ElementalService.Action"/>.
    /// </summary>
    public int Mode { get; set; }

    /// <summary>Current HP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int Hp
    {
        get => Stats.Hp;
        set => Stats.Hp = value;
    }

    /// <summary>Maximum HP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int MaxHp
    {
        get => Stats.MaxHp;
        set => Stats.MaxHp = value;
    }

    /// <summary>Current SP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int Sp
    {
        get => Stats.Sp;
        set => Stats.Sp = value;
    }

    /// <summary>Maximum SP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int MaxSp
    {
        get => Stats.MaxSp;
        set => Stats.MaxSp = value;
    }

    /// <summary>rAthena <c>battle_status.rhw.atk</c>.</summary>
    public int Attack { get; set; }
    /// <summary>rAthena <c>battle_status.rhw.atk2</c>.</summary>
    public int Attack2 { get; set; }
    /// <summary>rAthena <c>battle_status.matk_min</c>.</summary>
    public int Matk { get; set; }
    /// <summary>rAthena <c>battle_status.def</c>.</summary>
    public int Def { get; set; }
    /// <summary>rAthena <c>battle_status.mdef</c>.</summary>
    public int Mdef { get; set; }
    /// <summary>rAthena <c>battle_status.flee</c>.</summary>
    public int Flee { get; set; }
    /// <summary>rAthena <c>battle_status.hit</c>.</summary>
    public int Hit { get; set; }
    /// <summary>rAthena <c>battle_status.amotion</c> (aspd).</summary>
    public int Aspd { get; set; }

    /// <summary>
    /// rAthena <c>ed-&gt;target_id</c>. The entity this elemental is
    /// engaged with right now. 0 = no current target. Mutated by
    /// <c>elemental_set_target</c> / <c>elemental_unlocktarget</c>.
    /// </summary>
    public int TargetId { get; set; }

    /// <summary>
    /// Absolute tick (<see cref="Environment.TickCount64"/>) at which
    /// the summon expires. Mirrors rAthena
    /// <c>ed-&gt;summon_timer</c>'s scheduled fire tick — when the
    /// AI loop sees we've crossed it, the elemental is deleted
    /// (<c>elemental_summon_end</c>).
    /// </summary>
    public long SummonExpiresAtTick { get; set; }

    /// <summary>
    /// rAthena <c>ed-&gt;last_thinktime</c> — tick of last AI think.
    /// Action tick is gated by MIN_ELETHINKTIME (100 ms) past this.
    /// </summary>
    public long LastThinkTick { get; set; }

    /// <summary>
    /// rAthena <c>ed-&gt;last_spdrain_time</c> — tick of last
    /// master-SP drain (sustain check, every 10 s).
    /// </summary>
    public long LastSpDrainTick { get; set; }

    public override EntityType Type => EntityType.Elemental;

    public ElementalEntity(EntityId id, int elementalId, int classId, EntityId masterId, uint mapId, short x, short y)
        : base(id, mapId, x, y)
    {
        ElementalId = elementalId;
        ClassId = classId;
        MasterId = masterId;
    }
}
