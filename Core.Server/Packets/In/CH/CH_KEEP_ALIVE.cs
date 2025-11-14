namespace Core.Server.Packets.In.CH;

public class CH_KEEP_ALIVE : IncomingPacket
{
    public uint AccountId { get; internal set; }

    public CH_KEEP_ALIVE() : base(PacketHeader.CH_KEEP_ALIVE, true) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        AccountId = reader.ReadUInt32();
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(uint); // packetType + accountId
    }
}