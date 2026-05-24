using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_DETECTING — Hunter Detect. Manual port of
/// <c>rathena-fork/src/map/skills/archer/detect.cpp</c>.
/// Reveals hidden enemies and traps in splash. Reveal helper not
/// wired here — broadcast only.
/// </summary>
public sealed class Detect : SkillImpl
{
    public Detect() : base(SkillIds.HT_DETECTING) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: invoke status_change_timer_sub on every BL_CHAR in splash to break
        // Hide/Cloak; also reveal traps via skill_reveal_trap_inarea — needs SC enumeration + BL_SKILL splash.
    }
}
