namespace Core.Server.IPC;

public class ServerEndpointConfiguration
{
    public ServerType ServerType { get; set; }
    public string Endpoint { get; set; } = string.Empty;
}

