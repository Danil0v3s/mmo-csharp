namespace Core.Server;

public interface IServer
{
    public Task StartAsync();
    public Task StopAsync();
}