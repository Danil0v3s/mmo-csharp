using Core.Server.IPC;
using Grpc.Core;

namespace Char.Server.Services;

public class MapServerIpcService(
    IServerConnectionService connectionService,
    ILogger<MapServerIpcService> logger
) : IMapServerIpcService
{
    private IEnumerable<ServerSession> GetConnectedMaps()
        => connectionService.GetSessionsByType(ServerType.Map).Where(s => s.IsConnected);

    public async Task BroadcastAsync(MapBroadcastNotification notification, CancellationToken cancellationToken = default)
    {
        await FanOutAsync(notification, (client, n, ct) => client.ReceiveBroadcastAsync(n, cancellationToken: ct), cancellationToken);
    }

    public async Task BroadcastItemAsync(MapItemBroadcastNotification notification, CancellationToken cancellationToken = default)
    {
        await FanOutAsync(notification, (client, n, ct) => client.ReceiveItemBroadcastAsync(n, cancellationToken: ct), cancellationToken);
    }

    public async Task<bool> SendWhisperAsync(MapWhisperNotification notification, CancellationToken cancellationToken = default)
    {
        var delivered = false;
        foreach (var session in GetConnectedMaps())
        {
            try
            {
                var client = new MapService.MapServiceClient(session.Channel);
                var ack = await client.ReceiveWhisperAsync(notification, cancellationToken: cancellationToken);
                if (ack.Delivered)
                {
                    delivered = true;
                }
            }
            catch (RpcException ex)
            {
                logger.LogWarning(ex, "Failed to deliver whisper to map server {Name}", session.ServerName);
            }
        }
        return delivered;
    }

    public async Task SendWhisperToGmAsync(MapWhisperToGmNotification notification, CancellationToken cancellationToken = default)
    {
        await FanOutAsync(notification, (client, n, ct) => client.ReceiveWhisperToGmAsync(n, cancellationToken: ct), cancellationToken);
    }

    public async Task NotifyNameChangeAsync(MapNameChangeNotification notification, CancellationToken cancellationToken = default)
    {
        await FanOutAsync(notification, (client, n, ct) => client.NotifyNameChangeAsync(n, cancellationToken: ct), cancellationToken);
    }

    public async Task NotifyAddressSyncAsync(CancellationToken cancellationToken = default)
    {
        var notification = new MapAddressSyncNotification();
        await FanOutAsync(notification, (client, n, ct) => client.NotifyAddressSyncAsync(n, cancellationToken: ct), cancellationToken);
    }

    private async Task FanOutAsync<TNotification>(
        TNotification notification,
        Func<MapService.MapServiceClient, TNotification, CancellationToken, AsyncUnaryCall<MapBroadcastAck>> call,
        CancellationToken cancellationToken)
    {
        foreach (var session in GetConnectedMaps())
        {
            try
            {
                var client = new MapService.MapServiceClient(session.Channel);
                await call(client, notification, cancellationToken);
            }
            catch (RpcException ex)
            {
                logger.LogWarning(ex, "Failed to push {Notification} to map server {Name}", typeof(TNotification).Name, session.ServerName);
            }
        }
    }
}
