using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_ALL_BLOOM — Arch Mage All Bloom. Manual port of
/// <c>rathena-fork/src/map/skills/mage/allbloom.cpp</c>.
///
/// <para>Drops a Flower Garden ground unit then spawns rose buds at
/// random splash cells, staggered by the unit interval. SC_CLIMAX
/// modulates the behavior (lv 1: 2× tick speed; lv 2: double bud per
/// stagger; lv 4: SC inflict instead of damage; lv 5: extra
/// finishing-hit unit). Climax SC readback + repeated unit-spawning
/// helpers aren't wired here — for now we drop the single garden
/// unit.</para>
/// </summary>
public sealed class AllBloom : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public AllBloom() : base(SkillIds.AG_ALL_BLOOM) { }

    public AllBloom(ISkillUnitService? units = null) : base(SkillIds.AG_ALL_BLOOM)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _units?.Place(src, SkillId, skillLevel, x, y);
        // Deferred: rose-bud staggered spawns at random splash cells + SC_CLIMAX
        // modulation (lv 1 2× speed / lv 2 double-bud / lv 4 SC-inflict / lv 5 finisher) —
        // needs the repeated-Place + Climax SC readback helpers.
    }
}
