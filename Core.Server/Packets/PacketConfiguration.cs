using Microsoft.Extensions.Configuration;

namespace Core.Server.Packets;

/// <summary>
/// Configuration for packet versioning.
/// Loads active packet versions from application configuration.
/// </summary>
public class PacketConfiguration
{
    /// <summary>
    /// Dictionary mapping packet header names to their active version numbers.
    /// </summary>
    public Dictionary<string, int> PacketVersions { get; set; } = new();
    
    /// <summary>
    /// Loads packet configuration from IConfiguration.
    /// </summary>
    public static PacketConfiguration Load(IConfiguration configuration)
    {
        var config = new PacketConfiguration();
        var section = configuration.GetSection("PacketVersions");
        
        if (section.Exists())
        {
            foreach (var child in section.GetChildren())
            {
                if (int.TryParse(child.Value, out var version))
                {
                    config.PacketVersions[child.Key] = version;
                }
            }
        }
        
        return config;
    }
    
    /// <summary>
    /// Applies the configured packet versions to the packet factory.
    /// </summary>
    public void ApplyToFactory(IPacketFactory factory)
    {
        foreach (var (headerName, version) in PacketVersions)
        {
            if (Enum.TryParse<PacketHeader>(headerName, out var header))
            {
                try
                {
                    factory.SetActiveVersion(header, version);
                }
                catch (InvalidOperationException)
                {
                    // Log warning but continue - packet might not be registered yet
                    Console.WriteLine($"Warning: Could not set version {version} for packet {headerName} - packet may not be registered");
                }
            }
            else
            {
                Console.WriteLine($"Warning: Unknown packet header name in configuration: {headerName}");
            }
        }
    }
    
    /// <summary>
    /// Gets the configured version for a packet header.
    /// </summary>
    public int? GetVersion(PacketHeader header)
    {
        var headerName = header.ToString();
        if (PacketVersions.TryGetValue(headerName, out var version))
        {
            return version;
        }
        return null;
    }
}

