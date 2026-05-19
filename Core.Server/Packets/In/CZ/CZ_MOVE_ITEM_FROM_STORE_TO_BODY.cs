namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Take a storage item back into inventory. rAthena
/// <c>clif_parse_MoveFromKafra</c> (clif.cpp:13627). Wire:
/// <c>00f5 &lt;index&gt;.W &lt;amount&gt;.L</c> — total 8 bytes.
///
/// Note: storage uses a 1-based client_index (server_index = client_index − 1),
/// distinct from inventory's −2 offset. rAthena's <c>server_storage_index</c>.
/// </summary>
public class CZ_MOVE_ITEM_FROM_STORE_TO_BODY : IncomingPacket
{
    private const int SIZE = 8;

    public ushort ClientIndex { get; private set; }
    public int Amount { get; private set; }

    public CZ_MOVE_ITEM_FROM_STORE_TO_BODY()
        : base(PacketHeader.CZ_MOVE_ITEM_FROM_STORE_TO_BODY, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ClientIndex = reader.ReadUInt16();
        Amount = reader.ReadInt32();
    }

    public static CZ_MOVE_ITEM_FROM_STORE_TO_BODY Create(BinaryReader reader)
    {
        var packet = new CZ_MOVE_ITEM_FROM_STORE_TO_BODY();
        packet.Read(reader);
        return packet;
    }
}
