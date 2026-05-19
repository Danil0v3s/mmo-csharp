using Core.Server.Packets.Out.ZC;
using Map.Server.Inventory;
using Map.Server.Items;

namespace Map.Server.Handlers.Equip;

/// <summary>
/// Wire-emission helpers for equip / unequip acks. Mirrors rAthena
/// <c>clif_equipitemack</c> + <c>clif_unequipitemack</c>.
/// </summary>
public static class EquipNotifier
{
    /// <summary>
    /// Map <see cref="EquipOpResult"/> to the modern (PACKETVER ≥ 20121205)
    /// <c>clif_equipitemack_flag</c> code: 0 = OK, 1 = level/job-restriction
    /// fail, 2 = generic fail.
    /// </summary>
    public static byte ToEquipAckCode(EquipOpResult result) => result switch
    {
        EquipOpResult.Ok => 0,
        EquipOpResult.NotIdentified or EquipOpResult.NotEquippable => 2,
        EquipOpResult.InvalidSlot or EquipOpResult.NoSuchItem or EquipOpResult.PlayerDead => 2,
        _ => 1,
    };

    public static ZC_REQ_WEAR_EQUIP_ACK BuildWearAck(
        ushort clientIndex,
        uint wearLocation,
        ushort viewId,
        EquipOpResult result) => new()
    {
        ClientIndex = clientIndex,
        WearLocation = wearLocation,
        ViewId = viewId,
        Result = ToEquipAckCode(result),
    };

    public static ZC_REQ_TAKEOFF_EQUIP_ACK BuildTakeOffAck(
        ushort clientIndex,
        uint wearLocation,
        bool success) => new()
    {
        ClientIndex = clientIndex,
        WearLocation = wearLocation,
        // rAthena clif_unequipitemack: 1 = success, 0 = fail. Legacy bool ack.
        Flag = success ? (byte)1 : (byte)0,
    };

    public static ushort ResolveViewId(IItemCatalog catalog, uint nameId)
    {
        var row = catalog.Get(nameId);
        return (ushort)(row?.View ?? 0);
    }
}
