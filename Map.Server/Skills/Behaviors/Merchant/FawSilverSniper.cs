using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_SILVERSNIPER — Mechanic FAW Silver Sniper. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/fawsilversniper.cpp</c>.
/// Spawns a Silver Sniper turret. mob_once_spawn TODO.
/// </summary>
public sealed class FawSilverSniper : SkillImpl
{
    public FawSilverSniper() : base(SkillIds.NC_SILVERSNIPER) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: mob_once_spawn(MOBID_SILVERSNIPER) tied to caster with deletetimer.
    }
}
