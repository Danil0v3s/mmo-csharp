using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_HOLYWATER — Acolyte Aqua Benedicta. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/holywater.cpp</c>.
///
/// <para>Produces one Holy Water item using <c>skill_produce_mix</c>.
/// Also wipes the NJ_SUITON ground unit at the target cell on
/// success (Aqua Benedicta clears the Ninja Water Cell visual).</para>
///
/// <para>Player-only. Item-production helper isn't wired through
/// <c>SkillBehaviorContext</c> yet (SK-M4 in the roadmap), so the
/// cast emits the visual but doesn't actually grant the item.</para>
/// </summary>
public sealed class HolyWater : SkillImpl
{
    public HolyWater() : base(SkillIds.AL_HOLYWATER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity caster) return;

        // TODO: skill_produce_mix(sd, AL_HOLYWATER, ITEMID_HOLY_WATER, 0,0,0, 1, -1).
        // For now broadcast the cast visual; once production wires up
        // it'll deliver the item + clear SUITON at the cell.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // TODO: clear NJ_SUITON skill unit at (target.X, target.Y) via
        // ISkillUnitService.RemoveAt(skillId, x, y) once that accessor lands.
    }
}
