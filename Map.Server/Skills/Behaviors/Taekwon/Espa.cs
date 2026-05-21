using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SP_SPA — Espa. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/espa.cpp</c>.
/// Magic single hit; ratio <c>+(400 + 250*lv)</c>. Counter chain
/// applies SC_USE_SKILL_SP_SPA on caster (TODO).
/// </summary>
public sealed class Espa : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public Espa() : base(SkillIds.SP_SPA) { }

    public Espa(ISkillAttackService? skillAttack = null) : base(SkillIds.SP_SPA)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400 + 250 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
