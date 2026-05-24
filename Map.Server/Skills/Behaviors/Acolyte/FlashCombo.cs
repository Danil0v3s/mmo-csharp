using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_FLASHCOMBO — Sura Flash Combo. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/flashcombo.cpp</c>.
///
/// <para>Triggers a fixed 3-skill chain on the target:</para>
/// <list type="number">
///   <item>SR_DRAGONCOMBO at +0 ms</item>
///   <item>SR_FALLENEMPIRE at +750 ms</item>
///   <item>SR_TIGERCANNON at +1250 ms</item>
/// </list>
/// <para>Caster's attack/cast/use-item locks are held for the full
/// 1250 ms window. The deferred hits use
/// <see cref="ISkillAttackService"/> with BF_WEAPON.</para>
/// </summary>
public sealed class FlashCombo : StatusSkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public FlashCombo() : base(SkillIds.SR_FLASHCOMBO) { }

    public FlashCombo(
        Map.Server.Skills.ISkillTimerService? timers = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.SR_FLASHCOMBO)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena: lock the caster's attack / cast / use-item for the full
        // combo window (1250 ms). PlayerEntity.CanActUntilTick is the
        // canact-tick equivalent; downstream attack/skill gates check it
        // before allowing further input.
        const int ComboLockMs = 1250;
        if (src is PlayerEntity caster)
        {
            caster.CanActUntilTick = Environment.TickCount64 + ComboLockMs;
        }

        // Schedule the 3-skill chain. Each fires through SkillAttackService
        // as a BF_WEAPON hit on the target.
        var combo = new[] { SkillIds.SR_DRAGONCOMBO, SkillIds.SR_FALLENEMPIRE, SkillIds.SR_TIGERCANNON };
        var delays = new[] { 0, 750, 1250 };
        for (int i = 0; i < combo.Length; i++)
        {
            int delay = delays[i];
            ushort comboSkill = combo[i];
            _timers?.Schedule(src, target, delay, comboSkill, skillLevel,
                (s, t, lv) =>
                {
                    _skillAttack?.SkillAttack(BattleAttackType.Weapon, s, s, t, comboSkill, lv);
                });
        }
    }
}
