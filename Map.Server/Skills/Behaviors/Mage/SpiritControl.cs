using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_CONTROL — Sorcerer Spirit Control. Manual port of
/// <c>rathena-fork/src/map/skills/mage/spiritcontrol.cpp</c>.
///
/// <para>Controls the caster's bound elemental:
/// lv 1 → passive, lv 2 → assist, lv 3 → aggressive, lv 4 → dismiss.
/// Bound-elemental access + mode change isn't wired here — for now we
/// only validate caster type and broadcast the cast.</para>
/// </summary>
public sealed class SpiritControl : SkillImpl
{
    public SpiritControl() : base(SkillIds.SO_EL_CONTROL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity)
            return;
        // Deferred: bound-elemental subsystem not ported — mode change (passive/assist/
        // aggressive) + delete per skillLevel needs it.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
