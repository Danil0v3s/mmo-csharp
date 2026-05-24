using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_DILECTIO_HEAL — Cardinal Dilectio Heal. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/dilectioheal.cpp</c>.
///
/// <para>Two-pass dispatch:</para>
/// <list type="bullet">
///   <item>Outer pass (flag &amp; 1 not set): broadcasts the cast
///         frame on the target, then recursively invokes self on the
///         same target with flag bit 1.</item>
///   <item>Inner pass (flag &amp; 1 set): apply
///         <c>skill_calc_heal</c> to the target — broadcast as if it
///         were AL_HEAL (so the heal popup uses the standard heal
///         visual) and heal HP. Party iteration happens via
///         flag &amp; 2 once the partied caster path lands.</item>
/// </list>
///
/// <para>The C# port executes both passes inline since flag is
/// implicit — single-target apply + AL_HEAL-flavored broadcast.</para>
/// </summary>
public sealed class DilectioHeal : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public DilectioHeal() : base(SkillIds.CD_DILECTIO_HEAL) { }

    public DilectioHeal(IStatusOpsService? statusOps = null) : base(SkillIds.CD_DILECTIO_HEAL)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena outer-pass broadcast — emits the cast visual on
        // the target so even when the caster fails to heal them, the
        // target sees the animation.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // Inner pass: heal calc + apply.
        var heal = (src.Level + src.Stats.IntStat) / 5 * 30 * skillLevel / 10;
        heal = Math.Max(1, heal);

        // rAthena emits as AL_HEAL so the client renders the standard
        // heal popup with the amount.
        ctx.Client?.BroadcastSkillNoDamage(null, target, SkillIds.AL_HEAL, heal);

        _statusOps?.Heal(target, heal, 0, 0);

        // rAthena outer recursion via flag&1: heal every same-map
        // partymate by the same calculated amount.
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Client?.BroadcastSkillNoDamage(null, m, SkillIds.AL_HEAL, heal);
                _statusOps?.Heal(m, heal, 0, 0);
            }, includeSelf: false);
        }
    }
}
