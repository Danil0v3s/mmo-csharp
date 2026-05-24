using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_MONOCELL — Sage Monocell. Manual port of
/// <c>rathena-fork/src/map/skills/mage/monocell.cpp</c>.
///
/// <para>Transforms the target monster into a Poring (MOBID_PORING).
/// Player caster fails on status-immune (boss) mobs. On success it
/// also clears every common SC and a curated list of crowd-control
/// SCs. <c>mob_class_change</c> isn't wired here yet — the
/// transformation is left as TODO and we only land the broadcast +
/// safety gate.</para>
/// </summary>
public sealed class Monocell : SkillImpl
{
    public Monocell() : base(SkillIds.SA_MONOCELL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not MobEntity)
            return;
        if (src is PlayerEntity sd && (target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // MOBID_PORING = 1002 in rAthena's mob_db.
        ctx.MobOps?.SetClass((MobEntity)target, 1002);
        // Deferred: rAthena also clears every common SC + a curated CC list on success;
        // bulk SC clean-up helper isn't surfaced on IStatusChangeService yet.
    }
}
