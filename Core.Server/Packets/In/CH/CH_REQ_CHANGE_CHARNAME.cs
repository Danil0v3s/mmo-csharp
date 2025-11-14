namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHANGE_CHARNAME : IncomingPacket
{
    public uint CharId { get; internal set; }

    public CH_REQ_CHANGE_CHARNAME() : base(PacketHeader.CH_REQ_CHANGE_CHARNAME, true) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        CharId = reader.ReadUInt32();
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(uint); // packetType + charId
    }
}