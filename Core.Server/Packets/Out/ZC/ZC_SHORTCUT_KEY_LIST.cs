namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_hotkeys_send</c> ([clif.cpp:10891] / [packets_struct.hpp:1584])
/// for PACKETVER_MAIN_NUM ≥ 20190522. Fixed 271 bytes:
///
/// <code>
///   0x0b20 packet_id (2) + rotate (1) + tab (2) + 38× hotkey_data
///   hotkey_data = isSkill (1) + id (4) + count (2)   // 7 bytes packed
/// </code>
///
/// Two tabs are sent (tab 0 and tab 1) on first login.
/// </summary>
public class ZC_SHORTCUT_KEY_LIST : OutgoingPacket
{
    public const int MaxHotkeys = 38;
    private const int HotkeyDataSize = sizeof(byte) + sizeof(uint) + sizeof(short); // 7
    private const int SIZE = sizeof(short) + sizeof(byte) + sizeof(short) + MaxHotkeys * HotkeyDataSize; // 271

    public byte Rotate { get; init; }
    public short Tab { get; init; }
    public Hotkey[] Hotkeys { get; init; } = Array.Empty<Hotkey>();

    public ZC_SHORTCUT_KEY_LIST() : base(PacketHeader.ZC_SHORTCUT_KEY_LIST, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Rotate);
        writer.Write(Tab);
        for (var i = 0; i < MaxHotkeys; i++)
        {
            var hk = i < Hotkeys.Length ? Hotkeys[i] : default;
            writer.Write(hk.IsSkill);
            writer.Write(hk.Id);
            writer.Write(hk.Count);
        }
    }

    public readonly record struct Hotkey(byte IsSkill, uint Id, short Count);
}
