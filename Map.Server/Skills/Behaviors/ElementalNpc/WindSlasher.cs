using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_WIND_SLASH — Elemental Wind Slasher. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/windslasher.cpp</c>.
/// +100 ratio; 25% bleed (SC_BLEEDING).
/// </summary>
public sealed class WindSlasher : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public WindSlasher() : base(SkillIds.EL_WIND_SLASH) { }

    public WindSlasher(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_WIND_SLASH)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 25)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
}
