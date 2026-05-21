using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_FLAMELAUNCHER — Mechanic Flame Launcher. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/flamelauncher.cpp</c>.
/// Eight-path Fire splash. Ratio: <c>+(200 + 300*lv)</c>. SC_BURNING
/// at <c>20 + 10*lv %</c>. Eight-path AoE TODO — primary hit lands.
/// </summary>
public sealed class FlameLauncher : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly Random _rng;

    public FlameLauncher() : base(SkillIds.NC_FLAMELAUNCHER) => _rng = Random.Shared;

    public FlameLauncher(ISkillAttackService? skillAttack = null, Random? rng = null) : base(SkillIds.NC_FLAMELAUNCHER)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200 + 300 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 20 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Burning, val1: skillLevel, val2: 1000, val3: (int)src.Id, 0, durationMs: 10_000, src);
    }
}
