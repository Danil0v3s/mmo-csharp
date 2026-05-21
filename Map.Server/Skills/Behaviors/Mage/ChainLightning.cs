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
        // rAthena: schedules a WL_CHAINLIGHTNING_ATK follow-up hit. That id isn't on our
        // SkillIds catalog yet, so the chained sub-skill bounce is TODO — the primary cast
        // already landed via the magic resolver.
        _timers?.Schedule(src, target, delayMs: src.Stats.Amotion, SkillId, skillLevel,
            (s, t, lv) => _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillIds.WL_CHAINLIGHTNING, lv));
    }
}
