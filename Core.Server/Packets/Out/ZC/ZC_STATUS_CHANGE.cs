namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_zc_status_change</c> ([clif.cpp:3568]). Used by
/// <c>clif_initialstatus</c> for the renewal <c>SP_USTR..SP_UCRT</c>
/// need-points fields. Fixed 5 bytes:
/// <c>0x00be packet_id (2) + statusId (2) + value (1)</c>.
/// </summary>
public class ZC_STATUS_CHANGE : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(short) + sizeof(byte);

    public ushort StatusId { get; init; }
    public byte Value { get; init; }

    public ZC_STATUS_CHANGE() : base(PacketHeader.ZC_STATUS_CHANGE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(StatusId);
        writer.Write(Value);
    }
}
