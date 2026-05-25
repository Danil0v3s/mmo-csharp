using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Port of rAthena <c>pc_updateweightstatus</c> (pc.cpp:3026). Maintains
/// the SC_WEIGHT50 / SC_WEIGHT90 overweight overlay on a player based on
/// their current carried weight vs max weight ratio:
/// <list type="bullet">
///   <item>&lt; 50 %  → both SCs cleared</item>
///   <item>50–89 % → SC_WEIGHT50 active</item>
///   <item>≥ 90 %  → SC_WEIGHT90 active (replaces SC_WEIGHT50)</item>
/// </list>
///
/// <para>Caller responsibility: invoke whenever inventory or equip
/// changes (rAthena calls it from <c>pc_additem</c>, <c>pc_delitem</c>,
/// <c>pc_equipitem</c>, <c>pc_unequipitem</c>, and the inventory bulk
/// load path). The service reads `session.Inventory` + per-item
/// `IItemCatalog.Get(itemId).Weight` to compute the current weight; the
/// max weight is derived from <see cref="RenewalFormulas.MaxWeight"/>
/// until the bonus path lands.</para>
/// </summary>
public interface IPlayerWeightStatusService
{
    /// <summary>rAthena <c>pc_updateweightstatus</c> (pc.cpp:3026). Returns
    /// the new overweight tier (0/1/2) after the SC dispatch — useful for
    /// log/observability and tests.</summary>
    int UpdateWeightStatus(PlayerEntity pc);
}
