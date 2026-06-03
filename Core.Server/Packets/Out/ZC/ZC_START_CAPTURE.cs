namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Open the taming targeting cursor. rAthena <c>clif_catch_process</c> (clif.cpp, 0x019e). Header only
/// (2 bytes, no body): sent after a taming item arms the catch so the client lets the player click a
/// monster, which replies with <see cref="In.CZ.CZ_TRYCAPTURE_MONSTER"/>.
/// </summary>
public class ZC_START_CAPTURE : OutgoingPacket
{
    private const int SIZE = 2;

    public ZC_START_CAPTURE() : base(PacketHeader.ZC_START_CAPTURE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        // Header-only packet — base already wrote the 2-byte id.
    }
}
