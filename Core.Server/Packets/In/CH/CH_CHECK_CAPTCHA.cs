namespace Core.Server.Packets.In.CH;

public class CH_CHECK_CAPTCHA : IncomingPacket
{
    private const int SIZE = 32; // packetType (2) + length (2) + accountId (4) + code (10) + unknown (14)
    
    public ushort Length { get; internal set; }
    public uint AccountId { get; internal set; }
    public byte[] Code { get; internal set; } = new byte[10];
    public byte[] Unknown { get; internal set; } = new byte[14];

    public CH_CHECK_CAPTCHA() : base(PacketHeader.CH_CHECK_CAPTCHA, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        Length = reader.ReadUInt16();
        AccountId = reader.ReadUInt32();

        // Read code (10 bytes)
        Code = reader.ReadBytes(10);

        // Read unknown (14 bytes)
        Unknown = reader.ReadBytes(14);
    }
}