namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHAR_DELETE2_CANCEL : IncomingPacket
{
    public uint CharId { get; internal set; }

    public CH_REQ_CHAR_DELETE2_CANCEL() : base(PacketHeader.CH_REQ_CHAR_DELETE2_CANCEL, true) { }

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