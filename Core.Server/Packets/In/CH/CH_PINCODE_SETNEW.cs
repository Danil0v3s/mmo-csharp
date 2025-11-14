namespace Core.Server.Packets.In.CH;

public class CH_PINCODE_SETNEW : IncomingPacket
{
    public uint AccountId { get; internal set; }
    public string NewPin { get; internal set; } = string.Empty;

    private const int SIZE = 10; // packetType (2) + accountId (4) + newpin (4)
    
    public CH_PINCODE_SETNEW() : base(PacketHeader.CH_PINCODE_SETNEW, SIZE)
    {
        NewPin = new string('\0', 4);
    }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        AccountId = reader.ReadUInt32();

        // Read new PIN (typically 4 characters)
        NewPin = reader.ReadFixedString(4);
    }
}