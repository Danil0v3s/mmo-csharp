namespace Core.Server.Packets.Out.HC;

public class HC_SECOND_PASSWD_LOGIN : OutgoingPacket
{
    private const int SIZE = 12; // packetType (2) + seed (4) + accountId (4) + state (2)

    public uint Seed { get; init; }
    public uint AccountId { get; init; }
    public ushort State { get; init; }

    public HC_SECOND_PASSWD_LOGIN() : base(PacketHeader.HC_SECOND_PASSWD_LOGIN, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Seed);
        writer.Write(AccountId);
        writer.Write(State);
    }
}
