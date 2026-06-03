namespace Core.Server.Packets.Out.ZC;

/// <summary>Pet-data change type. rAthena <c>e_changestate_pet</c> (clif.hpp).</summary>
public enum PetDataType : byte
{
    Init = 0,
    Intimacy = 1,
    Hunger = 2,
    Accessory = 3,
    Performance = 4,
    HairStyle = 5,
    UpdateEgg = 6,
}

/// <summary>
/// Pet-data update. rAthena <c>clif_send_petdata</c> (clif.cpp, 0x01a4). Fixed 11 bytes:
/// <c>01a4 &lt;type&gt;.B &lt;GID&gt;.L &lt;data&gt;.L</c>. Carries one changed field (intimacy, hunger, accessory,
/// performance number, …) for the pet identified by <see cref="Gid"/>.
/// </summary>
public class ZC_CHANGESTATE_PET : OutgoingPacket
{
    private const int SIZE = 2 + 1 + 4 + 4; // 11

    public PetDataType Type { get; init; }
    public int Gid { get; init; }
    public int Data { get; init; }

    public ZC_CHANGESTATE_PET() : base(PacketHeader.ZC_CHANGESTATE_PET, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((byte)Type);
        writer.Write(Gid);
        writer.Write(Data);
    }
}
