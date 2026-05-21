using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_REDEMPTIO — Priest Redemptio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/redemptio.cpp</c>.
///
/// <para>Party-wide group revive at the cost of the caster's life
/// (post-revive HP = 1). For each revived party member, caster
/// loses a percentage of base EXP (pre-renewal only). Renewal
/// drops the EXP penalty.</para>
///
/// <para>WoE / Battleground maps reject the cast (no reviving on
/// PvP ground). SC_HELLPOWER targets emit the cast frame but no
/// revive. Caster's SP becomes 0 and HP becomes 1.</para>
///
/// <para>Party iteration is TODO until the same-map party helper
/// lands; this port handles the per-target revive branch faithfully
/// + sets the caster's HP/SP to the cost.</para>
/// </summary>
public sealed class Redemptio : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public Redemptio() : base(SkillIds.PR_REDEMPTIO) { }

    public Redemptio(IStatusOpsService? statusOps = null) : base(SkillIds.PR_REDEMPTIO)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity caster) return;

        // TODO: map-flag gating (WoE / Battleground) once IMapFlagService
        // is routed through SkillBehaviorContext.

        // Per-victim path: must be dead, no SC_HELLPOWER.
        if (target.Stats.Hp > 0) return;

        if (ctx.Sc?.Get(target, StatusType.Hellpower) != null)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillIds.ALL_RESURRECTION, skillLevel);
            return;
        }

        // Redemptio always uses Resurrection lv 3 (50% HP).
        var revived = _statusOps?.Revive(target, percentHp: 50, percentSp: 0) ?? 0;
        if (revived > 0)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillIds.ALL_RESURRECTION, 3);
        }

        // Caster pays the cost: HP = 1 (renewal skips the EXP penalty).
        caster.Hp = 1;
        caster.Sp = 0;
    }
}
