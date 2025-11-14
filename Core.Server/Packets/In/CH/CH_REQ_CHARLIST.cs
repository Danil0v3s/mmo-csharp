namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHARLIST : IncomingPacket
{
    public CH_REQ_CHARLIST() : base(PacketHeader.CH_REQ_CHARLIST, true) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
    }

    public override int GetSize()
    {
        return sizeof(short); // packetType only
    }
}