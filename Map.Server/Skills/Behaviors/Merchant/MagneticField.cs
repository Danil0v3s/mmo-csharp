using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_MAGNETICFIELD — Mechanic Magnetic Field. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/magneticfield.cpp</c>.
/// Centred on the named target — splashes SC_MAGNETICFIELD on every
/// enemy in a 3-cell radius via
/// <see cref="IEntityRegistry.ForEachInRange"/>. The caster itself
/// is excluded (rAthena <c>map_flag_vs</c> filter); we follow the
/// PvE-conservative path of always skipping the caster.
///
/// <para>Val2 carries the caster's Entity id so the SC tick can route
/// the per-second SP-drain back to a valid attacker reference.</para>
/// </summary>
public sealed class MagneticField : SkillImpl
{
    private const short SplashRange = 3;
    private const int DurationMs = 5000;

    public MagneticField() : base(SkillIds.NC_MAGNETICFIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            SplashRange, EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            ctx.Sc?.Start(v, StatusType.Magneticfield, val1: skillLevel, val2: (int)src.Id, 0, 0,
                durationMs: DurationMs, src);
        }
    }
}
