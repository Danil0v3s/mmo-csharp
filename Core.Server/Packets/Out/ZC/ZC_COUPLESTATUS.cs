namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_couplestatus</c> ([clif.cpp:3605]). Per-stat broadcast
/// that carries both the base value and the equipment/buff plus value.
/// Used for SP_STR..LUK + renewal SP_POW..CRT. Fixed 14 bytes:
/// <c>0x0141 packet_id (2) + statusType (4) + base (4) + plus (4)</c>.
/// </summary>
public class ZC_COUPLESTATUS : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(int) + sizeof(int) + sizeof(int);

    public uint StatusType { get; init; }
    public int BaseStatus { get; init; }
    public int PlusStatus { get; init; }

    public ZC_COUPLESTATUS() : base(PacketHeader.ZC_COUPLESTATUS, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(StatusType);
        writer.Write(BaseStatus);
        writer.Write(PlusStatus);
    }
}
