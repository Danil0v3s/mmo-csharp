using System.Buffers;
using Core.Server.Packets;

namespace Core.Server.Network;

public class PacketBuffer
{
    private byte[] _buffer;
    private int _writePosition;
    private int _readPosition;
    private readonly IPacketSizeRegistry _sizeRegistry;

    public int Available => _writePosition - _readPosition;
    public int Capacity => _buffer.Length;

    public PacketBuffer(IPacketSizeRegistry sizeRegistry, int initialCapacity = 8192)
    {
        _sizeRegistry = sizeRegistry ?? throw new ArgumentNullException(nameof(sizeRegistry));
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

    public bool TryReadPacket(out PacketHeader header, out Memory<byte> packetData)
    {
        header = default;
        packetData = Memory<byte>.Empty;

        // Need at least 2 bytes for packet header
        if (Available < 2)
            return false;

        // Read packet header
        short headerValue = BitConverter.ToInt16(_buffer, _readPosition);
        
        // Validate header
        if (!Enum.IsDefined(typeof(PacketHeader), headerValue))
        {
            // Invalid packet header - skip this byte and try again
            _readPosition++;
            return false;
        }
        
        header = (PacketHeader)headerValue;
        
        // Check if header is registered
        if (!_sizeRegistry.IsRegistered(header))
        {
            // Unknown packet - skip header and continue
            _readPosition += 2;
            return false;
        }

        // Check if it's a fixed-length packet
        if (_sizeRegistry.IsFixedLength(header))
        {
            int? fixedSize = _sizeRegistry.GetFixedSize(header);
            if (fixedSize.HasValue)
            {
                // Check if we have the complete fixed-length packet
                if (Available < fixedSize.Value)
                    return false;
                
                // Extract packet data (everything after header)
                int bodySize = fixedSize.Value - 2; // Subtract header size
                if (bodySize > 0)
                {
                    packetData = new Memory<byte>(_buffer, _readPosition + 2, bodySize);
                }
                _readPosition += fixedSize.Value;
                return true;
            }
        }

        // Variable-length packet: need 4 bytes total for header + size field
        if (Available < 4)
            return false;

        // Read packet size (total size including header and size field)
        short packetSize = BitConverter.ToInt16(_buffer, _readPosition + 2);

        // Validate packet size (note: MaxPacketSize is 32KB but we compare as int since size could be malformed)
        if (packetSize < 4 || (int)packetSize > PacketValidator.MaxPacketSize)
        {
            // Invalid size - skip this packet
            _readPosition += 4;
            return false;
        }

        // Check if we have the full packet
        if (Available < packetSize)
            return false;

        // Extract packet data (excluding header and size field)
        int dataSize = packetSize - 4; // Subtract header (2) and size field (2)
        if (dataSize > 0)
        {
            packetData = new Memory<byte>(_buffer, _readPosition + 4, dataSize);
        }
        _readPosition += packetSize;

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

