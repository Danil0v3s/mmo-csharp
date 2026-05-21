using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_QD_SHOT — Rebellion Quick-Draw Shot. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/quickdrawshot.cpp</c>.
/// Hits the marked target + SC_C_MARKER targets in splash. Splash
/// dispatch + SC_QD_SHOT_READY consumption are TODO; we run a single
/// weapon strike at the named target.
/// </summary>
public sealed class QuickDrawShot : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public QuickDrawShot() : base(SkillIds.RL_QD_SHOT) { }

    public QuickDrawShot(ISkillAttackService? skillAttack = null) : base(SkillIds.RL_QD_SHOT)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
