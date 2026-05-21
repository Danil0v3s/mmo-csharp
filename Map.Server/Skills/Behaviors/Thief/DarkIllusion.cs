using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_DARKILLUSION — Dark Illusion. Manual port of
/// <c>rathena-fork/src/map/skills/thief/darkillusion.cpp</c>.
/// Caster teleports behind target, hits, and at 4*lv% chains
/// GC_CROSSIMPACT. Teleport + chain are TODO.
/// </summary>
public sealed class DarkIllusion : WeaponSkillImpl
{
    public DarkIllusion() : base(SkillIds.GC_DARKILLUSION) { }
}
