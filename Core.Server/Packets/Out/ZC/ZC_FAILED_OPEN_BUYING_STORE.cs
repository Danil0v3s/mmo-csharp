namespace Core.Server.Packets.Out.ZC;

/// <summary>Buying-store open-failure result. rAthena clif.cpp comment at 0x0812.</summary>
public enum BuyingStoreOpenResult : ushort
{
    Failed = 1,        // generic "Failed to open buying store."
    Overweight = 2,    // possessed items exceed the weight limit
    NoSalesInfo = 8,   // no purchase information available
}

/// <summary>
/// The requested buying store could not be created. rAthena <c>clif_buyingstore_open_failed</c>
/// (clif.cpp, 0x0812). Fixed 8 bytes: <c>0812 &lt;result&gt;.W &lt;total weight&gt;.L</c>.
/// </summary>
public class ZC_FAILED_OPEN_BUYING_STORE : OutgoingPacket
{
    private const int SIZE = 2 + 2 + 4; // 8

    public BuyingStoreOpenResult Result { get; init; }
    public uint Weight { get; init; }

    public ZC_FAILED_OPEN_BUYING_STORE() : base(PacketHeader.ZC_FAILED_OPEN_BUYING_STORE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((ushort)Result);
        writer.Write(Weight);
    }
}
