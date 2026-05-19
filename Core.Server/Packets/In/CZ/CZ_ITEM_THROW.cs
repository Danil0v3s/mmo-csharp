namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player drops an inventory item to the floor. rAthena
/// <c>clif_parse_DropItem</c> (clif.cpp:12016) → <c>pc_dropitem</c>.
/// Wire: <c>00a2 &lt;index&gt;.W &lt;amount&gt;.W</c> — total 6 bytes.
/// <see cref="ClientIndex"/> is the client-side slot
/// (server_index = client_index − 2).
/// </summary>
public class CZ_ITEM_THROW : IncomingPacket
{
    private const int SIZE = 6;

    public ushort ClientIndex { get; private set; }
    public ushort Amount { get; private set; }

    public CZ_ITEM_THROW() : base(PacketHeader.CZ_ITEM_THROW, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ClientIndex = reader.ReadUInt16();
        Amount = reader.ReadUInt16();
    }

    public static CZ_ITEM_THROW Create(BinaryReader reader)
    {
        var packet = new CZ_ITEM_THROW();
        packet.Read(reader);
        return packet;
    }
}
