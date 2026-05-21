using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_METEOR_STORM_BUSTER — Hyper Novice Meteor Storm Buster. Manual
/// port of <c>rathena-fork/src/map/skills/novice/meteorstormbuster.cpp</c>.
/// Explosion ratio <c>+(-100 + 450 + 160*lv) + 3*SPL</c>; fall ratio is
/// <c>+(-100 + 300 + 320*lv) + 3*SPL</c>. We use the explosion formula
/// by default. 3*lv% stun on hit. HN_SELFSTUDY_SOCERY amp +
/// SC_RULEBREAK boost are TODO.
/// </summary>
public sealed class MeteorStormBuster : SkillImpl
{
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;
    private readonly ISkillUnitService? _units;

    public MeteorStormBuster() : base(SkillIds.HN_METEOR_STORM_BUSTER) => _rng = Random.Shared;

    public MeteorStormBuster(ISkillAttackService? skillAttack = null, ISkillUnitService? units = null, Random? rng = null)
        : base(SkillIds.HN_METEOR_STORM_BUSTER)
    {
        _skillAttack = skillAttack;
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 + 160 * skillLevel) + 3 * src.Stats.Spl;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
