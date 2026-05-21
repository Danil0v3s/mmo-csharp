using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_KAGEAKUMU — Shadow Nightmare. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadownightmare.cpp</c>.
/// Splash damage; ratio <c>+(-100 + 18000) + 5*pow</c>, +50% extra
/// when target has SC_NIGHTMARE. Ends SC_NIGHTMARE on hit.
/// </summary>
public sealed class ShadowNightmare : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public ShadowNightmare() : base(SkillIds.SS_KAGEAKUMU) { }

    public ShadowNightmare(ISkillAttackService? skillAttack = null) : base(SkillIds.SS_KAGEAKUMU)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 18000) + 5 * src.Stats.Pow;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.End(target, StatusType.Nightmare);
}
