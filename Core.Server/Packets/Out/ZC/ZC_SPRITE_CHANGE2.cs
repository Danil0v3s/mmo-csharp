namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_sprite_change</c> [clif.cpp:3929] sent as the modern
/// "<c>sendLookType</c>" variant ([packets_struct.hpp:317]) for
/// PACKETVER ≥ 20181121 / PACKETVER_RE ≥ 20180704. Fixed 15 bytes:
///
/// <code>
///   0x01d7 packet_id (2) + AID (4) + type (1) + val (4) + val2 (4)
/// </code>
///
/// <c>type</c> uses the <c>_look</c> enum ([map.hpp:585]): BASE=0,
/// HAIR=1, WEAPON=2, HEAD_BOTTOM=3, ..., BODY2=14.
/// </summary>
public class ZC_SPRITE_CHANGE2 : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint) + sizeof(byte) + sizeof(uint) + sizeof(uint);

    public uint AccountId { get; init; }
    public byte LookType { get; init; }
    public uint Value { get; init; }
    public uint Value2 { get; init; }

    public ZC_SPRITE_CHANGE2() : base(PacketHeader.ZC_SPRITE_CHANGE2, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.Write(LookType);
        writer.Write(Value);
        writer.Write(Value2);
    }
}
