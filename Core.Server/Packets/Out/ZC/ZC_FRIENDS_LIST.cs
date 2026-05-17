namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_friendslist_send</c> (clif.cpp:15332). Emitted as part
/// of <c>pc_authok</c> right after <c>ZC_ACCEPT_ENTER</c>. Variable length:
///
/// <code>
///   int16  packetType  2
///   int16  packetLen   2
///   { int32 AID, int32 CID, char[24] name }*   (32 B per friend)
/// </code>
///
/// For a freshly created character with no friends the body is empty —
/// just header + length = 4 bytes on the wire.
/// </summary>
public class ZC_FRIENDS_LIST : OutgoingPacket
{
    public IReadOnlyList<Friend> Friends { get; init; } = Array.Empty<Friend>();

    public ZC_FRIENDS_LIST() : base(PacketHeader.ZC_FRIENDS_LIST, -1) { }

    public override int GetSize() => sizeof(short) + sizeof(short) + Friends.Count * Friend.SerializedSize;

    public override void Write(BinaryWriter writer)
    {
        foreach (var f in Friends)
        {
            writer.Write(f.AccountId);
            writer.Write(f.CharacterId);
            writer.WriteFixedString(f.Name, Friend.NameLength);
        }
    }

    public sealed record Friend(int AccountId, int CharacterId, string Name)
    {
        public const int NameLength = 24;
        public const int SerializedSize = sizeof(int) + sizeof(int) + NameLength; // 32
    }
}
