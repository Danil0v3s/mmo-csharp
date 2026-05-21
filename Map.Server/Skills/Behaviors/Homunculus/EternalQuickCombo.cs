using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_EQC — Homunculus Eternal Quick Combo. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_eternalquickcombo.cpp</c>.
/// Applies SC_EQC for max(lv, STRsrc/7 − STRtgt/10) seconds; runs a hit.
/// Tinder Breaker 2 break + Stun follow-up are TODO.
/// </summary>
public sealed class EternalQuickCombo : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public EternalQuickCombo() : base(SkillIds.MH_EQC) { }

    public EternalQuickCombo(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_EQC)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var seconds = Math.Max(skillLevel, src.Stats.Str / 7 - target.Stats.Str / 10);
        ctx.Sc?.Start(target, StatusType.Eqc, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: seconds * 1_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }
}
