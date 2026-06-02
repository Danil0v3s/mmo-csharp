using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Default <see cref="IPlayerWeightStatusService"/>. Port of rAthena
/// <c>pc_updateweightstatus</c> (pc.cpp:3026).
/// </summary>
public sealed class PlayerWeightStatusService : IPlayerWeightStatusService
{
    private readonly IStatusChangeService _sc;
    private readonly IItemCatalog? _catalog;
    private readonly IBattleConfigService? _battleConfig;
    private readonly ISessionManagerAccessor? _sessions;
    private readonly ILogger<PlayerWeightStatusService> _logger;

    public PlayerWeightStatusService(
        IStatusChangeService sc,
        ILogger<PlayerWeightStatusService> logger,
        IItemCatalog? catalog = null,
        IBattleConfigService? battleConfig = null,
        ISessionManagerAccessor? sessions = null)
    {
        _sc = sc;
        _catalog = catalog;
        _battleConfig = battleConfig;
        _sessions = sessions;
        _logger = logger;
    }

    public int UpdateWeightStatus(PlayerEntity pc)
    {
        // rAthena pc.cpp:3028 — read old tier from existing SCs.
        bool weight90 = _sc.Get(pc, StatusType.Weight90) != null;
        bool weight50 = _sc.Get(pc, StatusType.Weight50) != null;
        int oldTier = weight90 ? 2 : weight50 ? 1 : 0;

        // rAthena pc.cpp:3029 — pc_getpercentweight.
        int percent = GetPercentWeight(pc);

        // rAthena pc.cpp:3030 — config-driven cutoffs (renewal uses 70 / 90).
        int heavyCutoff = _battleConfig?.GetValue("natural_heal_weight_rate_renewal") ?? 70;
        int majorCutoff = _battleConfig?.GetValue("major_overweight_rate") ?? 90;

        int newTier = (percent >= majorCutoff) ? 2 : (percent >= heavyCutoff) ? 1 : 0;

        if (oldTier == newTier) return newTier;

        switch (newTier)
        {
            case 0:
                _sc.End(pc, StatusType.Weight50);
                _sc.End(pc, StatusType.Weight90);
                break;
            case 1:
                _sc.End(pc, StatusType.Weight90); // ensure we don't double-stack
                _sc.Start(pc, StatusType.Weight50, val1: 1, 0, 0, 0,
                    durationMs: 0 /* permanent until cleared */,
                    source: pc);
                break;
            case 2:
                _sc.End(pc, StatusType.Weight50);
                _sc.Start(pc, StatusType.Weight90, val1: 1, 0, 0, 0,
                    durationMs: 0, source: pc);
                break;
        }

        return newTier;
    }

    /// <summary>rAthena <c>pc_getpercentweight</c> (pc.cpp). Reads
    /// inventory + Weight from the item catalog; max is delegated to
    /// <see cref="RenewalFormulas.MaxWeight"/> as a default until the
    /// bonus-pipeline MaxWeight path is fully wired.</summary>
    private int GetPercentWeight(PlayerEntity pc)
    {
        if (_sessions == null) return 0;
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return 0;

        long currentWeight = 0;
        if (_catalog != null)
        {
            foreach (var row in session.Inventory)
            {
                if (row.Amount == 0) continue;
                var def = _catalog.Get(row.NameId);
                if (def == null) continue;
                currentWeight += (long)(def.Weight ?? 0) * row.Amount;
            }
        }

        // Default max weight = 20000 (rAthena pc.cpp base). Job-table
        // MaxWeight + bonus pipe accrual is layered on later by the
        // status calc path; until that lands we floor to the rAthena
        // base to avoid divide-by-zero. COMBAT-45 — bAddMaxWeight
        // (SP_ADD_MAX_WEIGHT) adds a flat bonus to the cap.
        long maxWeight = 20000 + Math.Max(0, pc.EquipBonuses?.AddMaxWeight ?? 0);

        return (int)((currentWeight * 100L) / maxWeight);
    }
}
