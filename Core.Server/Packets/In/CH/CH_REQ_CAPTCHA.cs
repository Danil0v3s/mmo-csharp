namespace Core.Server.Packets.In.CH;

public class CH_REQ_CAPTCHA : IncomingPacket
{
    private const int SIZE = 8; // packetType (2) + unknown (2) + accountId (4)
    
    public ushort Unknown { get; internal set; }
    public uint AccountId { get; internal set; }

    public CH_REQ_CAPTCHA() : base(PacketHeader.CH_REQ_CAPTCHA, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        Unknown = reader.ReadUInt16();
        AccountId = reader.ReadUInt32();
    }
}