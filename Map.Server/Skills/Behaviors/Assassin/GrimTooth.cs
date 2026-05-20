using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Assassin;

/// <summary>
/// AS_GRIMTOOTH — Assassin Grimtooth. Mirrors
/// <c>rathena-fork/src/map/skills/assassin/grimtooth.cpp</c>.
///
/// Ranged katar attack at (120 + 30*lv)% ATK. Auto-consumes
/// Hiding on cast.
/// </summary>
public sealed class GrimTooth : WeaponSkillImpl
{
    public GrimTooth() : base(SkillIds.AS_GRIMTOOTH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 20 + 30 * skillLevel; // 120 + 30*lv overall

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Pop hiding on cast (rAthena fold-in).
        ctx.Sc?.End(src, StatusType.Hiding);
    }
}
