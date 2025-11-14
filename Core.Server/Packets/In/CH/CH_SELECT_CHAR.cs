namespace Core.Server.Packets.In.CH;

public class CH_SELECT_CHAR : IncomingPacket
{
    private const int SIZE = 3; // packetType + slot
    
    public byte Slot { get; internal set; }

    public CH_SELECT_CHAR() : base(PacketHeader.CH_SELECT_CHAR, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        Slot = reader.ReadByte();
    }
}