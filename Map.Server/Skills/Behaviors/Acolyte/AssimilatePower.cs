using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_ASSIMILATEPOWER — Sura Assimilate Power. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/assimilatepower.cpp</c>.
///
/// <para>Splash drain: consumes each PC victim's Spirit Spheres /
/// Charms and converts the count to a <c>1 %</c> SP restoration per
/// sphere on the caster. Gunslingers' Coins are exempt (per rAthena
/// comment). Self-cast in PvP/GvG drains the caster's own spheres.</para>
/// </summary>
public sealed class AssimilatePower : SkillImpl
{
    private readonly IPlayerOrbService? _orbs;
    private readonly IStatusOpsService? _statusOps;

    public AssimilatePower() : base(SkillIds.SR_ASSIMILATEPOWER) { }

    public AssimilatePower(
        IPlayerOrbService? orbs = null,
        IStatusOpsService? statusOps = null) : base(SkillIds.SR_ASSIMILATEPOWER)
    {
        _orbs = orbs;
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Inner per-victim path: drain spheres + SP %heal caster.
        if (target is PlayerEntity dstsd)
        {
            // TODO: gunslinger class guard via MAPID_FIRSTMASK.
            int amount = _orbs?.Get(dstsd, OrbKind.Spirit) ?? 0;
            if (amount > 0)
            {
                _orbs?.Remove(dstsd, OrbKind.Spirit, amount);
            }
            if (amount > 0)
            {
                // status_percent_heal(src, 0, amount) — amount % SP restore.
                _statusOps?.PercentHeal(src, hpPercent: 0, spPercent: (sbyte)Math.Min(100, amount));
            }
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, success: amount != 0);
        }

        // TODO: outer splash iteration when called via skill_area_sub.
    }
}
