namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Tells the freshly-connected client its account id. rAthena
/// <c>pc_setnewpc</c> sends this immediately after <c>WantToConnection</c>
/// succeeds. Shape: 0x0283 packet_id (2) + account_id (4) = 6 bytes.
/// </summary>
public class ZC_AID : OutgoingPacket
{
    private const int SIZE = 6;

    public int AccountId { get; init; }

    public ZC_AID() : base(PacketHeader.ZC_AID, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
    }
}
