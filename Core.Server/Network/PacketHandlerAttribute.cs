using Core.Server.Packets;

namespace Core.Server.Network;

/// <summary>
/// Marks a class as a packet handler and specifies which packet header it handles.
/// Used for automatic registration via reflection.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class PacketHandlerAttribute : Attribute
{
    /// <summary>
    /// The packet header this handler processes.
    /// </summary>
    public PacketHeader Header { get; }
    
    public PacketHandlerAttribute(PacketHeader header)
    {
        Header = header;
    }
}

