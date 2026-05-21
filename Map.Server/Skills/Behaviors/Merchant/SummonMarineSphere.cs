using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_SPHEREMINE — Summon Marine Sphere. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/summonmarinesphere.cpp</c>.
/// Spawns MOBID_MARINE_SPHERE at the cast XY with AI_SPHERE, links it
/// to the caster, and arms the cleanup timer. Mob spawn helper isn't
/// ported yet — TODO.
/// </summary>
public sealed class SummonMarineSphere : SkillImpl
{
    public SummonMarineSphere() : base(SkillIds.AM_SPHEREMINE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: spawn MOBID_MARINE_SPHERE at (x, y) with AI_SPHERE, master_id = src.Id,
        // and delete-timer of skill_get_time(AM_SPHEREMINE, skillLevel).
    }
}
