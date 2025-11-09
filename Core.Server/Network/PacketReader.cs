using System.Text;

namespace Core.Server.Network;

public ref struct PacketReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    public PacketReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    public byte ReadByte()
    {
        if (_position + 1 > _data.Length)
            throw new InvalidOperationException("Not enough data to read byte");
        
        return _data[_position++];
    }

    public ushort ReadUInt16()
    {
        if (_position + 2 > _data.Length)
            throw new InvalidOperationException("Not enough data to read ushort");
        
        var value = BitConverter.ToUInt16(_data.Slice(_position, 2));
        _position += 2;
        return value;
    }

    public uint ReadUInt32()
    {
        if (_position + 4 > _data.Length)
            throw new InvalidOperationException("Not enough data to read uint");
        
        var value = BitConverter.ToUInt32(_data.Slice(_position, 4));
        _position += 4;
        return value;
    }

    public long ReadInt64()
    {
        if (_position + 8 > _data.Length)
            throw new InvalidOperationException("Not enough data to read long");
        
        var value = BitConverter.ToInt64(_data.Slice(_position, 8));
        _position += 8;
        return value;
    }

    public string ReadString()
    {
        var length = ReadUInt16();
        if (_position + length > _data.Length)
            throw new InvalidOperationException("Not enough data to read string");
        
        var value = Encoding.UTF8.GetString(_data.Slice(_position, length));
        _position += length;
        return value;
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (_position + count > _data.Length)
            throw new InvalidOperationException($"Not enough data to read {count} bytes");
        
        var slice = _data.Slice(_position, count);
        _position += count;
        return slice;
    }
}

