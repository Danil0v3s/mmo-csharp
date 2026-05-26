using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_RENOVATIO — Arch Bishop Renovatio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/renovatio.cpp</c>.
///
/// <para>Holy-element HoT buff. Single-target path applies
/// <see cref="StatusType.Renovatio"/>; the partied caster path
/// iterates same-map members via <see cref="IPartyMapService.ForEachOnSameMap"/>.</para>
///
/// <para>Duration: <c>90000 ms</c> per <c>db/re/skill_db.yml</c>.</para>
/// </summary>
public sealed class Renovatio : StatusSkillImpl
{
    public Renovatio() : base(SkillIds.AB_RENOVATIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Renovatio,
            val1: skillLevel, 0, 0, 0, durationMs: 90_000, src);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Renovatio, val1: skillLevel, 0, 0, 0, durationMs: 90_000, src);
            }, includeSelf: false);
        }
    }
}
