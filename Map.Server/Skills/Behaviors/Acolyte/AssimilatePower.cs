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
            // rAthena: gunslinger coins are protected — skip the drain path
            // when the victim is any Gunslinger-family class (Rebellion too).
            if (MapidClass.IsBase(dstsd.ClassMask, MapidClass.Gunslinger))
            {
                ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, success: false);
                return;
            }
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

        // Outer splash iteration: rAthena's skill_area_sub invokes the inner
        // per-victim path on each PC in range. Use ctx.SkillAttack.SkillAreaSub
        // when present so the drain extends beyond the primary target.
        if (ctx.SkillAttack != null && target != null && ReferenceEquals(target, src))
        {
            // Self-cast outer iteration (PvP/GvG drain): walk a 3-cell range
            // around the caster and recursively invoke the inner branch.
            ctx.SkillAttack.SkillAreaSub(src, range: 3, victim =>
            {
                if (ReferenceEquals(victim, src)) return false;
                if (victim is PlayerEntity) CastendNoDamageId(src, victim, skillLevel, ctx);
                return true;
            });
        }
    }
}
