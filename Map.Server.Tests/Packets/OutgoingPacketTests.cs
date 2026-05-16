using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;

namespace Map.Server.Tests.Packets;

/// <summary>
/// Wire-shape and field-encoding tests for the MS1 outgoing (ZC_) packets.
/// Each test writes the packet to a buffer and inspects the bytes to confirm
/// the on-wire format matches what rAthena clients expect.
/// </summary>
public class OutgoingPacketTests
{
    [Fact]
    public void ZC_AID_Emits6Bytes_WithHeaderAndAccountId()
    {
        var packet = new ZC_AID { AccountId = 2000001 };
        var bytes = WritePacket(packet);

        Assert.Equal(6, bytes.Length);
        Assert.Equal(0x0283, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(2000001, BitConverter.ToInt32(bytes, 2));
    }

    [Fact]
    public void ZC_ACCEPT_ENTER_ZONE_Emits13Bytes_WithFontField()
    {
        var packet = new ZC_ACCEPT_ENTER_ZONE
        {
            StartTime = 1234567,
            X = 156,
            Y = 191,
            Dir = 4,
            Font = 0
        };
        var bytes = WritePacket(packet);

        Assert.Equal(13, bytes.Length);
        Assert.Equal(0x02eb, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(1234567u, BitConverter.ToUInt32(bytes, 2));
        // posDir at bytes 6..8; round-trip through PositionPacker.
        using var ms = new MemoryStream(bytes, 6, 3);
        var (x, y, dir) = PositionPacker.ReadPos(new BinaryReader(ms));
        Assert.Equal((short)156, x);
        Assert.Equal((short)191, y);
        Assert.Equal((byte)4, dir);
        Assert.Equal((byte)5, bytes[9]);   // xSize
        Assert.Equal((byte)5, bytes[10]);  // ySize
        Assert.Equal((short)0, BitConverter.ToInt16(bytes, 11)); // font
    }

    [Fact]
    public void ZC_REFUSE_ENTER_ZONE_Emits3Bytes_WithErrorCode()
    {
        var packet = new ZC_REFUSE_ENTER_ZONE { ErrorCode = 1 };
        var bytes = WritePacket(packet);

        Assert.Equal(3, bytes.Length);
        Assert.Equal(0x0074, BitConverter.ToInt16(bytes, 0));
        Assert.Equal((byte)1, bytes[2]);
    }

    [Fact]
    public void ZC_NOTIFY_TIME_Emits6Bytes_WithServerTick()
    {
        var packet = new ZC_NOTIFY_TIME { ServerTick = 8765432 };
        var bytes = WritePacket(packet);

        Assert.Equal(6, bytes.Length);
        Assert.Equal(0x007f, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(8765432u, BitConverter.ToUInt32(bytes, 2));
    }

    [Fact]
    public void ZC_NOTIFY_VANISH_Emits7Bytes_WithEntityAndReason()
    {
        var packet = new ZC_NOTIFY_VANISH
        {
            EntityId = 1234567,
            Reason = VanishReason.Logout
        };
        var bytes = WritePacket(packet);

        Assert.Equal(7, bytes.Length);
        Assert.Equal(0x0080, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(1234567, BitConverter.ToInt32(bytes, 2));
        Assert.Equal((byte)2, bytes[6]); // Logout = 2
    }

    [Fact]
    public void ZC_NPCACK_MAPMOVE_Emits22Bytes_WithMapNameAndCoords()
    {
        var packet = new ZC_NPCACK_MAPMOVE
        {
            MapName = "prontera",
            X = 156,
            Y = 191
        };
        var bytes = WritePacket(packet);

        Assert.Equal(22, bytes.Length);
        Assert.Equal(0x0091, BitConverter.ToInt16(bytes, 0));
        // Map name bytes 2..17, null-padded, ".gat" appended.
        var name = System.Text.Encoding.ASCII.GetString(bytes, 2, 16).TrimEnd('\0');
        Assert.Equal("prontera.gat", name);
        Assert.Equal((short)156, BitConverter.ToInt16(bytes, 18));
        Assert.Equal((short)191, BitConverter.ToInt16(bytes, 20));
    }

    [Fact]
    public void ZC_NOTIFY_PLAYERMOVE_Emits12Bytes()
    {
        var packet = new ZC_NOTIFY_PLAYERMOVE
        {
            StartTime = 1000,
            FromX = 10, FromY = 10, ToX = 15, ToY = 15
        };
        var bytes = WritePacket(packet);

        Assert.Equal(12, bytes.Length);
        Assert.Equal(0x0087, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(1000u, BitConverter.ToUInt32(bytes, 2));
    }

    [Fact]
    public void ZC_NOTIFY_MOVE_Emits16Bytes()
    {
        var packet = new ZC_NOTIFY_MOVE
        {
            EntityId = 999,
            FromX = 10, FromY = 10, ToX = 15, ToY = 15,
            StartTime = 5000,
        };
        var bytes = WritePacket(packet);

        Assert.Equal(16, bytes.Length);
        Assert.Equal(0x0086, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(999, BitConverter.ToInt32(bytes, 2));
        Assert.Equal(5000u, BitConverter.ToUInt32(bytes, 12));
    }

    [Fact]
    public void ZC_STOPMOVE_Emits10Bytes()
    {
        var packet = new ZC_STOPMOVE { EntityId = 7, X = 42, Y = 21 };
        var bytes = WritePacket(packet);

        Assert.Equal(10, bytes.Length);
        Assert.Equal(0x0088, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(7, BitConverter.ToInt32(bytes, 2));
        Assert.Equal((short)42, BitConverter.ToInt16(bytes, 6));
        Assert.Equal((short)21, BitConverter.ToInt16(bytes, 8));
    }

    [Fact]
    public void ZC_NOTIFY_STANDENTRY_Emits108Bytes_WithLengthPrefix()
    {
        var packet = new ZC_NOTIFY_STANDENTRY
        {
            ObjectType = 0,
            AccountId = 2000001,
            CharacterOrEntityId = 1500001,
            Job = 0,
            Sex = 1,
            X = 156,
            Y = 191,
            Dir = 4,
            Name = "TestPlayer"
        };
        var bytes = WritePacket(packet);

        Assert.Equal(108, bytes.Length);
        // [0..1] packet type
        Assert.Equal(unchecked((short)0x09ff), BitConverter.ToInt16(bytes, 0));
        // [2..3] declared packet length
        Assert.Equal((short)108, BitConverter.ToInt16(bytes, 2));
        // Account id is at offset 5 (after objecttype byte at 4).
        Assert.Equal(2000001, BitConverter.ToInt32(bytes, 5));
        Assert.Equal(1500001, BitConverter.ToInt32(bytes, 9));
        // Name occupies the last 24 bytes (NULL-padded).
        var nameStart = 108 - 24;
        var name = System.Text.Encoding.ASCII.GetString(bytes, nameStart, 24).TrimEnd('\0');
        Assert.Equal("TestPlayer", name);
    }

    [Fact]
    public void ZC_ITEM_ENTRY_Emits19Bytes()
    {
        var packet = new ZC_ITEM_ENTRY
        {
            EntityId = 2_000_000_001,
            ItemId = 501,
            Identified = 1,
            X = 156, Y = 191,
            Amount = 5,
            SubX = 2, SubY = 3,
        };
        var bytes = WritePacket(packet);

        Assert.Equal(19, bytes.Length);
        Assert.Equal(0x009d, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(2_000_000_001, BitConverter.ToInt32(bytes, 2));
        Assert.Equal(501, BitConverter.ToInt32(bytes, 6));
        Assert.Equal((byte)1, bytes[10]);
        Assert.Equal((short)156, BitConverter.ToInt16(bytes, 11));
        Assert.Equal((short)191, BitConverter.ToInt16(bytes, 13));
        Assert.Equal((short)5, BitConverter.ToInt16(bytes, 15));
        Assert.Equal((byte)2, bytes[17]);
        Assert.Equal((byte)3, bytes[18]);
    }

    [Fact]
    public void ZC_ITEM_FALL_ENTRY_Emits24Bytes()
    {
        var packet = new ZC_ITEM_FALL_ENTRY
        {
            EntityId = 2_000_000_001,
            ItemId = 501,
            ItemType = 0,
            Identified = 1,
            X = 156, Y = 191,
            SubX = 2, SubY = 3,
            Amount = 5,
            ShowDropEffect = 0,
            DropEffectMode = 0,
        };
        var bytes = WritePacket(packet);

        Assert.Equal(24, bytes.Length);
        Assert.Equal(unchecked((short)0x0add), BitConverter.ToInt16(bytes, 0));
        Assert.Equal(2_000_000_001, BitConverter.ToInt32(bytes, 2));
        Assert.Equal(501, BitConverter.ToInt32(bytes, 6));
    }

    [Fact]
    public void ZC_ITEM_DISAPPEAR_Emits6Bytes()
    {
        var packet = new ZC_ITEM_DISAPPEAR { EntityId = 2_000_000_001 };
        var bytes = WritePacket(packet);

        Assert.Equal(6, bytes.Length);
        Assert.Equal(0x00a1, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(2_000_000_001, BitConverter.ToInt32(bytes, 2));
    }

    private static byte[] WritePacket(OutgoingPacket packet)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        packet.WritePacket(bw);
        return ms.ToArray();
    }
}
