namespace Core.Server.Packets;

public class CharacterBlockInfo
{
    public uint GID { get; internal set; }
    public string ExpireDate { get; internal set; } = string.Empty;

    public CharacterBlockInfo()
    {
        ExpireDate = new string('\0', 20);
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(GID);
        writer.WriteFixedString(ExpireDate, 20);
    }

    public int GetSize()
    {
        return sizeof(uint) + 20; // GID + szExpireDate[20]
    }
}