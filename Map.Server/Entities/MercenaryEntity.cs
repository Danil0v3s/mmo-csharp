namespace Map.Server.Entities;

/// <summary>
/// FEATURE-09 — live mercenary battle unit. A contract-bound, per-master companion (one at a time,
/// bound via <see cref="Entity.MasterId"/>), spawned into the spatial registry + visibility so it has
/// a position, an HP/lifetime bar, and is visible to the AOI — mirroring <see cref="HomunculusEntity"/>
/// / <see cref="ElementalEntity"/>.
///
/// <para>Non-spatial bookkeeping (faith, kill count, contract end, skills) stays on the
/// <c>MercenaryService</c>'s per-master record; this entity is the in-world mirror. The persisted shape
/// is <see cref="Core.Server.IPC.MercenaryData"/>, projected by <c>MercenaryService.SerializeSnapshot</c>.</para>
/// </summary>
public sealed class MercenaryEntity : Entity
{
    /// <summary>Persistent mercenary id (char-assigned). 0 = unsaved.</summary>
    public int MercenaryId { get; init; }

    /// <summary>rAthena <c>md-&gt;mercenary.class_</c> — the merc class id.</summary>
    public int ClassId { get; init; }

    /// <summary>Contract end (<see cref="Environment.TickCount64"/> units); the merc despawns at it.</summary>
    public long ContractEndTick { get; set; }

    public int Hp { get => Stats.Hp; set => Stats.Hp = value; }
    public int MaxHp { get => Stats.MaxHp; set => Stats.MaxHp = value; }
    public int Sp { get => Stats.Sp; set => Stats.Sp = value; }
    public int MaxSp { get => Stats.MaxSp; set => Stats.MaxSp = value; }

    /// <summary>The merc's combat target (0 = idle). Driven by the summon AI — FEATURE-32.</summary>
    public int TargetId { get; set; }

    public override EntityType Type => EntityType.Mercenary;

    public MercenaryEntity(EntityId id, int mercenaryId, int classId, EntityId masterId,
        uint mapId, short x, short y)
        : base(id, mapId, x, y)
    {
        MercenaryId = mercenaryId;
        ClassId = classId;
        MasterId = masterId;
    }
}
