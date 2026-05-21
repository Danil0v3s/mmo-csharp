using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_WIDESUCK — Mirrors
/// <c>rathena-fork/src/map/skills/npc/widesuck.cpp</c>.
/// Cell-placed ground unit (CastendPos2 → skill_unitsetting). NOT an
/// SC_BLOODSUCKER application — that was a port bug found via the
/// T3.1 rAthena parity sweep.
/// </summary>
public sealed class WideSuck : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public WideSuck() : base(SkillIds.NPC_WIDESUCK) { }
    public WideSuck(ISkillUnitService? units = null) : base(SkillIds.NPC_WIDESUCK) { _units = units; }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
