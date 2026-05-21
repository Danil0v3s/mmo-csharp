using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_TURNKICK — Roundhouse Kick. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/turnkick.cpp</c>.
/// +90 + 30*lv ratio; splash stuns at 2% + NoAction. Splash iteration
/// is TODO.
/// </summary>
public sealed class TurnKick : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public TurnKick() : base(SkillIds.TK_TURNKICK) { }

    public TurnKick(ISkillAttackService? skillAttack = null) : base(SkillIds.TK_TURNKICK)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 90 + 30 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 2)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 3_000, src);
    }
}
