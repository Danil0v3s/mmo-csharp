namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Client closed the storage window. rAthena
/// <c>clif_parse_CloseKafra</c> (clif.cpp:13703). Wire: <c>00f7</c>
/// header only — 2 bytes.
/// </summary>
public class CZ_CLOSE_STORE : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_CLOSE_STORE() : base(PacketHeader.CZ_CLOSE_STORE, SIZE) { }

    public override void Read(BinaryReader reader) { }

    public static CZ_CLOSE_STORE Create(BinaryReader reader) => new();
}
