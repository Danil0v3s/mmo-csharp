using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_CHAIN_REACTION_SHOT — Chain Reaction Shot. Manual port of
/// <c>rathena-fork/src/map/skills/thief/chainreactionshot.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 850*lv) + 15*con</c>.
/// Follow-up ABC_CHAIN_REACTION_SHOT_ATK detonation is TODO.
/// </summary>
public sealed class ChainReactionShot : RecursiveDamageSplashSkillImpl
{
    public ChainReactionShot() : base(SkillIds.ABC_CHAIN_REACTION_SHOT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 850 * skillLevel) + 15 * src.Stats.Con;
}
