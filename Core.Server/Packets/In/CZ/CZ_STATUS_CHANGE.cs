namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Spend a status point on a stat. rAthena
/// <c>clif_parse_StatusUp</c> (clif.cpp:12690). Wire:
/// <c>00bb &lt;status id&gt;.W &lt;amount&gt;.B</c>. Total: 2 + 2 + 1 = 5 bytes.
///
/// <c>status_id</c> = SP_STR..SP_LUK (13..18 per SpId.cs).
/// <c>amount</c> = how many points to spend (older clients always send 1).
/// </summary>
public class CZ_STATUS_CHANGE : IncomingPacket
{
    private const int SIZE = 5;

    public ushort StatusId { get; private set; }
    public byte Amount { get; private set; }

    public CZ_STATUS_CHANGE() : base(PacketHeader.CZ_STATUS_CHANGE, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        StatusId = reader.ReadUInt16();
        Amount = reader.ReadByte();
    }

    public static CZ_STATUS_CHANGE Create(BinaryReader reader)
    {
        var packet = new CZ_STATUS_CHANGE();
        packet.Read(reader);
        return packet;
    }
}
