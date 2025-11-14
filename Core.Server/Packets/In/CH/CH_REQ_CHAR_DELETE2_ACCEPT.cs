namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHAR_DELETE2_ACCEPT : IncomingPacket
{
    private const int SIZE = 12; // packetType (2) + charId (4) + birthDate (6)
    
    public uint CharId { get; internal set; }
    public byte[] BirthDate { get; internal set; } = new byte[6];

    public CH_REQ_CHAR_DELETE2_ACCEPT() : base(PacketHeader.CH_REQ_CHAR_DELETE2_ACCEPT, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        CharId = reader.ReadUInt32();

        // Read birth date (6 bytes: YY MM DD)
        for (int i = 0; i < 6; i++)
        {
            BirthDate[i] = reader.ReadByte();
        }
    }
}