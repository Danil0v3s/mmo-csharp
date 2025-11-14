namespace Core.Server.Packets.In.CH;

public class CH_KEEP_ALIVE : IncomingPacket
{
    private const int SIZE = 6; // packetType (2) + accountId (4)
    
    public uint AccountId { get; internal set; }

    public CH_KEEP_ALIVE() : base(PacketHeader.CH_KEEP_ALIVE, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        AccountId = reader.ReadUInt32();
    }
}