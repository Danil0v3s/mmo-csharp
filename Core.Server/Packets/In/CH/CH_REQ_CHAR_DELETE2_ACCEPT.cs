namespace Core.Server.Packets.In.CH;

public class CH_REQ_CHAR_DELETE2_ACCEPT : IncomingPacket
{
    public uint CharId { get; internal set; }
    public byte[] BirthDate { get; internal set; } = new byte[6];

    public CH_REQ_CHAR_DELETE2_ACCEPT() : base(PacketHeader.CH_REQ_CHAR_DELETE2_ACCEPT, true) { }

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

    public override int GetSize()
    {
        return sizeof(short) + sizeof(uint) + 6; // packetType + charId + birthDate[6]
    }
}