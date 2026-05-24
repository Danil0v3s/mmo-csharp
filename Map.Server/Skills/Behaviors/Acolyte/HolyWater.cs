using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_HOLYWATER — Acolyte Aqua Benedicta. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/holywater.cpp</c>.
///
/// <para>Produces one Holy Water item (rAthena
/// <c>ITEMID_HOLY_WATER = 523</c>) into the caster's bag and clears
/// the NJ_SUITON ground unit at the caster's cell (Aqua Benedicta
/// dispels the Water Cell visual). Player-only.</para>
///
/// <para>rAthena dispatches via <c>skill_produce_mix(sd, AL_HOLYWATER,
/// ITEMID_HOLY_WATER, 0, 0, 0, 1, -1)</c>. The C# port grants the
/// item through the session inventory bridge and walks the ground-unit
/// service for any SUITON unit on the cell.</para>
/// </summary>
public sealed class HolyWater : SkillImpl
{
    public HolyWater() : base(SkillIds.AL_HOLYWATER) { }

    /// <summary>rAthena <c>ITEMID_HOLY_WATER</c> — Aqua Benedicta produces id 523.</summary>
    private const uint ItemIdHolyWater = 523;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity caster) return;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena: skill_produce_mix(sd, AL_HOLYWATER, ITEMID_HOLY_WATER, ...).
        // Grant 1 Holy Water to the caster's inventory; the session bridge
        // mirrors rAthena's pc_additem path.
        if (ctx.Sessions != null && ctx.Inventory != null)
        {
            var session = ctx.Sessions.TryGet(caster);
            if (session != null)
            {
                ctx.Inventory.GiveItem(session, ItemIdHolyWater, 1);
            }
        }

        // rAthena: skill_unit_setting / clearing — Aqua Benedicta wipes
        // any NJ_SUITON unit standing on the caster's cell. Walk the
        // ground-unit list and delete the matching group.
        if (ctx.Units != null)
        {
            var units = ctx.Units.GetUnitsInArea(caster.MapId, caster.X, caster.Y,
                radius: 0, skillId: SkillIds.NJ_SUITON);
            foreach (var u in units)
            {
                ctx.Units.DelUnitGroup(u.Group);
            }
        }
    }
}
