using System.Buffers;

namespace Core.Server.Network;

public class PacketBuffer
{
    private byte[] _buffer;
    private int _writePosition;
    private int _readPosition;

    public int Available => _writePosition - _readPosition;
    public int Capacity => _buffer.Length;

    public PacketBuffer(int initialCapacity = 8192)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _writePosition = 0;
        _readPosition = 0;
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(_writePosition + data.Length);
        data.CopyTo(_buffer.AsSpan(_writePosition));
        _writePosition += data.Length;
    }

    public bool TryReadPacket(out ushort packetId, out Memory<byte> packetData)
    {
        packetId = 0;
        packetData = Memory<byte>.Empty;

        // Need at least 2 bytes for packet ID
        if (Available < 2)
            return false;

        // Read packet ID
        packetId = BitConverter.ToUInt16(_buffer, _readPosition);

        // Check if it's a fixed-length packet (heartbeat)
        if (packetId == PacketIds.Heartbeat)
        {
            _readPosition += 2;
            return true;
        }

        // Need 4 bytes total for variable-length packet header (ID + size)
        if (Available < 4)
            return false;

        // Read packet size
        var packetSize = BitConverter.ToUInt16(_buffer, _readPosition + 2);

        // Check if we have the full packet
        if (Available < 4 + packetSize)
            return false;

        // Extract packet data (excluding header)
        packetData = new Memory<byte>(_buffer, _readPosition + 4, packetSize);
        _readPosition += 4 + packetSize;

        return true;
    }

    public void Compact()
    {
        if (_readPosition == 0)
            return;

        if (_readPosition == _writePosition)
        {
            _readPosition = 0;
            _writePosition = 0;
            return;
        }

        var remaining = Available;
        Array.Copy(_buffer, _readPosition, _buffer, 0, remaining);
        _readPosition = 0;
        _writePosition = remaining;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= Capacity)
            return;

        var newCapacity = Math.Max(Capacity * 2, requiredCapacity);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        
        Array.Copy(_buffer, newBuffer, _writePosition);
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
}

