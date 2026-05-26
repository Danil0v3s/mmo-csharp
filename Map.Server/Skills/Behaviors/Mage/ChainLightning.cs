using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_CHAINLIGHTNING — Warlock Chain Lightning. Schedules the
/// follow-up <c>WL_CHAINLIGHTNING_ATK</c> hit at <c>tick + amotion</c>.
/// </summary>
public sealed class ChainLightning : SkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;
    public ChainLightning() : base(SkillIds.WL_CHAINLIGHTNING) { }
    public ChainLightning(Map.Server.Skills.ISkillTimerService? timers = null, Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.WL_CHAINLIGHTNING)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: schedules a WL_CHAINLIGHTNING_ATK hit at tick + amotion.
        // The chained-bounce ratio bump per hop is applied through the ATK
        // skill's own ratio formula (driven by miscflag from the
        // skill_addtimerskill data field).
        _timers?.Schedule(src, target, delayMs: src.Stats.Amotion, SkillId, skillLevel,
            (s, t, lv) => _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillIds.WL_CHAINLIGHTNING_ATK, lv));
    }
}
