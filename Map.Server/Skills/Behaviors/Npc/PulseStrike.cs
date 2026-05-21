using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_PULSESTRIKE — Splash weapon hit; ratio +100*(lv-1).</summary>
public sealed class PulseStrike : RecursiveDamageSplashSkillImpl
{
    public PulseStrike() : base(SkillIds.NPC_PULSESTRIKE) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
}
