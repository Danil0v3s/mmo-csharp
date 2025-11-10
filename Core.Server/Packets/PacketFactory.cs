using System.Collections.Concurrent;
using System.Reflection;

namespace Core.Server.Packets;

/// <summary>
/// Default implementation of the packet factory.
/// Automatically discovers and registers packet types with version support.
/// </summary>
public class PacketFactory : IPacketFactory
{
    private readonly ConcurrentDictionary<PacketHeader, Dictionary<int, Func<IncomingPacket>>> _packetFactories = new();
    private readonly ConcurrentDictionary<PacketHeader, int> _activeVersions = new();
    
    public void RegisterPacket<T>(PacketHeader header, int version) where T : IncomingPacket, new()
    {
        var factories = _packetFactories.GetOrAdd(header, _ => new Dictionary<int, Func<IncomingPacket>>());
        
        lock (factories)
        {
            if (factories.ContainsKey(version))
            {
                throw new InvalidOperationException(
                    $"Packet {header} version {version} is already registered");
            }
            
            factories[version] = () => new T();
        }
    }
    
    public void SetActiveVersion(PacketHeader header, int version)
    {
        if (!_packetFactories.TryGetValue(header, out var factories) || !factories.ContainsKey(version))
        {
            throw new InvalidOperationException(
                $"Cannot set active version: Packet {header} version {version} is not registered");
        }
        
        _activeVersions[header] = version;
    }
    
    public IncomingPacket CreatePacket(PacketHeader header, BinaryReader reader)
    {
        if (!_packetFactories.TryGetValue(header, out var factories))
        {
            throw new InvalidOperationException(
                $"No packet factory registered for header {header} (0x{(short)header:X4})");
        }
        
        // Get active version or use the latest version
        int version;
        if (!_activeVersions.TryGetValue(header, out version))
        {
            // Use highest version available
            lock (factories)
            {
                version = factories.Keys.Max();
            }
        }
        
        Func<IncomingPacket> factory;
        lock (factories)
        {
            if (!factories.TryGetValue(version, out factory!))
            {
                throw new InvalidOperationException(
                    $"Packet {header} version {version} is not registered");
            }
        }
        
        var packet = factory();
        packet.Read(reader);
        return packet;
    }
    
    public bool CanCreatePacket(PacketHeader header)
    {
        return _packetFactories.ContainsKey(header);
    }
    
    public int? GetActiveVersion(PacketHeader header)
    {
        if (_activeVersions.TryGetValue(header, out var version))
        {
            return version;
        }
        
        // Return highest version if no active version is set
        if (_packetFactories.TryGetValue(header, out var factories))
        {
            lock (factories)
            {
                if (factories.Count > 0)
                {
                    return factories.Keys.Max();
                }
            }
        }
        
        return null;
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
    /// Scans an assembly for incoming packet types and registers them.
    /// </summary>
    private void ScanAssembly(Assembly assembly)
    {
        try
        {
            var packetTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && 
                           typeof(IncomingPacket).IsAssignableFrom(t) &&
                           t.GetCustomAttribute<PacketVersionAttribute>() != null &&
                           t.GetConstructor(Type.EmptyTypes) != null); // Must have parameterless constructor
            
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
                    typeof(IncomingPacket).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<PacketVersionAttribute>() != null &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                {
                    RegisterPacketType(type);
                }
            }
        }
    }
    
    /// <summary>
    /// Registers a single packet type.
    /// </summary>
    private void RegisterPacketType(Type type)
    {
        try
        {
            var versionAttr = type.GetCustomAttribute<PacketVersionAttribute>();
            if (versionAttr == null)
                return;
            
            // Create a temporary instance to get the header
            var packet = Activator.CreateInstance(type) as IncomingPacket;
            if (packet == null)
                return;
            
            var header = packet.Header;
            var version = versionAttr.Version;
            
            // Register using reflection to call the generic method
            var method = typeof(PacketFactory).GetMethod(nameof(RegisterPacket))!;
            var genericMethod = method.MakeGenericMethod(type);
            
            try
            {
                genericMethod.Invoke(this, new object[] { header, version });
            }
            catch (TargetInvocationException ex)
            {
                // If already registered, that's okay (might be re-scanning)
                if (ex.InnerException is InvalidOperationException)
                {
                    return;
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to register packet type {type.FullName}: {ex.Message}", ex);
        }
    }
}

