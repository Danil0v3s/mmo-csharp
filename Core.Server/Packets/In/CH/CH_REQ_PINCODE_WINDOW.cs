namespace Core.Server.Packets.In.CH;

public class CH_REQ_PINCODE_WINDOW : IncomingPacket
{
    public uint AccountId { get; internal set; }

    public CH_REQ_PINCODE_WINDOW() : base(PacketHeader.CH_REQ_PINCODE_WINDOW, true) { }

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