using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_FIREPILLAR — Wizard Fire Pillar. Manual port of
/// <c>rathena-fork/src/map/skills/mage/firepillar.cpp</c>.
///
/// <para>Drops the Fire Pillar ground unit. Per-tick MATK ratio:
/// <c>+(-60 + 20*lv)</c>. Player casters split damage across hits
/// (negative div_ on rAthena); this nuance is deferred until BattleDamage
/// gains the signed-div field. Walk-delay slow on hit victims is also
/// deferred.</para>
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

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: players split MATK across hits (dmg.div_ *= -1). Signed-div TODO.
    }
}
