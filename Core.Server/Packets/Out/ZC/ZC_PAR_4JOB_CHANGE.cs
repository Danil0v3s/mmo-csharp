namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena 4-job stat coupled change ([packets_struct.hpp:347], gated
/// behind PACKETVER_MAIN_NUM ≥ 20200916 || PACKETVER_RE_NUM ≥ 20200723).
/// Same shape as <see cref="ZC_COUPLESTATUS"/> but routed to the 4-job
/// UI for SP_POW..CRT. Fixed 14 bytes:
/// <c>0x0b25 packet_id (2) + varId (4) + base (4) + plus (4)</c>.
/// </summary>
public class ZC_PAR_4JOB_CHANGE : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint) + sizeof(int) + sizeof(int);

    public uint VarId { get; init; }
    public int BaseStatus { get; init; }
    public int PlusStatus { get; init; }

    public ZC_PAR_4JOB_CHANGE() : base(PacketHeader.ZC_PAR_4JOB_CHANGE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(VarId);
        writer.Write(BaseStatus);
        writer.Write(PlusStatus);
    }
}
