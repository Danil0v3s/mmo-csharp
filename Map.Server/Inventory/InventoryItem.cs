namespace Map.Server.Inventory;

/// <summary>
/// Runtime mirror of one inventory row — what the gameplay loop sees
/// after the session loads. Mirrors <c>Core.Database.Entities.InventoryEntity</c>
/// but stripped of EF artefacts (no nav properties, no PK column on writes).
///
/// <para>
/// <see cref="ServerIndex"/> is the slot's position in the per-character
/// inventory list (0-based). The client-side index used in packets is
/// <c>ServerIndex + 2</c> — rAthena's <c>client_index</c> helper.
/// </para>
/// </summary>
public sealed class InventoryItem
{
    /// <summary>DB primary key. Zero for items that haven't hit the DB yet.</summary>
    public int Id { get; set; }
    public int ServerIndex { get; set; }
    public uint NameId { get; set; }
    public uint Amount { get; set; }
    /// <summary>Equip-slot bits set when this item is currently worn. 0 when not equipped.</summary>
    public uint Equip { get; set; }
    public bool Identified { get; set; } = true;
    public byte Refine { get; set; }
    public byte Attribute { get; set; }
    public uint Card0 { get; set; }
    public uint Card1 { get; set; }
    public uint Card2 { get; set; }
    public uint Card3 { get; set; }
    /// <summary>Random-option slots; up to 5. Zero-id means empty.</summary>
    public ItemOption[] Options { get; set; } = new ItemOption[5];
    /// <summary>Unix timestamp; 0 = no expiry.</summary>
    public uint ExpireTime { get; set; }
    public byte Favorite { get; set; }
    public byte Bound { get; set; }
    public ulong UniqueId { get; set; }
    public uint EquipSwitch { get; set; }
    public byte EnchantGrade { get; set; }
}

public struct ItemOption
{
    public short Id;
    public short Value;
    public sbyte Param;
}
