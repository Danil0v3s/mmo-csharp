using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_KAIHOU — Release Ninja Spell. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/releaseninjaspell.cpp</c>.
/// Magic hit; ratio <c>+(-100 + 200*spiritCharm)</c> when charms are
/// present. Charm consumption is TODO — needs IPlayerOrbsService.
/// </summary>
public sealed class ReleaseNinjaSpell : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public ReleaseNinjaSpell() : base(SkillIds.KO_KAIHOU) { }

    public ReleaseNinjaSpell(ISkillAttackService? skillAttack = null) : base(SkillIds.KO_KAIHOU)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // TODO: scale by sd->spiritcharm + RE_LVL_DMOD(100) + pc_delspiritcharm.
        return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
