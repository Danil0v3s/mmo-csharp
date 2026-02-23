namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHANGE_CHARNAME : IncomingPacket
{
    private const int SIZE = 30; // packetType (2) + charId (4) + newName (24)
    
    public uint CharId { get; internal set; }
    public string NewName { get; internal set; } = string.Empty;

    public CH_REQ_CHANGE_CHARNAME() : base(PacketHeader.CH_REQ_CHANGE_CHARNAME, SIZE)
    {
        NewName = new string('\0', PacketConstants.NAME_LENGTH);
    }

    public override void Read(BinaryReader reader)
    {
        CharId = reader.ReadUInt32();
        NewName = reader.ReadFixedString(PacketConstants.NAME_LENGTH);
    }
}
