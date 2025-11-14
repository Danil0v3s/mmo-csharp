namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHANGE_CHARNAME : IncomingPacket
{
    private const int SIZE = 6; // packetType (2) + charId (4)
    
    public uint CharId { get; internal set; }

    public CH_REQ_CHANGE_CHARNAME() : base(PacketHeader.CH_REQ_CHANGE_CHARNAME, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        CharId = reader.ReadUInt32();
    }
}