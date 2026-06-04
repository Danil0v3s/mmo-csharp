namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_change_title_ack</c> ([clif.cpp:20698], 0x0a2f). Result of a title-change request.
/// Fixed 7 bytes: <c>0a2f &lt;result&gt;.B &lt;title_id&gt;.L</c>. <c>result == 0</c> = applied,
/// <c>result == 1</c> = the player does not own that title.
/// </summary>
public class ZC_ACK_CHANGE_TITLE : OutgoingPacket
{
    private const int SIZE = 7;

    public byte Result { get; init; }
    public int TitleId { get; init; }

    public ZC_ACK_CHANGE_TITLE() : base(PacketHeader.ZC_ACK_CHANGE_TITLE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Result);
        writer.Write(TitleId);
    }
}
