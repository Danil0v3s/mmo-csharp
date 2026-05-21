using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_TINDER_BREAKER — Homunculus Tinder Breaker. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_tinderbreaker.cpp</c>.
/// Applies SC_TINDER_BREAKER2 for max(lv, STRsrc/7 − STRtgt/10) seconds
/// and lands a hit. Move + knockback are TODO.
/// </summary>
public sealed class TinderBreaker : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public TinderBreaker() : base(SkillIds.MH_TINDER_BREAKER) { }

    public TinderBreaker(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_TINDER_BREAKER)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var seconds = Math.Max(skillLevel, src.Stats.Str / 7 - target.Stats.Str / 10);
        ctx.Sc?.Start(target, StatusType.TinderBreaker2, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: seconds * 1_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }
}
