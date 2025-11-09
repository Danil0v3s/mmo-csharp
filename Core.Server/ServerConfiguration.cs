namespace Core.Server;

public class ServerConfiguration
{
    public int Port { get; set; }
    public int GrpcPort { get; set; }
    public int TargetFPS { get; set; } = 20;
    public int HeartbeatTimeout { get; set; } = 30000; // milliseconds
    public int MaxConnections { get; set; } = 1000;
    public Dictionary<string, string> OtherServerEndpoints { get; set; } = new();
}

