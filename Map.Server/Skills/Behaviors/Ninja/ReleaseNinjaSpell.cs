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
        if (src is PlayerEntity sd && sd.SpiritCharmType != 0 && sd.SpiritCharm > 0)
            return baseRatio + (-100 + 200 * sd.SpiritCharm);
        // Deferred: RE_LVL_DMOD(100) scale by caster base level (no
        // CalculateSkillRatio-side helper plumbed yet) and pc_delspiritcharm
        // consumption (no IPlayerOrbService access in this signature; charms
        // must currently be cleared elsewhere or left to natural expiry).
        return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
