using System.Net;
using System.Net.Sockets;

namespace Core.Server.Monitoring;

public static class EndpointProbe
{
    public static async Task<bool> TryConnectAsync(
        uint ip,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (ip == 0 || port <= 0)
        {
            return false;
        }

        var address = ConvertUInt32ToIp(ip);
        using var client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await client.ConnectAsync(address, port, timeoutCts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string ConvertUInt32ToIp(uint ipAddress)
    {
        var bytes = BitConverter.GetBytes(ipAddress);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes).ToString();
    }

    public static string FormatEndpoint(uint ip, int port)
    {
        return $"{ConvertUInt32ToIp(ip)}:{port}";
    }
}
