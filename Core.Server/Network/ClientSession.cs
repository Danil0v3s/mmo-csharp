using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Core.Server.Network;

public class ClientSession : IDisposable
{
    private readonly Socket _socket;
    private readonly ILogger _logger;
    private readonly PacketBuffer _incomingBuffer;
    private readonly ConcurrentQueue<byte[]> _outgoingPackets;
    private readonly CancellationTokenSource _cts;
    private readonly Task _receiveTask;
    private readonly IPacketFactory _packetFactory;
    
    public Guid SessionId { get; }
    public long LastHeartbeatTime { get; private set; }
    public int HeartbeatTimeout { get; }
    public bool IsAlive { get; private set; }
    public DisconnectReason? DisconnectReason { get; private set; }
    
    public ConcurrentQueue<IncomingPacket> IncomingPackets { get; }

    public ClientSession(Socket socket, int heartbeatTimeout, IPacketFactory packetFactory, 
        IPacketSizeRegistry sizeRegistry, ILogger logger)
    {
        _socket = socket;
        _logger = logger;
        _packetFactory = packetFactory ?? throw new ArgumentNullException(nameof(packetFactory));
        HeartbeatTimeout = heartbeatTimeout;
        
        SessionId = Guid.NewGuid();
        LastHeartbeatTime = Stopwatch.GetTimestamp();
        IsAlive = true;
        
        _incomingBuffer = new PacketBuffer(sizeRegistry);
        _outgoingPackets = new ConcurrentQueue<byte[]>();
        IncomingPackets = new ConcurrentQueue<IncomingPacket>();
        
        _cts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    public void UpdateHeartbeat()
    {
        LastHeartbeatTime = Stopwatch.GetTimestamp();
    }

    public bool IsHeartbeatTimedOut()
    {
        if (!IsAlive)
            return true;

        var elapsed = (Stopwatch.GetTimestamp() - LastHeartbeatTime) * 1000.0 / Stopwatch.Frequency;
        return elapsed > HeartbeatTimeout;
    }

    public void EnqueuePacket(byte[] packet)
    {
        if (!IsAlive)
            return;

        _outgoingPackets.Enqueue(packet);
    }
    
    public void EnqueuePacket(OutgoingPacket packet)
    {
        if (!IsAlive)
            return;

        try
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            packet.Write(writer);
            _outgoingPackets.Enqueue(ms.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing packet {PacketType} for session {SessionId}", 
                packet.GetType().Name, SessionId);
        }
    }

    public async Task FlushPacketsAsync()
    {
        if (!IsAlive || _outgoingPackets.IsEmpty)
            return;

        try
        {
            while (_outgoingPackets.TryDequeue(out var packet))
            {
                await _socket.SendAsync(packet, SocketFlags.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing packets for session {SessionId}", SessionId);
            Disconnect(Network.DisconnectReason.SocketError);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsAlive)
            {
                var received = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
                
                if (received == 0)
                {
                    Disconnect(Network.DisconnectReason.ClientDisconnect);
                    break;
                }

                _incomingBuffer.Write(buffer.AsSpan(0, received));

                // Parse packets from buffer
                while (_incomingBuffer.TryReadPacket(out var header, out var packetData))
                {
                    try
                    {
                        // Handle heartbeat packets directly
                        if (header == PacketHeader.CZ_HEARTBEAT)
                        {
                            UpdateHeartbeat();
                            continue;
                        }

                        // Check if we can create this packet type
                        if (!_packetFactory.CanCreatePacket(header))
                        {
                            _logger.LogWarning("No handler for packet {Header} (0x{HeaderHex:X4}) from session {SessionId}", 
                                header, (short)header, SessionId);
                            continue;
                        }

                        // Deserialize packet
                        IncomingPacket packet;
                        using (var ms = new MemoryStream(packetData.ToArray()))
                        using (var reader = new BinaryReader(ms))
                        {
                            packet = _packetFactory.CreatePacket(header, reader);
                        }

                        // Enqueue packet for processing
                        IncomingPackets.Enqueue(packet);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing packet {Header} from session {SessionId}", 
                            header, SessionId);
                    }
                }

                _incomingBuffer.Compact();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in receive loop for session {SessionId}", SessionId);
            Disconnect(Network.DisconnectReason.SocketError);
        }
    }

    public void Disconnect(DisconnectReason reason)
    {
        if (!IsAlive)
            return;

        IsAlive = false;
        DisconnectReason = reason;
        
        _logger.LogInformation("Session {SessionId} disconnected: {Reason}", SessionId, reason);
        
        _cts.Cancel();
    }

    public void Dispose()
    {
        Disconnect(Network.DisconnectReason.ServerShutdown);
        
        try
        {
            _receiveTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch { }
        
        _socket.Close();
        _socket.Dispose();
        _incomingBuffer.Dispose();
        _cts.Dispose();
    }
}

public enum DisconnectReason
{
    ClientDisconnect,
    HeartbeatTimeout,
    SocketError,
    ServerShutdown,
    Kicked,
    UnhandledPacket,
    PacketHandlerError
}

