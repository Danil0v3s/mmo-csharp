using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_PULSESTRIKE2 — Splash weapon hit variant.</summary>
public sealed class PulseStrike2 : RecursiveDamageSplashSkillImpl
{
    public PulseStrike2() : base(SkillIds.NPC_PULSESTRIKE2) { }
}
