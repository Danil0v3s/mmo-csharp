using Core.Server.Packets;

namespace Map.Server.Tests.Packets;

public class PositionPackerTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(156, 191, 4)]  // prontera spawn, facing south
    [InlineData(1023, 1023, 7)] // max for 10-bit coords
    [InlineData(255, 255, 0)]
    public void WritePos_ReadPos_RoundTrips(short x, short y, byte dir)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        PositionPacker.WritePos(bw, x, y, dir);

        ms.Position = 0;
        using var br = new BinaryReader(ms);
        var (rx, ry, rdir) = PositionPacker.ReadPos(br);

        Assert.Equal(x, rx);
        Assert.Equal(y, ry);
        Assert.Equal(dir, rdir);
    }

    [Theory]
    [InlineData(0, 0, 10, 10, 0, 0)]
    [InlineData(156, 191, 200, 200, 0, 0)]
    [InlineData(1023, 1023, 0, 0, 8, 8)]
    public void WriteMove_ReadMove_RoundTrips(short x0, short y0, short x1, short y1, byte sx, byte sy)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        PositionPacker.WriteMove(bw, x0, y0, x1, y1, sx, sy);

        ms.Position = 0;
        using var br = new BinaryReader(ms);
        var (rx0, ry0, rx1, ry1, rsx, rsy) = PositionPacker.ReadMove(br);

        Assert.Equal(x0, rx0);
        Assert.Equal(y0, ry0);
        Assert.Equal(x1, rx1);
        Assert.Equal(y1, ry1);
        Assert.Equal(sx, rsx);
        Assert.Equal(sy, rsy);
    }

    [Fact]
    public void WritePos_Produces3Bytes()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        PositionPacker.WritePos(bw, 100, 100, 4);
        Assert.Equal(3, ms.Length);
    }

    [Fact]
    public void WriteMove_Produces6Bytes()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        PositionPacker.WriteMove(bw, 0, 0, 10, 10);
        Assert.Equal(6, ms.Length);
    }
}
