namespace Core.Server.Packets.In.CH;

public class CH_REQ_CAPTCHA : IncomingPacket
{
    public ushort Unknown { get; internal set; }
    public uint AccountId { get; internal set; }

    public CH_REQ_CAPTCHA() : base(PacketHeader.CH_REQ_CAPTCHA, true) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        Unknown = reader.ReadUInt16();
        AccountId = reader.ReadUInt32();
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(ushort) + sizeof(uint); // packetType + unknown + accountId
    }
}