using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// BS_ADRENALINE (id 111) — Blacksmith Adrenaline Rush. rAthena
/// <c>skill.cpp:case BS_ADRENALINE</c> applies
/// <see cref="StatusType.Adrenaline"/> on the caster (and party
/// members in the standard renewal SP-cost behavior). For the C#
/// port's first wave we apply to the caster only; party broadcast
/// rides on a future party-targeted skill enhancement.
///
/// AspdRate boost: +30 % (lv1) flat in renewal; Val1 stored at the
/// scaled rate so the SC handler can read it back without re-deriving.
/// Duration <c>60_000 + 60_000 * (lv-1)</c> ms.
/// </summary>
public sealed class AdrenalineRushBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.BS_ADRENALINE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Caster always gets the buff; party-wide projection is a later
        // wave (needs IPartyService.ForEachMemberInRange).
        var aspdBoost = 30; // rAthena flat 30 % at lv1+ for self.
        var durationMs = 60_000 + 60_000 * (skillLevel - 1);
        ctx.Sc.Start(source, StatusType.Adrenaline, val1: aspdBoost, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
