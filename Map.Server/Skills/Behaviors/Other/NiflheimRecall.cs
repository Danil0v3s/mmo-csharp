using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_NIFLHEIM_RECALL — Niflheim recall. Manual port of
/// <c>rathena-fork/src/map/skills/other/niflheimrecall.cpp</c>.
/// Teleports the caster to niflheim (193, 186). pc_setpos is TODO.
/// </summary>
public sealed class NiflheimRecall : SkillImpl
{
    public NiflheimRecall() : base(SkillIds.ALL_NIFLHEIM_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: pc_setpos(sd, "niflheim", 193, 186, CLR_TELEPORT).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
