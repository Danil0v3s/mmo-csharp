using System.Reflection;
using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Lookup table from <see cref="PacketHeader"/> to a registered
/// <see cref="IPacketDecoder"/>. Discovery is reflective: any non-abstract
/// class in this assembly that implements <see cref="IPacketDecoder"/>
/// and has a public parameterless constructor gets registered automatically.
/// </summary>
public sealed class DecoderRegistry
{
    private readonly Dictionary<PacketHeader, IPacketDecoder> _decoders = new();

    public DecoderRegistry()
    {
        var assembly = typeof(DecoderRegistry).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;
            if (!typeof(IPacketDecoder).IsAssignableFrom(type)) continue;
            if (type.GetConstructor(Type.EmptyTypes) == null) continue;

            var instance = (IPacketDecoder)Activator.CreateInstance(type)!;
            _decoders[instance.Header] = instance;
        }
    }

    public bool TryGet(PacketHeader header, out IPacketDecoder decoder)
        => _decoders.TryGetValue(header, out decoder!);

    public int Count => _decoders.Count;
    public IEnumerable<PacketHeader> RegisteredHeaders => _decoders.Keys;
}
