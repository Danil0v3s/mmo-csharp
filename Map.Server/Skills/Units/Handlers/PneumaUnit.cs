using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// AL_PNEUMA — single-cell defensive unit. Applies <c>SC_PNEUMA</c> to
/// every entity standing on the cell (blocks ranged attacks) and ends
/// when they leave. No periodic damage; OnTick re-applies the SC each
/// interval to keep the buff alive for any entity still standing on
/// the cell. Lifetime 5s per rAthena <c>skill_get_time(AL_PNEUMA)</c>.
/// </summary>
public sealed class PneumaUnit : ISkillUnitTickHandler
{
    public ushort SkillId => SkillIds.AL_PNEUMA;

    public int DurationMs(ushort skillLevel) => 5_000;
    public int IntervalMs(ushort skillLevel) => 1_000;  // sweep cadence for re-entry
    public int Radius(ushort skillLevel) => 0;          // single cell

    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx)
    {
        // Re-affirm the SC each interval so stepping back onto the cell
        // restarts the duration (rAthena re-applies via onplace_timer).
        ctx.Sc?.Start(victim, StatusType.Pneuma, val1: skillLevel, 0, 0, 0, durationMs: IntervalMs(skillLevel) + 100, caster);
    }

    // Pneuma is friendly — apply to everyone alive on the cell (party,
    // self, allies). Override the default "skip caster" rule.
    public bool IsValidVictim(Entity? caster, Entity victim) => victim switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => false,
    };
}
