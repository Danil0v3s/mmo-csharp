using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_CHARGEARROW — Mercenary Arrow Repel. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_arrowrepel.cpp</c>.
/// Ratio <c>+50</c>.
/// </summary>
public sealed class MercenaryArrowRepel : WeaponSkillImpl
{
    public MercenaryArrowRepel() : base(SkillIds.MA_CHARGEARROW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50;
}
