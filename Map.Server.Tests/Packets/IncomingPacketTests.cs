using Core.Server.Packets.In.CZ;

namespace Map.Server.Tests.Packets;

/// <summary>
/// Round-trip and wire-format tests for the MS1 incoming (CZ_) packets.
/// Each test constructs the on-wire byte sequence for the body, feeds it
/// to <c>Read</c>, and asserts every field deserialized correctly.
/// </summary>
public class IncomingPacketTests
{
    [Fact]
    public void CZ_WANT_TO_CONNECTION_ParsesAllFields()
    {
        // Body = account_id (4) + char_id (4) + login_id1 (4) + client_tick (4) + sex (1) = 17 bytes.
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(2000001);   // account_id
        bw.Write(1500001);   // char_id
        bw.Write(987654321); // login_id1
        bw.Write((uint)1234); // client_tick
        bw.Write((byte)1);   // sex
        ms.Position = 0;

        var packet = CZ_WANT_TO_CONNECTION.Create(new BinaryReader(ms));

        Assert.Equal(2000001, packet.AccountId);
        Assert.Equal(1500001, packet.CharacterId);
        Assert.Equal(987654321, packet.LoginId1);
        Assert.Equal((uint)1234, packet.ClientTick);
        Assert.Equal((byte)1, packet.Sex);
    }

    [Fact]
    public void CZ_NOTIFY_ACTORINIT_HasNoBody()
    {
        using var ms = new MemoryStream();
        ms.Position = 0;
        var packet = CZ_NOTIFY_ACTORINIT.Create(new BinaryReader(ms));
        Assert.NotNull(packet);
    }

    [Theory]
    [InlineData(156, 191, 4)]
    [InlineData(0, 0, 0)]
    [InlineData(1023, 1023, 7)]
    public void CZ_REQUEST_MOVE_ParsesPackedPosition(short x, short y, byte dir)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        Core.Server.Packets.PositionPacker.WritePos(bw, x, y, dir);
        ms.Position = 0;

        var packet = CZ_REQUEST_MOVE.Create(new BinaryReader(ms));

        Assert.Equal(x, packet.TargetX);
        Assert.Equal(y, packet.TargetY);
        Assert.Equal(dir, packet.Dir);
    }

    [Fact]
    public void CZ_REQUEST_TIME_ReadsClientTick()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((uint)9876543);
        ms.Position = 0;

        var packet = CZ_REQUEST_TIME.Create(new BinaryReader(ms));
        Assert.Equal((uint)9876543, packet.ClientTick);
    }

    [Fact]
    public void CZ_REQ_QUIT_ReadsAndDiscards()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((short)0); // reserved
        ms.Position = 0;

        var packet = CZ_REQ_QUIT.Create(new BinaryReader(ms));
        Assert.NotNull(packet);
    }
}
