using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SV_STEMSPEAR — Summoner Silvervine Stem Spear. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/silvervinestemspear.cpp</c>.
/// Ratio <c>+600</c>. 10% bleed on hit. SU_SPIRITOFLAND walkspeed buff
/// is TODO.
/// </summary>
public sealed class SilvervineStemSpear : SkillImpl
{
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;

    public SilvervineStemSpear() : base(SkillIds.SU_SV_STEMSPEAR) => _rng = Random.Shared;

    public SilvervineStemSpear(ISkillAttackService? skillAttack = null, Random? rng = null)
        : base(SkillIds.SU_SV_STEMSPEAR)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 600;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
