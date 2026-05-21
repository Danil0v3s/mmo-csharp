using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SPLASHATTACK — Splash weapon attack.</summary>
public sealed class SplashAttack : RecursiveDamageSplashSkillImpl
{
    public SplashAttack() : base(SkillIds.NPC_SPLASHATTACK) { }
}
