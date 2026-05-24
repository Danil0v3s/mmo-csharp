using Map.Server.Elemental;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_ELEMENTAL_VEIL — Elemental Master Elemental Veil
/// (skill.cpp:EM_ELEMENTAL_VEIL arm). Requires an EM-tier elemental
/// bound to the caster (Diluvio / Ardor / Procella / Terremotus /
/// Serpens). Applies <c>SC_ELEMENTAL_VEIL</c> to the caster on
/// success; refuses with <c>SkillFail</c> when the EM tier is
/// missing.
/// </summary>
public sealed class ElementalVeil : SkillImpl
{
    public ElementalVeil() : base(SkillIds.EM_ELEMENTAL_VEIL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (!IsEmTier(pc.ActiveElementalClassId))
        {
            ctx.Client?.BroadcastSkillFail(pc, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SummonNone);
            return;
        }
        ctx.Sc?.Start(src, StatusType.ElementalVeil, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }

    private static bool IsEmTier(int classId)
        => classId == ElementalClassIds.Diluvio
        || classId == ElementalClassIds.Ardor
        || classId == ElementalClassIds.Procella
        || classId == ElementalClassIds.Terremotus
        || classId == ElementalClassIds.Serpens;
}
