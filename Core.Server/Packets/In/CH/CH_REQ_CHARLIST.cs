namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHARLIST : IncomingPacket
{
    private const int SIZE = 2; // packetType only
    
    public CH_REQ_CHARLIST() : base(PacketHeader.CH_REQ_CHARLIST, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
    }
}