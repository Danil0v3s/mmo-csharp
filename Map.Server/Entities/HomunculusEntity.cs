namespace Map.Server.Entities;

/// <summary>
/// FEATURE-08 — live homunculus battle unit. Per-master companion (one at a time, bound via
/// <see cref="Entity.MasterId"/>), spawned into the spatial registry + visibility so it has a
/// position, an HP bar, and can be seen by the AOI — mirroring <see cref="ElementalEntity"/> /
/// <see cref="PetEntity"/>.
///
/// <para>The non-spatial bookkeeping (intimacy, hunger, exp, skill tree, evolution) stays on the
/// <c>HomunculusService</c>'s per-master record; this entity is the in-world mirror. The persisted
/// shape is <see cref="Core.Database.Entities.HomunculusEntity"/>; gRPC pushes the hydrated row to the
/// map, which constructs this entity in <c>HomunculusService.RecvData</c> / <c>Call</c>.</para>
/// </summary>
public sealed class HomunculusEntity : Entity
{
    /// <summary>Persistent homunculus id (PK on the <c>homunculus</c> table). 0 = unsaved.</summary>
    public int HomunculusId { get; init; }

    /// <summary>rAthena <c>hd-&gt;homunculus.class_</c> — the homun class id (Lif/Amistr/Filir/...).</summary>
    public int ClassId { get; set; }

    /// <summary>Display name.</summary>
    public string HomName { get; set; } = string.Empty;

    /// <summary>Current HP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int Hp { get => Stats.Hp; set => Stats.Hp = value; }

    /// <summary>Maximum HP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int MaxHp { get => Stats.MaxHp; set => Stats.MaxHp = value; }

    /// <summary>Current SP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int Sp { get => Stats.Sp; set => Stats.Sp = value; }

    /// <summary>Maximum SP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int MaxSp { get => Stats.MaxSp; set => Stats.MaxSp = value; }

    /// <summary>The homun's combat target (0 = idle). Driven by the summon AI — FEATURE-29.</summary>
    public int TargetId { get; set; }

    public override EntityType Type => EntityType.Homunculus;

    public HomunculusEntity(EntityId id, int homunculusId, int classId, EntityId masterId,
        uint mapId, short x, short y)
        : base(id, mapId, x, y)
    {
        HomunculusId = homunculusId;
        ClassId = classId;
        MasterId = masterId;
    }
}
