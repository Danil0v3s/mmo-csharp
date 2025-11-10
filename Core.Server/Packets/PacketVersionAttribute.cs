namespace Core.Server.Packets;

/// <summary>
/// Specifies the version number for a packet class.
/// Multiple versions of the same packet type can coexist with different version numbers.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PacketVersionAttribute : Attribute
{
    public int Version { get; }
    
    public PacketVersionAttribute(int version)
    {
        if (version < 1)
            throw new ArgumentException("Packet version must be 1 or greater", nameof(version));
            
        Version = version;
    }
}

