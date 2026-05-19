namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_changeoption_target</c> ([clif.cpp:5040]) modern
/// variant — <c>PACKET_ZC_STATE_CHANGE</c> with the 4-byte effect
/// state. Used by every PACKETVER ≥ 20070821 (so our DHXJ 20220401
/// target ships this).
///
/// Fixed 15 bytes:
/// <code>
///   0x0229 packet_id (2) + AID (4) + bodyState (2) + healthState (2)
///        + effectState (4) + pkMode (1)
/// </code>
///
/// <para><b>Field meanings</b></para>
/// <list type="bullet">
///   <item><c>bodyState</c> = OPT1 (stone/freeze/stun/sleep).</item>
///   <item><c>healthState</c> = OPT2 (poison/curse/silence).</item>
///   <item><c>effectState</c> = OPTION bitmask (cart/falcon/riding/...).</item>
///   <item><c>pkMode</c> = karma flag.</item>
/// </list>
/// </summary>
public class ZC_STATE_CHANGE3 : OutgoingPacket
{
    private const int SIZE =
        sizeof(short) + sizeof(uint) + sizeof(short) + sizeof(short) + sizeof(uint) + sizeof(byte);

    public uint AccountId { get; init; }
    public ushort BodyState { get; init; }
    public ushort HealthState { get; init; }
    public uint EffectState { get; init; }
    public byte PkMode { get; init; }

    public ZC_STATE_CHANGE3() : base(PacketHeader.ZC_STATE_CHANGE3, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.Write(BodyState);
        writer.Write(HealthState);
        writer.Write(EffectState);
        writer.Write(PkMode);
    }
}
