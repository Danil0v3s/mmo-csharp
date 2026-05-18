using Map.Server.Items;

namespace Map.Server.Inventory;

/// <summary>
/// Encodes inventory items into the binary <c>NORMALITEM_INFO</c> and
/// <c>EQUIPITEM_INFO</c> structs that the client expects inside
/// <see cref="Core.Server.Packets.Out.ZC.ZC_INVENTORYLIST_NORMAL_V6"/> and
/// <see cref="Core.Server.Packets.Out.ZC.ZC_INVENTORYLIST_EQUIP_V6"/>.
///
/// Layouts target PACKETVER_RE_NUM >= 20220401 (our live-client baseline):
/// <list type="bullet">
///   <item>NORMALITEM_INFO = 34 bytes:
///     index(2) + ITID(4) + type(1) + count(2) + WearState(4) +
///     card[4]*4(16) + HireExpireDate(4) + Flag(1).</item>
///   <item>EQUIPITEM_INFO = 68 bytes:
///     index(2) + ITID(4) + type(1) + location(4) + WearState(4) +
///     card[4]*4(16) + HireExpireDate(4) + bindOnEquipType(2) +
///     wItemSpriteNumber(2) + option_count(1) + option_data[5]*5(25) +
///     RefiningLevel(1) + grade(1) + Flag(1).</item>
/// </list>
///
/// See rAthena <c>packets_struct.hpp</c> NORMALITEM_INFO (line 418) and
/// EQUIPITEM_INFO (line 457). Bytes are little-endian (BinaryWriter default).
/// </summary>
public static class InventoryPacketBuilder
{
    public const int NormalItemInfoSize = 34;
    public const int EquipItemInfoSize  = 68;

    /// <summary>
    /// Convert one <see cref="InventoryItem"/> into its 34-byte
    /// NORMALITEM_INFO encoding. The caller owns the type byte (looked
    /// up via <see cref="IItemCatalog"/>) so this builder doesn't
    /// depend on the catalog itself.
    /// </summary>
    public static byte[] BuildNormal(InventoryItem item, byte typeCode)
    {
        using var ms = new MemoryStream(NormalItemInfoSize);
        using var w = new BinaryWriter(ms);
        WriteNormal(w, item, typeCode);
        return ms.ToArray();
    }

    public static byte[] BuildEquip(InventoryItem item, byte typeCode, uint location)
    {
        using var ms = new MemoryStream(EquipItemInfoSize);
        using var w = new BinaryWriter(ms);
        WriteEquip(w, item, typeCode, location);
        return ms.ToArray();
    }

    /// <summary>
    /// Build the full body (sequence of NORMALITEM_INFOs) for one
    /// <see cref="Core.Server.Packets.Out.ZC.ZC_INVENTORYLIST_NORMAL_V6"/>.
    /// </summary>
    public static byte[] BuildNormalListBody(
        IEnumerable<InventoryItem> items, IItemCatalog catalog)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        foreach (var item in items)
        {
            var entry = catalog.Get(item.NameId);
            var type = ItemTypeCodes.FromDbString(entry?.Type);
            WriteNormal(w, item, type);
        }
        return ms.ToArray();
    }

    public static byte[] BuildEquipListBody(
        IEnumerable<InventoryItem> items, IItemCatalog catalog)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        foreach (var item in items)
        {
            var entry = catalog.Get(item.NameId);
            var type = ItemTypeCodes.FromDbString(entry?.Type);
            var location = LocationBitsFromCatalog(entry);
            WriteEquip(w, item, type, location);
        }
        return ms.ToArray();
    }

    private static void WriteNormal(BinaryWriter w, InventoryItem item, byte typeCode)
    {
        // index = rAthena `client_index` = server slot + 2
        w.Write((short)(item.ServerIndex + 2));      // index
        w.Write(item.NameId);                         // ITID (uint32)
        w.Write(typeCode);                            // type
        w.Write((short)item.Amount);                  // count
        w.Write((uint)0);                             // WearState (0 = not equipped)
        w.Write(item.Card0);                          // slot.card[0]
        w.Write(item.Card1);                          // slot.card[1]
        w.Write(item.Card2);                          // slot.card[2]
        w.Write(item.Card3);                          // slot.card[3]
        w.Write((int)item.ExpireTime);                // HireExpireDate
        w.Write(BuildNormalFlag(item));               // Flag bitfield
    }

    private static void WriteEquip(BinaryWriter w, InventoryItem item, byte typeCode, uint location)
    {
        w.Write((short)(item.ServerIndex + 2));      // index
        w.Write(item.NameId);                         // ITID
        w.Write(typeCode);                            // type
        w.Write(location);                            // location (uint32)
        w.Write(item.Equip);                          // WearState (uint32) — equip-slot bits
        w.Write(item.Card0);                          // slot.card[0]
        w.Write(item.Card1);                          // slot.card[1]
        w.Write(item.Card2);                          // slot.card[2]
        w.Write(item.Card3);                          // slot.card[3]
        w.Write((int)item.ExpireTime);                // HireExpireDate
        w.Write((ushort)0);                           // bindOnEquipType — set on equip
        w.Write((ushort)0);                           // wItemSpriteNumber (override view; 0 = use db)
        // option_count + 5 × ItemOptions
        var optionCount = (byte)item.Options.Count(o => o.Id != 0);
        w.Write(optionCount);
        for (var i = 0; i < 5; i++)
        {
            var opt = i < item.Options.Length ? item.Options[i] : default;
            w.Write(opt.Id);
            w.Write(opt.Value);
            w.Write(opt.Param);
        }
        w.Write(item.Refine);                         // RefiningLevel (moved here in 20200723+ RE)
        w.Write(item.EnchantGrade);                   // grade (same)
        w.Write(BuildEquipFlag(item));                // Flag bitfield
    }

    /// <summary>
    /// Flag byte: IsIdentified bit 0, PlaceETCTab bit 1, SpareBits 2-7.
    /// </summary>
    private static byte BuildNormalFlag(InventoryItem item)
    {
        byte flag = 0;
        if (item.Identified) flag |= 0b0000_0001;
        return flag;
    }

    /// <summary>
    /// Flag byte (equip): IsIdentified bit 0, IsDamaged bit 1,
    /// PlaceETCTab bit 2, SpareBits 3-7.
    /// </summary>
    private static byte BuildEquipFlag(InventoryItem item)
    {
        byte flag = 0;
        if (item.Identified) flag |= 0b0000_0001;
        if (item.Attribute != 0) flag |= 0b0000_0010;   // attribute>0 = broken in rAthena
        return flag;
    }

    /// <summary>
    /// Combine the per-slot location columns from the item-db row into the
    /// rAthena <c>equip</c> bitmask the client expects (which slots this
    /// item is wearable on). Slice 1 leaves this as 0 — until the
    /// EquipService lands, items don't claim any wear slots and the
    /// client falls back to its own item-db.
    /// </summary>
    private static uint LocationBitsFromCatalog(Core.Database.Entities.ItemEntity? entry)
    {
        // TODO(slice-5): fold location_armor / location_head_top / ... into
        // EQP_* bits (see rAthena `pc.hpp` EQP_*). Returning 0 here is safe;
        // the client uses its own item-db to render slot icons.
        return 0;
    }
}
