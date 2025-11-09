using System.Net.Sockets;

namespace Core.Server.Network;

/// <summary>
/// Example TCP client implementation for connecting to game servers.
/// This demonstrates the proper packet protocol usage.
/// </summary>
public class GameClient : IDisposable
{
    private readonly Socket _socket;
    private readonly PacketBuffer _buffer;
    private readonly CancellationTokenSource _cts;
    private Task? _receiveTask;

    public event Action<ushort, Memory<byte>>? PacketReceived;
    public event Action? Disconnected;

    public GameClient()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _buffer = new PacketBuffer();
        _cts = new CancellationTokenSource();
    }

    public async Task ConnectAsync(string host, int port)
    {
        await _socket.ConnectAsync(host, port);
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    public async Task SendPacketAsync(byte[] packet)
    {
        await _socket.SendAsync(packet, SocketFlags.None);
    }

    public async Task SendHeartbeatAsync()
    {
        var packet = PacketWriter.CreateHeartbeatPacket();
        await SendPacketAsync(packet);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var received = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
                
                if (received == 0)
                {
                    Disconnected?.Invoke();
                    break;
                }

                _buffer.Write(buffer.AsSpan(0, received));

                while (_buffer.TryReadPacket(out var packetId, out var packetData))
                {
                    if (packetId != PacketIds.Heartbeat)
                    {
                        var dataCopy = new byte[packetData.Length];
                        packetData.CopyTo(dataCopy);
                        PacketReceived?.Invoke(packetId, dataCopy);
                    }
                }

                _buffer.Compact();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch
        {
            Disconnected?.Invoke();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _receiveTask?.Wait(TimeSpan.FromSeconds(1));
        _socket.Close();
        _socket.Dispose();
        _buffer.Dispose();
        _cts.Dispose();
    }
}

/// <summary>
/// Example usage demonstrating login flow
/// </summary>
public static class GameClientExample
{
    public static async Task LoginExample()
    {
        using var client = new GameClient();
        
        // Connect to LoginServer
        await client.ConnectAsync("localhost", 5001);

        // Handle received packets
        client.PacketReceived += (packetId, data) =>
        {
            if (packetId == PacketIds.LoginResponse)
            {
                var reader = new PacketReader(data.Span);
                var success = reader.ReadByte() == 1;
                
                if (success)
                {
                    var accountId = reader.ReadInt64();
                    var sessionToken = reader.ReadString();
                    Console.WriteLine($"Login successful! Account ID: {accountId}, Token: {sessionToken}");
                }
                else
                {
                    var errorMessage = reader.ReadString();
                    Console.WriteLine($"Login failed: {errorMessage}");
                }
            }
        };

        // Send login request
        var loginPacket = PacketWriter.CreatePacket(PacketIds.LoginRequest, writer =>
        {
            writer.WriteString("testuser");
            writer.WriteString("password123");
        });
        
        await client.SendPacketAsync(loginPacket);

        // Start heartbeat task
        var heartbeatTask = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(15000);
                await client.SendHeartbeatAsync();
            }
        });

        // Keep connection alive
        await Task.Delay(60000);
    }
}

