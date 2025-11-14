namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHAR_DELETE2_CANCEL : IncomingPacket
{
    private const int SIZE = 6; // packetType (2) + charId (4)
    
    public uint CharId { get; internal set; }

    public CH_REQ_CHAR_DELETE2_CANCEL() : base(PacketHeader.CH_REQ_CHAR_DELETE2_CANCEL, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        CharId = reader.ReadUInt32();
    }
}