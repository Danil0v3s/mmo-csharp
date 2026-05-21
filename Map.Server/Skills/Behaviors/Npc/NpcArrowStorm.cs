using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ARROWSTORM — Splash arrow storm. Ratio +900 (lv≤4) / +1900 (lv>4).</summary>
public sealed class NpcArrowStorm : RecursiveDamageSplashSkillImpl
{
    public NpcArrowStorm() : base(SkillIds.NPC_ARROWSTORM) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (skillLevel > 4 ? 1900 : 900);
}
