using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_JUMPKICK — Jump Kick. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/jumpkick.cpp</c>.
/// Ratio <c>+(-70 + 10*lv)</c> normally; Tumble/Running variants
/// (+4%*baseLv, or 8% under SC_SPURT) are TODO. Caster teleport to
/// target and soul-link dispel are TODO.
/// </summary>
public sealed class JumpKick : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public JumpKick() : base(SkillIds.TK_JUMPKICK) { }

    public JumpKick(ISkillAttackService? skillAttack = null) : base(SkillIds.TK_JUMPKICK)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-70 + 10 * skillLevel);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
