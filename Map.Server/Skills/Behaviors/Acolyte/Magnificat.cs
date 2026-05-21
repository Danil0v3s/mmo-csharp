using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_MAGNIFICAT — Priest Magnificat. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/magnificat.cpp</c>.
///
/// <para>Party-wide SP-regen doubler. Same dispatch shape as
/// <see cref="Angelus"/>: apply <see cref="StatusType.Magnificat"/>
/// to the target on the inner (no-party / flag&amp;1) branch;
/// iterate party members otherwise.</para>
///
/// <para>Duration per <c>db/re/skill_db.yml</c>:
/// <c>15000 * (1 + lv)</c> ms (30 s lv 1 → 165 s lv 10).</para>
/// </summary>
public sealed class Magnificat : SkillImpl
{
    public Magnificat() : base(SkillIds.PR_MAGNIFICAT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        var duration = 15_000 * (1 + skillLevel);
        ctx.Sc?.Start(target, StatusType.Magnificat, val1: skillLevel, 0, 0, 0, duration, src);
        // TODO: party_foreachsamemap — see Angelus for the rAthena pattern.
    }
}
