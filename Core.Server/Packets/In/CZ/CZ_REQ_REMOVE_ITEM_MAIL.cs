namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: unstage an item from the mail draft. rAthena <c>clif_parse_Mail_winopen</c>
/// (clif.cpp:16763) + <c>PACKET_CZ_REMOVE_ITEM_MAIL</c>. Wire: <c>0a06 &lt;index&gt;.W &lt;amount&gt;.W</c> — 6
/// bytes. <see cref="ClientIndex"/> is the client slot (server_index = client_index − 2).
/// </summary>
public class CZ_REQ_REMOVE_ITEM_MAIL : IncomingPacket
{
    private const int SIZE = 6;

    public ushort ClientIndex { get; private set; }
    public short Amount { get; private set; }

    public CZ_REQ_REMOVE_ITEM_MAIL() : base(PacketHeader.CZ_REQ_REMOVE_ITEM_MAIL, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ClientIndex = reader.ReadUInt16();
        Amount = reader.ReadInt16();
    }

    public static CZ_REQ_REMOVE_ITEM_MAIL Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_REMOVE_ITEM_MAIL();
        packet.Read(reader);
        return packet;
    }
}
