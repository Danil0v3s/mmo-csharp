namespace Core.Server.Packets.Out.HC;

public class HC_BLOCK_CHARACTER : OutgoingPacket
{
    public CharacterBlockInfo[] BlockInfo { get; init; } = Array.Empty<CharacterBlockInfo>();

    public HC_BLOCK_CHARACTER() : base(PacketHeader.HC_BLOCK_CHARACTER, -1) { }

    public override void Write(BinaryWriter writer)
    {
        foreach (var info in BlockInfo)
        {
            info.Write(writer);
        }
    }

    public override int GetSize()
    {
        int size = sizeof(short) + sizeof(short); // packetType + packetLength
        foreach (var info in BlockInfo)
        {
            size += info.GetSize();
        }
        return size;
    }
}
