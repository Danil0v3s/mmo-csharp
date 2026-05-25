using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Port of rAthena Mado Gear overheat path (pc.cpp:13082
/// <c>pc_overheat</c>). Each weapon swing while in Mado mode bumps
/// <c>SC_OVERHEAT_LIMITPOINT.Val1</c> by the per-swing heat amount
/// (fire weapons add 3, others add 1). When Val1 reaches the cap
/// (1000) the actual <c>SC_OVERHEAT</c> damage SC starts, draining
/// HP per tick until a cooling device is used (negative heat).
///
/// <para>Consumed by the BattleCalculator weapon path on every hit
/// when the source has Mado active (SC_MADOGEAR).</para>
/// </summary>
public interface IMadoGearService
{
    /// <summary>rAthena <c>pc_overheat</c> (pc.cpp:13082).</summary>
    void AddHeat(Entity bl, int heat);
}

/// <summary>Default <see cref="IMadoGearService"/>.</summary>
public sealed class MadoGearService : IMadoGearService
{
    private readonly IStatusChangeService _sc;
    private readonly ILogger<MadoGearService> _logger;

    /// <summary>rAthena cap (pc.cpp:13087) — Val1 clamped 0..1000.</summary>
    private const int OverheatCap = 1000;

    public MadoGearService(IStatusChangeService sc, ILogger<MadoGearService> logger)
    {
        _sc = sc;
        _logger = logger;
    }

    public void AddHeat(Entity bl, int heat)
    {
        var sce = _sc.Get(bl, StatusType.OverheatLimitpoint);
        if (sce != null)
        {
            sce.Val1 = Math.Clamp(sce.Val1 + heat, 0, OverheatCap);
            // Cooling device → wind down both SCs when limitpoint empties.
            if (heat < 0 && sce.Val1 == 0)
            {
                _sc.End(bl, StatusType.OverheatLimitpoint);
                _sc.End(bl, StatusType.Overheat);
            }
            // Reaching cap kicks the damage SC. rAthena starts SC_OVERHEAT
            // separately when the limitpoint Val1 hits the per-skill
            // threshold; we mirror that with a single threshold check
            // at the cap. (Sub-thresholds for visual warnings are caller
            // concerns.)
            else if (sce.Val1 >= OverheatCap && _sc.Get(bl, StatusType.Overheat) == null)
            {
                _sc.Start(bl, StatusType.Overheat, val1: 1, 0, 0, 0,
                    durationMs: 1000, source: bl);
            }
        }
        else if (heat > 0)
        {
            _sc.Start(bl, StatusType.OverheatLimitpoint, val1: heat, 0, 0, 0,
                durationMs: 1000, source: bl);
        }
    }
}
