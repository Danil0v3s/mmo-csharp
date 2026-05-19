using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Inventory.ItemEffects;

/// <summary>
/// Apply a status change for a fixed duration. The rAthena equivalent of
/// <c>sc_start type,duration,val1</c> in an item Script.
/// </summary>
public sealed class ApplyStatusHandler : IItemEffectHandler
{
    public string AegisName { get; }
    public StatusType Status { get; }
    public int Val1 { get; }
    public int DurationMs { get; }

    private readonly IStatusChangeService _scService;

    public ApplyStatusHandler(string aegisName, StatusType status, int val1, int durationMs, IStatusChangeService scService)
    {
        AegisName = aegisName;
        Status = status;
        Val1 = val1;
        DurationMs = durationMs;
        _scService = scService;
    }

    public bool Apply(PlayerEntity user)
    {
        var sc = _scService.Start(user, Status, Val1, 0, 0, 0, DurationMs);
        return sc != null;
    }
}
