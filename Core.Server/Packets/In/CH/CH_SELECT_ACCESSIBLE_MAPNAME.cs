namespace Core.Server.Packets.In.CH;

public class CH_SELECT_ACCESSIBLE_MAPNAME : IncomingPacket
{
    public short PacketType { get; internal set; }
    public sbyte Slot { get; internal set; }
    public sbyte MapNumber { get; internal set; }

    public CH_SELECT_ACCESSIBLE_MAPNAME() : base(PacketHeader.CH_SELECT_ACCESSIBLE_MAPNAME, true) { }

    public override void Read(BinaryReader reader)
    {
        PacketType = reader.ReadInt16();
        Slot = reader.ReadSByte();
        MapNumber = reader.ReadSByte();
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(sbyte) + sizeof(sbyte); // packetType + slot + mapnumber
    }
}