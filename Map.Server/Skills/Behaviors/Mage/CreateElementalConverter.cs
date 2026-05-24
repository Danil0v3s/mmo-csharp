using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_CREATECON — Sage Create Elemental Converter (skill.cpp:SA_CREATECON
/// arm). Opens the converter picker via
/// <c>clif_elementalconverter_list</c>. The four elements
/// (Fire / Water / Wind / Earth converters) are the canonical result
/// items the caster can produce — itemIds resolved through the produce
/// catalog when present, otherwise the empty list still surfaces the
/// dialog frame.
/// </summary>
public sealed class CreateElementalConverter : SkillImpl
{
    // Aegis item ids for the 4 elemental converters (rAthena item_db).
    private const ushort FireConverter = 12114;
    private const ushort WaterConverter = 12115;
    private const ushort WindConverter = 12117;
    private const ushort EarthConverter = 12116;

    public CreateElementalConverter() : base(SkillIds.SA_CREATECON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ushort[] converters = { FireConverter, WaterConverter, WindConverter, EarthConverter };
        ctx.Client?.BroadcastElementalConverterList(pc, converters);
    }
}
