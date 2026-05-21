using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_RAYOFGENESIS — Splash holy damage. Ratio +(-100 + 500*lv).</summary>
public sealed class NpcRayOfGenesis : RecursiveDamageSplashSkillImpl
{
    public NpcRayOfGenesis() : base(SkillIds.NPC_RAYOFGENESIS) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 500 * skillLevel);
}
