using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_NEEDLE_OF_PARALYZE — Homunculus Needle of Paralyze. Manual port
/// of <c>rathena-fork/src/map/skills/homunculus/homunculus_needleofparalyze.cpp</c>.
/// Ratio <c>+(-100 + 450*lv*BaseLv/100) + DEX</c>. (30 + 5*lv)%
/// SC_PARALYSIS on hit.
/// </summary>
public sealed class NeedleOfParalyze : SkillImpl
{
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;

    public NeedleOfParalyze() : base(SkillIds.MH_NEEDLE_OF_PARALYZE) => _rng = Random.Shared;

    public NeedleOfParalyze(ISkillAttackService? skillAttack = null, Random? rng = null)
        : base(SkillIds.MH_NEEDLE_OF_PARALYZE)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 * skillLevel * src.Level / 100) + src.Stats.Dex;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 30 + 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Paralysis, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
