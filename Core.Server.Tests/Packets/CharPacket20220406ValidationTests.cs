using Core.Server.Packets;

namespace Core.Server.Tests.Packets;

public class CharPacket20220406ValidationTests
{
    [Fact]
    public void PacketHeaderValues_ShouldMatchRathena_20220406()
    {
        Assert.Equal(0x8fc, (short)PacketHeader.CH_REQ_CHANGE_CHARNAME);
        Assert.Equal(0x8fd, (short)PacketHeader.HC_ACK_CHANGE_CHARNAME);
        Assert.Equal(0x6f, (short)PacketHeader.HC_ACCEPT_DELETECHAR);
        Assert.Equal(0x70, (short)PacketHeader.HC_REFUSE_DELETECHAR);
        Assert.Equal(0xb6e, (short)PacketHeader.HC_REFUSE_MAKECHAR);
        Assert.Equal(0xb6f, (short)PacketHeader.HC_ACCEPT_MAKECHAR);
        Assert.Equal(0xb70, (short)PacketHeader.HC_ACK_CHANGE_CHARACTER_SLOT);
        Assert.Equal(0xb72, (short)PacketHeader.HC_ACK_CHARINFO_PER_PAGE);
    }

    [Fact]
    public void CharIncomingFixedPacketSizes_ShouldMatchRathena_20220406()
    {
        var registry = new PacketSizeRegistry();
        registry.Initialize();

        var expected = new Dictionary<PacketHeader, int>
        {
            [PacketHeader.CH_REQ_TO_CONNECT] = 17,
            [PacketHeader.CH_SELECT_CHAR] = 3,
            [PacketHeader.CH_MAKE_NEW_CHAR] = 36,
            [PacketHeader.CH_DELETE_CHAR] = 56,
            [PacketHeader.CH_REQ_CHAR_DELETE2_CANCEL] = 6,
            [PacketHeader.CH_REQ_CHAR_DELETE2_ACCEPT] = 12,
            [PacketHeader.CH_REQ_CHAR_DELETE2] = 6,
            [PacketHeader.CH_KEEP_ALIVE] = 6,
            [PacketHeader.CH_REQ_IS_VALID_CHARNAME] = 34,
            [PacketHeader.CH_REQ_CHANGE_CHARNAME] = 30,
            [PacketHeader.CH_MOVE_CHAR_SLOT] = 8,
            [PacketHeader.CH_REQ_CHARLIST] = 2,
            [PacketHeader.CH_SELECT_ACCESSIBLE_MAPNAME] = 4,
            [PacketHeader.CH_PINCODE_CHECK] = 10,
            [PacketHeader.CH_REQ_PINCODE_WINDOW] = 6,
            [PacketHeader.CH_PINCODE_CHANGE] = 14,
            [PacketHeader.CH_PINCODE_SETNEW] = 10
        };

        foreach (var (header, size) in expected)
        {
            Assert.True(registry.IsRegistered(header), $"Missing registration for {header}");
            Assert.True(registry.IsFixedLength(header), $"{header} should be fixed-length");
            Assert.Equal(size, registry.GetFixedSize(header));
        }
    }
}
