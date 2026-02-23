namespace Core.Server.Packets.In.CH;

public class CH_PINCODE_CHECK : IncomingPacket
{
    public uint AccountId { get; internal set; }
    public string PinCode { get; internal set; } = string.Empty;

    private const int SIZE = 10; // packetType (2) + accountId (4) + pincode (4)
    
    public CH_PINCODE_CHECK() : base(PacketHeader.CH_PINCODE_CHECK, SIZE)
    {
        PinCode = new string('\0', 4); // Assuming PINCODE_LENGTH is 4 based on context
    }

    public override void Read(BinaryReader reader)
    {
        AccountId = reader.ReadUInt32();

        // Read PIN code (typically 4 characters)
        PinCode = reader.ReadFixedString(4);
    }
}
