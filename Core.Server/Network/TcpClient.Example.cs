using System.Net.Sockets;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;

namespace Core.Server.Network;

/// <summary>
/// Example TCP client implementation for connecting to game servers.
/// This demonstrates the proper packet protocol usage with the new packet system.
/// </summary>
public class GameClient : IDisposable
{
    private readonly Socket _socket;
    private readonly PacketBuffer _buffer;
    private readonly PacketSystem _packetSystem;
    private readonly CancellationTokenSource _cts;
    private Task? _receiveTask;

    public event Action<IncomingPacket>? PacketReceived;
    public event Action? Disconnected;

    public GameClient()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        // Initialize packet system for client-side deserialization
        _packetSystem = new PacketSystem();
        _packetSystem.Initialize();
        
        _buffer = new PacketBuffer(_packetSystem.Registry);
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

    public async Task SendPacketAsync(OutgoingPacket packet)
    {
        var data = _packetSystem.SerializePacket(packet);
        await _socket.SendAsync(data, SocketFlags.None);
    }

    public async Task SendHeartbeatAsync()
    {
        // Heartbeat is a fixed-length packet with just the header
        var buffer = new byte[2];
        BitConverter.TryWriteBytes(buffer, (short)PacketHeader.CZ_HEARTBEAT);
        await _socket.SendAsync(buffer, SocketFlags.None);
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

                while (_buffer.TryReadPacket(out var header, out var packetData))
                {
                    // Skip heartbeat responses (if server sends them)
                    if (header == PacketHeader.CZ_HEARTBEAT)
                        continue;

                    try
                    {
                        // Deserialize packet
                        using var ms = new MemoryStream(packetData.ToArray());
                        using var reader = new BinaryReader(ms);
                        var packet = _packetSystem.Factory.CreatePacket(header, reader);
                        
                        PacketReceived?.Invoke(packet);
                    }
                    catch (Exception)
                    {
                        // Invalid packet, continue
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
/// Example usage demonstrating login flow with new packet system
/// </summary>
public static class GameClientExample
{
    public static async Task LoginExample()
    {
        using var client = new GameClient();
        
        // Connect to LoginServer
        await client.ConnectAsync("localhost", 5001);

        // Handle received packets using pattern matching
        client.PacketReceived += packet =>
        {
            // Note: AC_ACCEPT_LOGIN is an OutgoingPacket (server-side), so client receives it as raw packet
            // In a real implementation, you would create client-side packet classes that inherit from IncomingPacket
            // or handle server packets differently
            Console.WriteLine($"Received packet: {packet.Header} ({packet.GetType().Name})");
            
            // Example of handling specific packets
            if (packet.Header == PacketHeader.AC_ACCEPT_LOGIN)
            {
                Console.WriteLine("Login accepted by server");
            }
        };

        // Send login request using strongly-typed packet
        // Note: CA_LOGIN can be constructed but setting properties requires internal visibility
        // In practice, client would create the packet and manually serialize it
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        // Write CA_LOGIN manually
        writer.Write((short)PacketHeader.CA_LOGIN); // Header
        writer.Write((short)53); // Size
        writer.WriteFixedString("testuser", 24); // Username
        writer.WriteFixedString("password123", 24); // Password
        writer.Write((byte)1); // ClientType
        
        var loginPacketData = ms.ToArray();
        
        await client.SendPacketAsync(loginPacketData);

        // Start heartbeat task
        using var heartbeatCts = new CancellationTokenSource();
        var heartbeatTask = Task.Run(async () =>
        {
            while (!heartbeatCts.Token.IsCancellationRequested)
            {
                await Task.Delay(15000, heartbeatCts.Token);
                await client.SendHeartbeatAsync();
            }
        }, heartbeatCts.Token);

        // Keep connection alive
        await Task.Delay(60000);
        heartbeatCts.Cancel();
    }
}

