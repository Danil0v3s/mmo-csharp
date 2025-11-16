using Core.Server;

namespace Map.Server;

public class MapServerConfiguration : ServerConfiguration
{
    public int MapLoadDistance { get; set; } = 2;
}