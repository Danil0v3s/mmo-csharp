using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_DEVOTION — Crusader Devotion / Sacrifice. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/sacrifice.cpp</c>.
/// Devotes the target player to the caster — pulls damage onto the
/// caster. Slot management + Crusader-class refusal + HellPower gate
/// are TODO; we apply SC_DEVOTION with val1 = caster id.
/// </summary>
public sealed class Sacrifice : SkillImpl
{
    public Sacrifice() : base(SkillIds.CR_DEVOTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        if (target is not PlayerEntity)
        {
            // Only players can be devoted.
            return;
        }
        ctx.Sc?.Start(target, StatusType.Devotion, val1: (int)src.Id, val2: 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
