using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_DOMINION_IMPULSE — Minstrel/Wanderer Dominion Impulse. Manual
/// port of <c>rathena-fork/src/map/skills/archer/dominionimpulse.cpp</c>.
/// Triggers all Reverberation units in splash. skill_active_reverberation
/// helper not surfaced — broadcast only.
/// </summary>
public sealed class DominionImpulse : SkillImpl
{
    public DominionImpulse() : base(SkillIds.WM_DOMINION_IMPULSE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: iterate BL_SKILL units in splash and fire skill_active_reverberation — needs BL_SKILL splash enumerator in ctx.
    }
}
