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

    // For newer versions
    public ushort StartingJob { get; internal set; }
    public byte Sex { get; internal set; }

    public CH_MAKE_NEW_CHAR(int cmd) : base((PacketHeader)cmd, GetPacketSize(cmd)) // cmd can be CH_MAKE_NEW_CHAR_V3 (0xa39), CH_MAKE_NEW_CHAR_V2 (0x970), or CH_MAKE_NEW_CHAR (0x67)
    {
    }
    
    private static int GetPacketSize(int cmd)
    {
        int size = sizeof(short); // packetType
        if (cmd == 0xa39) // >= 20151001
        {
            size += PacketConstants.NAME_LENGTH + sizeof(byte) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(byte); // name + slot + hairColor + hairStyle + startJob + unknown + sex
        }
        else if (cmd == 0x970) // >= 20120307
        {
            size += PacketConstants.NAME_LENGTH + sizeof(byte) + sizeof(ushort) + sizeof(ushort); // name + slot + hairColor + hairStyle
        }
        else // older versions
        {
            size += PacketConstants.NAME_LENGTH + 6 * sizeof(byte) + sizeof(byte) + 2 * sizeof(ushort); // name + str/agi/vit/int/dex/luk + slot + hairColor + hairStyle
        }
        return size;
    }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type

        int cmd = (int)Header;

        if (cmd == 0xa39) // >= 20151001
        {
            Name = reader.ReadFixedString(PacketConstants.NAME_LENGTH);
            Slot = reader.ReadByte();
            HairColor = reader.ReadUInt16();
            HairStyle = reader.ReadUInt16();
            StartingJob = reader.ReadUInt16();
            reader.ReadUInt16(); // Unknown
            Sex = reader.ReadByte();
        }
        else if (cmd == 0x970) // >= 20120307
        {
            Name = reader.ReadFixedString(PacketConstants.NAME_LENGTH);
            Slot = reader.ReadByte();
            HairColor = reader.ReadUInt16();
            HairStyle = reader.ReadUInt16();
        }
        else if (cmd == 0x67) // older versions
        {
            Name = reader.ReadFixedString(PacketConstants.NAME_LENGTH);
            Str = reader.ReadByte();
            Agi = reader.ReadByte();
            Vit = reader.ReadByte();
            Int = reader.ReadByte();
            Dex = reader.ReadByte();
            Luk = reader.ReadByte();
            Slot = reader.ReadByte();
            HairColor = reader.ReadUInt16();
            HairStyle = reader.ReadUInt16();
        }
    }
}