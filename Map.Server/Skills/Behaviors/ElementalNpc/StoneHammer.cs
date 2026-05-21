using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_STONE_HAMMER — Elemental Stone Hammer. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/stonehammer.cpp</c>.
/// +400 ratio; chance to stun (10*lv%).
/// </summary>
public sealed class StoneHammer : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public StoneHammer() : base(SkillIds.EL_STONE_HAMMER) { }

    public StoneHammer(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_STONE_HAMMER)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
