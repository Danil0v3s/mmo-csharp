namespace Core.Server.Packets.In.CH;

public class CH_MOVE_CHAR_SLOT : IncomingPacket
{
    public ushort From { get; internal set; }
    public ushort To { get; internal set; }

    public CH_MOVE_CHAR_SLOT() : base(PacketHeader.CH_MOVE_CHAR_SLOT, true) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        From = reader.ReadUInt16();
        To = reader.ReadUInt16();
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(ushort) + sizeof(ushort); // packetType + from + to
    }
}