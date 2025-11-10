namespace Core.Server.Packets.ServerPackets;

/// <summary>
/// Example 3: Fixed-length outgoing packet.
/// Server sends this to client when login is successful.
/// Packet structure: [Header (2)] [SessionToken (4)] [CharacterSlots (1)]
/// Total size: 7 bytes
/// </summary>
[PacketVersion(1)]
public class AC_ACCEPT_LOGIN : OutgoingPacket
{
    public int SessionToken { get; init; }
    public byte CharacterSlots { get; init; }
    
    public AC_ACCEPT_LOGIN() : base(PacketHeader.AC_ACCEPT_LOGIN, isFixedLength: true)
    {
    }
    
    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);  // 2 bytes
        writer.Write(SessionToken);   // 4 bytes
        writer.Write(CharacterSlots); // 1 byte
    }
    
    public override int GetSize() => 7; // header + body
}

