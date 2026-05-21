using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_GUARDIAN_RECALL — Mora (Guardian) recall. Manual port of
/// <c>rathena-fork/src/map/skills/other/guardiansrecall.cpp</c>.
/// Teleports the caster to mora (44, 151). pc_setpos pipeline is TODO.
/// </summary>
public sealed class GuardiansRecall : SkillImpl
{
    public GuardiansRecall() : base(SkillIds.ALL_GUARDIAN_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: pc_setpos(sd, "mora", 44, 151, CLR_TELEPORT).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
