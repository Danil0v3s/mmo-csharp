using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HAMI_CASTLE — Amistr Castling. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_castling.cpp</c>.
/// 20*lv% chance to swap positions with the master and pull aggro to
/// the homunculus. Aggro retarget + unit_movepos are TODO.
/// </summary>
public sealed class Castling : SkillImpl
{
    public Castling() : base(SkillIds.HAMI_CASTLE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
