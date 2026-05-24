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

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio;
        if (src is PlayerEntity sd && sd.SpiritCharmType != 0 && sd.SpiritCharm > 0)
        {
            ratio += (-100 + 200 * sd.SpiritCharm);
            // rAthena RE_LVL_DMOD(100) — multiply final ratio by
            // (caster base level / 100). Implements the renewal-tier
            // damage scaling.
            ratio = ratio * Math.Max(1, sd.Level) / 100;
        }
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        // rAthena pc_delspiritcharm: consume every active charm after
        // the cast (the damage formula already read SpiritCharm above).
        if (src is PlayerEntity sd && sd.SpiritCharm > 0)
        {
            ctx.Orbs?.RemoveCharms(sd);
        }
    }
}
