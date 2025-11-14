namespace Core.Server.Packets.Out.HC;

public class HC_ACK_CHANGE_CHARACTER_SLOT : OutgoingPacket
{
    public short Reason { get; init; }
    public short CharMoves { get; init; }

    public HC_ACK_CHANGE_CHARACTER_SLOT() : base(PacketHeader.HC_ACK_CHANGE_CHARACTER_SLOT, true) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write((short)8); // Fixed length: 2 (header) + 2 (length) + 2 (reason) + 2 (charMoves) = 8
        writer.Write(Reason);
        writer.Write(CharMoves);
    }

    public override int GetSize()
    {
        return 8;
    }
}