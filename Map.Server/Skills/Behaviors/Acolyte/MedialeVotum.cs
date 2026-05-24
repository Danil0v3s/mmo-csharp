using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_MEDIALE_VOTUM — Cardinal Mediale Votum. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/medialevotum.cpp</c>.
///
/// <para>Same dual-pass structure as <see cref="DilectioHeal"/>:</para>
/// <list type="bullet">
///   <item>Outer pass (flag bit 0): apply the SC marker via the
///         <see cref="StatusSkillImpl"/> generic path.</item>
///   <item>Inner pass (flag bit 0): compute heal + broadcast as
///         AL_HEAL on each party member.</item>
/// </list>
/// <para>The C# port runs both passes inline since flag is implicit.</para>
/// </summary>
public sealed class MedialeVotum : StatusSkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public MedialeVotum() : base(SkillIds.CD_MEDIALE_VOTUM) { }

    public MedialeVotum(IStatusOpsService? statusOps = null) : base(SkillIds.CD_MEDIALE_VOTUM)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena's outer branch broadcasts as AL_HEAL on the target,
        // then heals via skill_calc_heal.
        var heal = (src.Level + src.Stats.IntStat) / 5 * 30 * skillLevel / 10;
        heal = Math.Max(1, heal);

        ctx.Client?.BroadcastSkillNoDamage(null, target, SkillIds.AL_HEAL, heal);
        _statusOps?.Heal(target, heal, 0, 0);

        // rAthena party_foreachsamemap: heal every same-map partymate
        // with the same amount.
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
