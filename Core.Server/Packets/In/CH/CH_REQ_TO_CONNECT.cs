namespace Core.Server.Packets.In.CH;

public class CH_REQ_TO_CONNECT : IncomingPacket
{
    public uint AccountId { get; internal set; }
    public uint LoginId1 { get; internal set; }
    public uint LoginId2 { get; internal set; }
    public byte Sex { get; internal set; }

    public CH_REQ_TO_CONNECT() : base(PacketHeader.CH_REQ_TO_CONNECT, true) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        AccountId = reader.ReadUInt32();
        LoginId1 = reader.ReadUInt32();
        LoginId2 = reader.ReadUInt32();
        Sex = reader.ReadByte();
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(byte); // packetType + accountId + loginId1 + loginId2 + sex
    }
}