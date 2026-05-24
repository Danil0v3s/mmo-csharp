using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;
using Map.Server.World;

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

        // Map-flag gating: WoE / GvG / NoSkill reject the revive entirely.
        // (Battleground is a separate flag in rAthena's mapflag.yml and isn't
        // modeled in our MapFlag enum yet — deferred per PARITY-REMAINING.md §P2.3.)
        if (ctx.MapFlags != null && ctx.World != null)
        {
            string? mapName = null;
            foreach (var m in ctx.World.All)
            {
                if ((uint)m.Name.GetHashCode() == caster.MapId) { mapName = m.Name; break; }
            }
            if (mapName != null && (ctx.MapFlags.IsSet(mapName, MapFlag.Gvg)
                                     || ctx.MapFlags.IsSet(mapName, MapFlag.NoSkill)))
            {
                ctx.Client?.BroadcastSkillFail(caster, SkillId,
                    Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
                return;
            }
        }

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
