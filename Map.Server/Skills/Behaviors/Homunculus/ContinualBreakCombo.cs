using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_CBC — Homunculus Continual Break Combo. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_continualbreakcombo.cpp</c>.
/// Applies SC_CBC for max(lv, STRsrc/7 − STRtgt/10) seconds; runs a
/// damaging hit afterward.
/// </summary>
public sealed class ContinualBreakCombo : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public ContinualBreakCombo() : base(SkillIds.MH_CBC) { }

    public ContinualBreakCombo(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_CBC)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var seconds = Math.Max(skillLevel, src.Stats.Str / 7 - target.Stats.Str / 10);
        ctx.Sc?.Start(target, StatusType.Cbc, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: seconds * 1_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }
}
