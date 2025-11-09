using System.Buffers;
using System.Text;

namespace Core.Server.Network;

public class PacketWriter : IDisposable
{
    private byte[] _buffer;
    private int _position;

    public ReadOnlySpan<byte> WrittenData => _buffer.AsSpan(0, _position);
    public int Length => _position;

    public PacketWriter(int initialCapacity = 256)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _position = 0;
    }

    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value;
    }

    public void WriteUInt16(ushort value)
    {
        EnsureCapacity(2);
        BitConverter.TryWriteBytes(_buffer.AsSpan(_position), value);
        _position += 2;
    }

    public void WriteUInt32(uint value)
    {
        EnsureCapacity(4);
        BitConverter.TryWriteBytes(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    public void WriteInt64(long value)
    {
        EnsureCapacity(8);
        BitConverter.TryWriteBytes(_buffer.AsSpan(_position), value);
        _position += 8;
    }

    public void WriteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteUInt16((ushort)bytes.Length);
        WriteBytes(bytes);
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(_buffer.AsSpan(_position));
        _position += data.Length;
    }

    public byte[] ToArray()
    {
        return _buffer.AsSpan(0, _position).ToArray();
    }

    public void Reset()
    {
        _position = 0;
    }

    private void EnsureCapacity(int additionalBytes)
    {
        var requiredCapacity = _position + additionalBytes;
        if (requiredCapacity <= _buffer.Length)
            return;

        var newCapacity = Math.Max(_buffer.Length * 2, requiredCapacity);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        
        Array.Copy(_buffer, newBuffer, _position);
        ArrayPool<byte>.Shared.Return(_buffer);
        
        _buffer = newBuffer;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null!;
        }
    }

    public static byte[] CreateHeartbeatPacket()
    {
        var packet = new byte[2];
        BitConverter.TryWriteBytes(packet, PacketIds.Heartbeat);
        return packet;
    }

    public static byte[] CreatePacket(ushort packetId, Action<PacketWriter> writeBody)
    {
        using var writer = new PacketWriter();
        
        // Reserve space for header
        writer.WriteUInt16(packetId);
        writer.WriteUInt16(0); // Size placeholder
        
        var bodyStartPosition = writer.Length;
        
        // Write body
        writeBody(writer);
        
        var bodySize = writer.Length - bodyStartPosition;
        
        // Update size field
        var data = writer.ToArray();
        BitConverter.TryWriteBytes(data.AsSpan(2), (ushort)bodySize);
        
        return data;
    }
}

