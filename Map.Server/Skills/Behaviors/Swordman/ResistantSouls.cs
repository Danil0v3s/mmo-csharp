using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_PROVIDENCE — Crusader Providence / Resistant Souls. Manual port
/// of <c>rathena-fork/src/map/skills/swordman/resistantsouls.cpp</c>.
/// Applies SC_PROVIDENCE; refuses to target other crusaders (TODO —
/// needs job-class check).
/// </summary>
public sealed class ResistantSouls : SkillImpl
{
    public ResistantSouls() : base(SkillIds.CR_PROVIDENCE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // rAthena (skill.cpp:8037): CR_PROVIDENCE fails when the target is
        // already a Crusader (class_ & MAPID_UPPERMASK == MAPID_CRUSADER).
        if (target is PlayerEntity tpc
            && (MapidClass.FromClassId(tpc.ClassId) & MapidClass.UpperMask) == MapidClass.Crusader)
        {
            return;
        }
        ctx.Sc?.Start(target, StatusType.Providence, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
