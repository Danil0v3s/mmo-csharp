using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_CREATECON — Sage Create Elemental Converter. Opens the
/// converter-selection list on the caster's client + emits cast frame.
/// The element-converter list packet is TODO (ZC_ELEMENTAL_CONVERTER).
/// </summary>
public sealed class CreateElementalConverter : SkillImpl
{
    public CreateElementalConverter() : base(SkillIds.SA_CREATECON) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: clif_elementalconverter_list — sends the chooser packet.
    }
}
