namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Play a cosmetic effect on the named entity." rAthena
/// <c>clif_specialeffect</c> / <c>clif_specialeffect_value</c>
/// (packet name <c>ZC_NOTIFY_EFFECT2</c>, opcode 0x01f3).
///
/// Shape: 0x01f3 packet_id (2) + entityId (4) + effectId (4) = 10 bytes.
///
/// <para>Used by item scripts via <c>specialeffect EF_*</c> and
/// <c>specialeffect2 EF_*</c>:</para>
/// <list type="bullet">
///   <item><c>specialeffect</c> targets the affected entity's id and
///   broadcasts to everyone in AOI (including the entity itself).</item>
///   <item><c>specialeffect2</c> targets the script invoker; rAthena
///   sends to the AOI-or-self per the trigger context. Our port
///   collapses both to the same packet — the dispatcher decides
///   AOI vs self based on <see cref="Map.Server.Visibility.SendTarget"/>.</item>
/// </list>
///
/// Effect IDs come from rAthena's <c>effect_list.txt</c> (~600
/// numeric ids); we don't enumerate them in C# since the client is
/// the consumer.
/// </summary>
public class ZC_NOTIFY_EFFECT2 : OutgoingPacket
{
    private const int SIZE = 10;

    public int EntityId { get; init; }
    public int EffectId { get; init; }

    public ZC_NOTIFY_EFFECT2() : base(PacketHeader.ZC_NOTIFY_EFFECT2, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(EffectId);
    }
}
