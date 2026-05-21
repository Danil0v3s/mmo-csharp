using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_FATALMENACE — Splash weapon hit. Ratio +100*lv.</summary>
public sealed class NpcFatalMenace : RecursiveDamageSplashSkillImpl
{
    public NpcFatalMenace() : base(SkillIds.NPC_FATALMENACE) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * skillLevel;
}
