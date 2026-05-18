namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "OK to quit" / "Wait, can't disconnect yet". rAthena
/// <c>clif_disconnect_ack</c>. Fixed 4 bytes: 0x018b packet_id (2) + result (2).
///
/// <list type="bullet">
///   <item><see cref="Result"/> = 0 — disconnect accepted; client closes the
///     TCP and returns to login. Server does NOT close proactively — see the
///     handler comment in <c>ReqQuitHandler</c>.</item>
///   <item><see cref="Result"/> = 1 — refused (e.g. combat lockout). Client
///     stays connected and shows an "unable to disconnect" message.</item>
/// </list>
/// </summary>
public class ZC_DISCONNECT_ACK : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(ushort);

    public ushort Result { get; init; }

    public ZC_DISCONNECT_ACK() : base(PacketHeader.ZC_DISCONNECT_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Result);
    }
}
