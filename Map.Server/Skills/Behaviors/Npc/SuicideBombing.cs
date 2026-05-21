using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SELFDESTRUCTION — Self-destruct dealing AoE damage equal to caster current HP.</summary>
public sealed class SuicideBombing : RecursiveDamageSplashSkillImpl
{
    public SuicideBombing() : base(SkillIds.NPC_SELFDESTRUCTION) { }
}
