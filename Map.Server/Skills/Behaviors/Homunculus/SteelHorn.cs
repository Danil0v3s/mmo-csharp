using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_STAHL_HORN — Homunculus Stahl Horn. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_steelhorn.cpp</c>.
/// Ratio <c>+(-100 + 1000 + 300*lv*BaseLv/150) + VIT</c>. (20 + 2*lv)%
/// stun on hit.
/// </summary>
public sealed class SteelHorn : SkillImpl
{
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;

    public SteelHorn() : base(SkillIds.MH_STAHL_HORN) => _rng = Random.Shared;

    public SteelHorn(ISkillAttackService? skillAttack = null, Random? rng = null) : base(SkillIds.MH_STAHL_HORN)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1000 + 300 * skillLevel * src.Level / 150) + src.Stats.Vit;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 20 + 2 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
