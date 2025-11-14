namespace Core.Server.Packets.In.CH;

public class CH_REQ_TO_CONNECT : IncomingPacket
{
    private const int SIZE = 15; // packetType (2) + accountId (4) + loginId1 (4) + loginId2 (4) + sex (1)
    
    public uint AccountId { get; internal set; }
    public uint LoginId1 { get; internal set; }
    public uint LoginId2 { get; internal set; }
    public byte Sex { get; internal set; }

    public CH_REQ_TO_CONNECT() : base(PacketHeader.CH_REQ_TO_CONNECT, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        AccountId = reader.ReadUInt32();
        LoginId1 = reader.ReadUInt32();
        LoginId2 = reader.ReadUInt32();
        Sex = reader.ReadByte();
    }
}