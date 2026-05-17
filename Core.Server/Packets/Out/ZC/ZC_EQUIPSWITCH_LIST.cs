namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_equipswitch_list</c> ([clif.cpp:22196]). Variable
/// length: 4-byte header for an empty list, +6B per equip-switch entry.
/// </summary>
public class ZC_EQUIPSWITCH_LIST : OutgoingPacket
{
    public EquipSwitchEntry[] Entries { get; init; } = Array.Empty<EquipSwitchEntry>();

    public ZC_EQUIPSWITCH_LIST() : base(PacketHeader.ZC_EQUIPSWITCH_LIST, -1) { }

    public override int GetSize() => sizeof(short) + sizeof(short) + Entries.Length * EquipSwitchEntry.SerializedSize;

    public override void Write(BinaryWriter writer)
    {
        foreach (var e in Entries)
        {
            writer.Write(e.Index);
            writer.Write(e.Position);
        }
    }

    public readonly record struct EquipSwitchEntry(short Index, uint Position)
    {
        public const int SerializedSize = sizeof(short) + sizeof(uint); // 6
    }
}
