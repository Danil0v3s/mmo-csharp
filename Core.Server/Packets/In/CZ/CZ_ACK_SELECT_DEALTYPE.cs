namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player chose Buy or Sell on an NPC's buy/sell dialog.
/// rAthena <c>clif_parse_NpcBuySellSelected</c> (clif.cpp:12230). Wire:
/// <c>00c5 &lt;npc_id&gt;.L &lt;type&gt;.B</c> — total 7 bytes.
/// type: 0 = buy, 1 = sell.
/// </summary>
public class CZ_ACK_SELECT_DEALTYPE : IncomingPacket
{
    private const int SIZE = 7;

    public int NpcId { get; private set; }
    public byte DealType { get; private set; }

    public CZ_ACK_SELECT_DEALTYPE() : base(PacketHeader.CZ_ACK_SELECT_DEALTYPE, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        NpcId = reader.ReadInt32();
        DealType = reader.ReadByte();
    }

    public static CZ_ACK_SELECT_DEALTYPE Create(BinaryReader reader)
    {
        var packet = new CZ_ACK_SELECT_DEALTYPE();
        packet.Read(reader);
        return packet;
    }
}
