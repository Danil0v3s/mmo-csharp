using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_WATER_SCREW — Elemental Water Screw. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/waterscrew.cpp</c>.
/// +900 ratio; 30% splash, else single hit. EL_WATER_SCREW_ATK splash
/// variant lives in the same source file (+900).
/// </summary>
public sealed class WaterScrew : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public WaterScrew() : base(SkillIds.EL_WATER_SCREW) { }

    public WaterScrew(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_WATER_SCREW)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 900;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // TODO: 30% splash via EL_WATER_SCREW_ATK — deferred.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
