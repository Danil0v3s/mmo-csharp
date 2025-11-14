namespace Core.Server.Packets.In.CH;

public class CH_SELECT_ACCESSIBLE_MAPNAME : IncomingPacket
{
    private const int SIZE = 4; // packetType (2) + slot (1) + mapnumber (1)
    
    public short PacketType { get; internal set; }
    public sbyte Slot { get; internal set; }
    public sbyte MapNumber { get; internal set; }

    public CH_SELECT_ACCESSIBLE_MAPNAME() : base(PacketHeader.CH_SELECT_ACCESSIBLE_MAPNAME, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        PacketType = reader.ReadInt16();
        Slot = reader.ReadSByte();
        MapNumber = reader.ReadSByte();
    }
}