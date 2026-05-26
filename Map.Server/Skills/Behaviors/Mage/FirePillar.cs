using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_FIREPILLAR — Wizard Fire Pillar. Manual port of
/// <c>rathena-fork/src/map/skills/mage/firepillar.cpp</c>.
///
/// <para>Drops the Fire Pillar ground unit. Per-tick MATK ratio:
/// <c>+(-60 + 20*lv)</c>.</para>
///
/// <para>INFRA-DEFERRED: player casters split MATK across hits via
/// rAthena's <c>dmg.div_ *= -1</c> (a negative div_ tells the renderer
/// to fan the total across visual hits). The C# <see cref="BattleDamage"/>
/// uses an unsigned <c>Hits</c> field; the sign-encoded "split across
/// hits" semantic needs a new field on the struct.</para>
/// </summary>
public sealed class FirePillar : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public FirePillar() : base(SkillIds.WZ_FIREPILLAR) { }

    public FirePillar(ISkillUnitService? units = null) : base(SkillIds.WZ_FIREPILLAR)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -60 + 20*lv (20% MATK per hit).
        return baseRatio + (-60 + 20 * skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
