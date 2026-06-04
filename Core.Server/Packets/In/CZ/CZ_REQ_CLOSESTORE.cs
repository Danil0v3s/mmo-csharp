namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Close the player's vending shop. rAthena <c>clif_parse_CloseVending</c> (clif.cpp, 0x012e). Fixed
/// 2 bytes — header only.
/// </summary>
public class CZ_REQ_CLOSESTORE : IncomingPacket
{
    private const int SIZE = 2;

    public CZ_REQ_CLOSESTORE() : base(PacketHeader.CZ_REQ_CLOSESTORE, SIZE) { }

    public override void Read(BinaryReader reader) { }

    public static CZ_REQ_CLOSESTORE Create(BinaryReader reader) => new();
}
