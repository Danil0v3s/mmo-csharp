namespace Core.Server.Packets.In.CH;

public class CH_REQ_IS_VALID_CHARNAME : IncomingPacket
{
    public uint AccountId { get; internal set; }
    public uint CharId { get; internal set; }
    public string NewName { get; internal set; } = string.Empty;

    private const int SIZE = 34; // packetType (2) + accountId (4) + charId (4) + newName (24)
    
    public CH_REQ_IS_VALID_CHARNAME() : base(PacketHeader.CH_REQ_IS_VALID_CHARNAME, SIZE)
    {
        NewName = new string('\0', PacketConstants.NAME_LENGTH);
    }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        AccountId = reader.ReadUInt32();
        CharId = reader.ReadUInt32();
        NewName = reader.ReadFixedString(PacketConstants.NAME_LENGTH);
    }
}