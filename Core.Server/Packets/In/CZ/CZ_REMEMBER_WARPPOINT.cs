namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Remember this warp point" — rAthena <c>clif_parse_RequestMemo</c>
/// (clif.cpp, <c>PACKET_CZ_REMEMBER_WARPPOINT</c> 0x011d). Header-only (2 bytes,
/// no body); the client sends it when the player presses the Warp Portal "memo"
/// button. The server routes it to <c>pc_memo(sd, -1)</c>, which (level/mapflag
/// gates permitting) inserts the current cell at memo slot 0.
/// </summary>
public class CZ_REMEMBER_WARPPOINT : IncomingPacket
{
    private const int SIZE = 2; // header only

    public CZ_REMEMBER_WARPPOINT() : base(PacketHeader.CZ_REMEMBER_WARPPOINT, SIZE) { }

    public override void Read(BinaryReader reader) { /* no body */ }

    public static CZ_REMEMBER_WARPPOINT Create(BinaryReader reader)
    {
        var packet = new CZ_REMEMBER_WARPPOINT();
        packet.Read(reader);
        return packet;
    }
}
