namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_initialstatus</c>'s primary packet ([clif.cpp:4111],
/// struct at [packets.hpp:851]). The character's full base/battle status
/// snapshot sent once on first map login. Fixed 44 bytes:
///
/// <code>
///   0x00bd packet_id (2)
///   point (2)                — remaining status points
///   str (1) + standardStr (1)
///   agi (1) + standardAgi (1)
///   vit (1) + standardVit (1)
///   int (1) + standardInt (1)
///   dex (1) + standardDex (1)
///   luk (1) + standardLuk (1)
///   attPower (2) refiningPower (2)
///   max_mattPower (2) min_mattPower (2)
///   itemdefPower (2) plusdefPower (2)
///   mdefPower (2) plusmdefPower (2)
///   hitSuccessValue (2) avoidSuccessValue (2)
///   plusAvoidSuccessValue (2) criticalSuccessValue (2)
///   ASPD (2) plusASPD (2)
/// </code>
/// </summary>
public class ZC_STATUS : OutgoingPacket
{
    private const int SIZE = 44;

    public ushort StatusPoint { get; init; }
    public byte Str { get; init; }
    public byte StandardStr { get; init; }
    public byte Agi { get; init; }
    public byte StandardAgi { get; init; }
    public byte Vit { get; init; }
    public byte StandardVit { get; init; }
    public byte Int { get; init; }
    public byte StandardInt { get; init; }
    public byte Dex { get; init; }
    public byte StandardDex { get; init; }
    public byte Luk { get; init; }
    public byte StandardLuk { get; init; }
    public short AttPower { get; init; }
    public short RefiningPower { get; init; }
    public short MaxMattPower { get; init; }
    public short MinMattPower { get; init; }
    public short ItemDefPower { get; init; }
    public short PlusDefPower { get; init; }
    public short MdefPower { get; init; }
    public short PlusMdefPower { get; init; }
    public short Hit { get; init; }
    public short Flee { get; init; }
    public short Flee2 { get; init; }
    public short Crit { get; init; }
    public short Aspd { get; init; }
    public short PlusAspd { get; init; }

    public ZC_STATUS() : base(PacketHeader.ZC_STATUS, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(StatusPoint);
        writer.Write(Str); writer.Write(StandardStr);
        writer.Write(Agi); writer.Write(StandardAgi);
        writer.Write(Vit); writer.Write(StandardVit);
        writer.Write(Int); writer.Write(StandardInt);
        writer.Write(Dex); writer.Write(StandardDex);
        writer.Write(Luk); writer.Write(StandardLuk);
        writer.Write(AttPower); writer.Write(RefiningPower);
        writer.Write(MaxMattPower); writer.Write(MinMattPower);
        writer.Write(ItemDefPower); writer.Write(PlusDefPower);
        writer.Write(MdefPower); writer.Write(PlusMdefPower);
        writer.Write(Hit); writer.Write(Flee);
        writer.Write(Flee2); writer.Write(Crit);
        writer.Write(Aspd); writer.Write(PlusAspd);
    }
}
