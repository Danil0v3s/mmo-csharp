using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_IGNITIONBREAK — Splash AoE fire damage; ratio +(-100 + 600*lv).</summary>
public sealed class NpcIgnitionBreak : RecursiveDamageSplashSkillImpl
{
    public NpcIgnitionBreak() : base(SkillIds.NPC_IGNITIONBREAK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 600 * skillLevel);
}
