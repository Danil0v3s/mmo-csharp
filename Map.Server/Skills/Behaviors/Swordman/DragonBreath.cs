using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_DRAGONBREATH — Rune Knight Dragon Breath. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/dragonbreath.cpp</c>.
/// Hides the cast against hiding targets and otherwise lands a weapon
/// attack. On hit, 15% to inflict SC_BURNING.
/// </summary>
public sealed class DragonBreath : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;

    public DragonBreath() : base(SkillIds.RK_DRAGONBREATH) => _rng = Random.Shared;

    public DragonBreath(ISkillAttackService? skillAttack = null, Random? rng = null)
        : base(SkillIds.RK_DRAGONBREATH)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 15)
            ctx.Sc?.Start(target, StatusType.Burning, val1: skillLevel, val2: 1000, val3: (int)src.Id, 0, durationMs: 10_000, src);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Hiding) != null)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
            return;
        }
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }
}
