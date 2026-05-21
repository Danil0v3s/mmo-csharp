using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_STORMBLAST — Rune Knight Storm Blast. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/stormblast.cpp</c>.
/// Ratio <c>+(-100 + (RuneMastery + STR/6) * 100)</c>. RuneMastery
/// defaults to 0 pending skilltree wiring.
/// </summary>
public sealed class StormBlast : RecursiveDamageSplashSkillImpl
{
    public StormBlast() : base(SkillIds.RK_STORMBLAST) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + (src.Stats.Str / 6) * 100);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
