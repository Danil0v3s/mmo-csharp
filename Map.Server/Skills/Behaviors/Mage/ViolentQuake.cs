using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_VIOLENT_QUAKE — Arch Mage Violent Quake. Manual port of
/// <c>rathena-fork/src/map/skills/mage/violentquake.cpp</c>.
///
/// <para>Drops an Earthquake ground unit + staggered rising-rock
/// sub-units. SC_CLIMAX modifies behavior (lv 1: 2× rock spawn; lv 4:
/// SC inflict instead of damage; lv 5: 7×7 spawn area). The
/// per-stagger sub-unit spawn isn't wired here; we drop the primary
/// quake unit.</para>
/// </summary>
public sealed class ViolentQuake : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ViolentQuake() : base(SkillIds.AG_VIOLENT_QUAKE) { }

    public ViolentQuake(ISkillUnitService? units = null) : base(SkillIds.AG_VIOLENT_QUAKE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _units?.Place(src, SkillId, skillLevel, x, y);
        // Deferred: staggered AG_VIOLENT_QUAKE_ATK sub-unit spawns + SC_CLIMAX modes
        // (lv 1 2× rock spawn / lv 4 SC-inflict / lv 5 7×7 area) — needs repeated-Place
        // helper and Climax SC readback.
    }
}
