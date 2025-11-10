using System.Text;
using Microsoft.Extensions.Configuration;

namespace Core.Server.Packets;

/// <summary>
/// Central packet system that coordinates the factory and registry.
/// Provides a convenient initialization and access point for packet operations.
/// </summary>
public class PacketSystem
{
    public IPacketFactory Factory { get; }
    public IPacketSizeRegistry Registry { get; }
    public PacketConfiguration Configuration { get; private set; }
    
    public PacketSystem(IPacketFactory? factory = null, IPacketSizeRegistry? registry = null)
    {
        Factory = factory ?? new PacketFactory();
        Registry = registry ?? new PacketSizeRegistry();
        Configuration = new PacketConfiguration();
    }
    
    /// <summary>
    /// Initializes the packet system by scanning assemblies and loading configuration.
    /// </summary>
    public void Initialize(IConfiguration? configuration = null)
    {
        // Initialize factory and registry
        Factory.Initialize();
        Registry.Initialize();
        
        // Load and apply configuration if provided
        if (configuration != null)
        {
            Configuration = PacketConfiguration.Load(configuration);
            Configuration.ApplyToFactory(Factory);
        }
    }
    
    /// <summary>
    /// Reads a packet from a stream.
    /// Reads the header, determines if it's fixed or variable length, reads the size if needed,
    /// then creates the packet instance.
    /// </summary>
    public IncomingPacket? ReadPacket(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        return ReadPacket(reader);
    }
    
    /// <summary>
    /// Reads a packet from a BinaryReader.
    /// </summary>
    public IncomingPacket? ReadPacket(BinaryReader reader)
    {
        // Read packet header
        short headerValue = reader.ReadInt16();
        if (!Enum.IsDefined(typeof(PacketHeader), headerValue))
        {
            throw new InvalidDataException($"Unknown packet header: 0x{headerValue:X4}");
        }
        
        var header = (PacketHeader)headerValue;
        
        // Check if we can create this packet
        if (!Factory.CanCreatePacket(header))
        {
            throw new InvalidOperationException($"No factory registered for packet {header} (0x{headerValue:X4})");
        }
        
        // If variable length, read size field
        if (!Registry.IsFixedLength(header))
        {
            short size = reader.ReadInt16();
            PacketValidator.ValidateSize(size);
            // Size includes header and size field, so body size is size - 4
            // We don't need to do anything special here as the packet's Read method will read the body
        }
        
        // Create and read the packet
        return Factory.CreatePacket(header, reader);
    }
    
    /// <summary>
    /// Writes a packet to a stream.
    /// </summary>
    public void WritePacket(Stream stream, OutgoingPacket packet)
    {
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        WritePacket(writer, packet);
    }
    
    /// <summary>
    /// Writes a packet to a BinaryWriter.
    /// </summary>
    public void WritePacket(BinaryWriter writer, OutgoingPacket packet)
    {
        int size = packet.GetSize();
        PacketValidator.ValidateSize(size);
        packet.Write(writer);
    }
    
    /// <summary>
    /// Serializes a packet to a byte array.
    /// </summary>
    public byte[] SerializePacket(OutgoingPacket packet)
    {
        using var ms = new MemoryStream();
        WritePacket(ms, packet);
        return ms.ToArray();
    }
}

