namespace Core.Server.Packets.In.CH;

public class CH_MAKE_NEW_CHAR : IncomingPacket
{
    public string Name { get; internal set; } = string.Empty;
    public byte Str { get; internal set; }
    public byte Agi { get; internal set; }
    public byte Vit { get; internal set; }
    public byte Int { get; internal set; }
    public byte Dex { get; internal set; }
    public byte Luk { get; internal set; }
    public byte Slot { get; internal set; }
    public ushort HairColor { get; internal set; }
    public ushort HairStyle { get; internal set; }

    // PACKETVER 20220406
    public uint StartingJob { get; internal set; }
    public byte Sex { get; internal set; }

    private const int SIZE = 36; // packetType (2) + name (24) + slot (1) + hairColor (2) + hairStyle (2) + job (4) + sex (1)

    public CH_MAKE_NEW_CHAR() : base(PacketHeader.CH_MAKE_NEW_CHAR, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Name = reader.ReadFixedString(PacketConstants.NAME_LENGTH);
        Slot = reader.ReadByte();
        HairColor = reader.ReadUInt16();
        HairStyle = reader.ReadUInt16();
        StartingJob = reader.ReadUInt32();
        Sex = reader.ReadByte();
    }
}
