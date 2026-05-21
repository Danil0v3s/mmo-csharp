using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_ICE_NEEDLE — Elemental Ice Needle. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/iceneedle.cpp</c>.
/// Single-target magic hit; +400 ratio.
/// </summary>
public sealed class IceNeedle : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public IceNeedle() : base(SkillIds.EL_ICE_NEEDLE) { }

    public IceNeedle(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_ICE_NEEDLE)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
