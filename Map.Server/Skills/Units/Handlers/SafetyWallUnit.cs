using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// MG_SAFETYWALL — single-cell defensive unit. Applies <c>SC_SAFETYWALL</c>
/// to entities standing on the cell (blocks N melee hits where N grows
/// with skill_lv). Wall expires when its hit-counter hits zero (handled
/// by SC_SAFETYWALL's tick) or when 30s passes. Like Pneuma, OnTick
/// re-affirms the SC for any entity still on the cell.
/// </summary>
public sealed class SafetyWallUnit : ISkillUnitTickHandler
{
    public ushort SkillId => SkillIds.MG_SAFETYWALL;

    public int DurationMs(ushort skillLevel) => 30_000;
    public int IntervalMs(ushort skillLevel) => 1_000;
    public int Radius(ushort skillLevel) => 0;

    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx)
    {
        // val1 = block-count remaining (rAthena: 2 + 2*skill_lv at place
        // time, decremented by the damage interceptor).
        ctx.Sc?.Start(victim, StatusType.Safetywall, val1: skillLevel, val2: 2 + 2 * skillLevel, 0, 0, durationMs: IntervalMs(skillLevel) + 100, caster);
    }

    public bool IsValidVictim(Entity? caster, Entity victim) => victim switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => false,
    };
}
