namespace Map.Server.Storage;

/// <summary>
/// One slot in account or guild storage. Same shape as
/// <see cref="Map.Server.Inventory.InventoryItem"/> trimmed to the
/// columns persisted in <c>storage</c> / <c>guild_storage</c>.
/// Mirrors rAthena <c>struct item</c> inside <c>s_storage</c>.
/// </summary>
public sealed class StorageItem
{
    public int ServerIndex { get; set; }
    public uint NameId { get; set; }
    public uint Amount { get; set; }
    public bool Identified { get; set; } = true;
    public byte Refine { get; set; }
    public byte Attribute { get; set; }
    public uint Card0 { get; set; }
    public uint Card1 { get; set; }
    public uint Card2 { get; set; }
    public uint Card3 { get; set; }
    public uint ExpireTime { get; set; }
    public byte Bound { get; set; }
    public ulong UniqueId { get; set; }
    public byte EnchantGrade { get; set; }
}
