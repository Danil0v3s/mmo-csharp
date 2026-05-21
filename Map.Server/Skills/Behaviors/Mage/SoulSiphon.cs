using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_SOULBURN — Professor Soul Siphon (Soul Burn). Manual port of
/// <c>rathena-fork/src/map/skills/mage/soulsiphon.cpp</c>.
///
/// <para>Drains the target's full SP at a <c>30 + 10*lv %</c> rate
/// (capped at 70 % for lv 1-4, fixed 70 % for lv 5+). On failure the
/// SP loss boomerangs onto the caster. Lv 5 additionally fires a
/// magic-damage hit on the resolved side.</para>
/// </summary>
public sealed class SoulSiphon : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly IStatusOpsService? _statusOps;
    private readonly Random _rng;

    public SoulSiphon() : base(SkillIds.PF_SOULBURN) => _rng = Random.Shared;

    public SoulSiphon(
        ISkillAttackService? skillAttack = null,
        IStatusOpsService? statusOps = null,
        Random? rng = null) : base(SkillIds.PF_SOULBURN)
    {
        _skillAttack = skillAttack;
        _statusOps = statusOps;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = skillLevel < 5 ? 30 + skillLevel * 10 : 70;
        var success = _rng.Next(100) < rate;
        var landingTarget = success ? target : src;
        ctx.Client?.BroadcastSkillNoDamage(src, landingTarget, SkillId, skillLevel);
        if (skillLevel == 5)
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, landingTarget, SkillId, skillLevel);
        _statusOps?.PercentDamage(src, landingTarget, 0, 100, can_kill: false);
    }
}
