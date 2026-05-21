using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_STEAL — Steal. Manual port of
/// <c>rathena-fork/src/map/skills/thief/steal.cpp</c>.
/// Calls into pc_steal_item; success animation + drop, failure fail.
/// pc_steal_item already lives in <c>PlayerStealService</c> — wiring
/// the broadcast is TODO; this stub broadcasts animation only.
/// </summary>
public sealed class Steal : SkillImpl
{
    public Steal() : base(SkillIds.TF_STEAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: pc_steal_item(sd, target, skill_lv) — animation gates on success.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
