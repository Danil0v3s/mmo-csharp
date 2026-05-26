using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_CHAIN_REACTION_SHOT — Chain Reaction Shot. Manual port of
/// <c>rathena-fork/src/map/skills/thief/chainreactionshot.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 850*lv) + 15*con</c>. Each
/// splash victim takes a follow-up ABC_CHAIN_REACTION_SHOT_ATK
/// detonation: rAthena schedules it ~200 ms after the initial hit;
/// we dispatch it immediately within the same resolve since the C#
/// pipeline doesn't carry the per-call <c>tick</c> offset.
/// </summary>
public sealed class ChainReactionShot : RecursiveDamageSplashSkillImpl
{
    public ChainReactionShot() : base(SkillIds.ABC_CHAIN_REACTION_SHOT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 850 * skillLevel) + 15 * src.Stats.Con;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena splashSearch: clif_skill_nodamage(... CRS_ATK ...) +
        // map_foreachinrange(... CRS_ATK ...). Each splash victim takes
        // a follow-up ABC_CHAIN_REACTION_SHOT_ATK hit. We mirror the
        // detonation by routing the secondary skill through the
        // SkillAttack façade (when wired) so its own resolver picks up
        // the ATK-skill formula (+800 + 2550*lv + 15*con, +700*lv with
        // SC_CHASING).
        ctx.SkillAttack?.SkillAttack(Map.Server.Combat.BattleAttackType.Weapon,
            src, src, target, SkillIds.ABC_CHAIN_REACTION_SHOT_ATK, skillLevel);
    }
}
