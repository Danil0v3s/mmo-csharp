namespace Core.Server.Packets.Out.HC;

public class HC_NOTIFY_ACCESSIBLE_MAPNAME_SUB
{
    public int Status { get; internal set; }
    public string Map { get; internal set; } = string.Empty;

    public void Read(BinaryReader reader)
    {
        Status = reader.ReadInt32();
        Map = reader.ReadFixedString(PacketConstants.MAP_NAME_LENGTH_EXT);
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Status);
        writer.WriteFixedString(Map, PacketConstants.MAP_NAME_LENGTH_EXT);
    }

    public int GetSize()
    {
        return sizeof(int) + PacketConstants.MAP_NAME_LENGTH_EXT; // Status + Map[MAP_NAME_LENGTH_EXT]
    }
}