using System.Collections.Concurrent;
using System.Reflection;

namespace Core.Server.Packets;

/// <summary>
/// Implementation of the packet size registry.
/// Automatically discovers packet types through reflection at startup.
/// </summary>
public class PacketSizeRegistry : IPacketSizeRegistry
{
    private readonly ConcurrentDictionary<PacketHeader, PacketSizeInfo> _registry = new();
    
    private record PacketSizeInfo(bool IsFixedLength, int? FixedSize);
    
    public void RegisterFixedSize(PacketHeader header, int size)
    {
        PacketValidator.ValidateSize(size);
        _registry[header] = new PacketSizeInfo(true, size);
    }
    
    public void RegisterVariableSize(PacketHeader header)
    {
        _registry[header] = new PacketSizeInfo(false, null);
    }
    
    public bool IsFixedLength(PacketHeader header)
    {
        if (_registry.TryGetValue(header, out var info))
        {
            return info.IsFixedLength;
        }
        
        throw new InvalidOperationException($"Packet header {header} (0x{(short)header:X4}) is not registered");
    }
    
    public int? GetFixedSize(PacketHeader header)
    {
        if (_registry.TryGetValue(header, out var info))
        {
            return info.FixedSize;
        }
        
        return null;
    }
    
    public bool IsRegistered(PacketHeader header)
    {
        return _registry.ContainsKey(header);
    }
    
    public void Initialize()
    {
        // Get all assemblies that might contain packet types
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && (a.FullName?.Contains("Core.Server") == true || 
                                         a.FullName?.Contains("Char.Server") == true ||
                                         a.FullName?.Contains("Login.Server") == true ||
                                         a.FullName?.Contains("Map.Server") == true));
        
        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly);
        }
    }
    
    /// <summary>
    /// Scans an assembly for packet types and registers them.
    /// </summary>
    private void ScanAssembly(Assembly assembly)
    {
        try
        {
            var packetTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && 
                           typeof(Packet).IsAssignableFrom(t) &&
                           t.GetCustomAttribute<PacketVersionAttribute>() != null);
            
            foreach (var type in packetTypes)
            {
                RegisterPacketType(type);
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some types couldn't be loaded, but we can still process the ones that could
            foreach (var type in ex.Types.Where(t => t != null))
            {
                if (!type!.IsAbstract && 
                    typeof(Packet).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<PacketVersionAttribute>() != null)
                {
                    RegisterPacketType(type);
                }
            }
        }
    }
    
    /// <summary>
    /// Registers a single packet type by creating a temporary instance to read its metadata.
    /// </summary>
    private void RegisterPacketType(Type type)
    {
        try
        {
            // Create instance using default constructor
            var packet = Activator.CreateInstance(type) as Packet;
            if (packet == null)
                return;
            
            var header = packet.Header;
            bool isFixedLength = packet.Size != -1;
            
            // If already registered with the same settings, skip
            if (_registry.TryGetValue(header, out var existing))
            {
                if (existing.IsFixedLength == isFixedLength)
                {
                    if (!isFixedLength || existing.FixedSize == packet.Size)
                    {
                        return; // Already registered correctly
                    }
                }
                
                // Conflicting registration
                throw new InvalidOperationException(
                    $"Packet header {header} (0x{(short)header:X4}) is registered with conflicting size information. " +
                    $"Type: {type.FullName}");
            }
            
            if (isFixedLength)
            {
                RegisterFixedSize(header, packet.Size);
            }
            else
            {
                RegisterVariableSize(header);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to register packet type {type.FullName}: {ex.Message}", ex);
        }
    }
}

