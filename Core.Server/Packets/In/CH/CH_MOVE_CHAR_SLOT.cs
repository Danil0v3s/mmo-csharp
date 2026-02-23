namespace Core.Server.Packets.In.CH;

public class CH_MOVE_CHAR_SLOT : IncomingPacket
{
    private const int SIZE = 8; // packetType + from + to + remaining
    
    public ushort From { get; internal set; }
    public ushort To { get; internal set; }
    public ushort Remaining { get; internal set; }

    public CH_MOVE_CHAR_SLOT() : base(PacketHeader.CH_MOVE_CHAR_SLOT, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        From = reader.ReadUInt16();
        To = reader.ReadUInt16();
        Remaining = reader.ReadUInt16();
    }
}
