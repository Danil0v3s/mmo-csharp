using System.Collections.Concurrent;
using Core.Server.Network;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Core.Server.Tests.Mocks;

/// <summary>
/// Mock client session for testing that doesn't start a receive loop.
/// </summary>
public class MockClientSession
{
    public Guid SessionId { get; }
    public bool IsAlive { get; private set; }
    public DisconnectReason? DisconnectReason { get; private set; }
    public ConcurrentQueue<IncomingPacket> IncomingPackets { get; }

    private readonly ILogger _logger;

    public MockClientSession(ILogger logger)
    {
        SessionId = Guid.NewGuid();
        IsAlive = true;
        IncomingPackets = new ConcurrentQueue<IncomingPacket>();
        _logger = logger;
    }

    public void Disconnect(DisconnectReason reason)
    {
        if (!IsAlive)
            return;

        IsAlive = false;
        DisconnectReason = reason;
        _logger.LogInformation("Mock session {SessionId} disconnected: {Reason}", SessionId, reason);
    }

    public void EnqueuePacket(OutgoingPacket packet)
    {
        // Mock implementation - just log it
        _logger.LogDebug("Mock session {SessionId} would send packet {PacketType}", 
            SessionId, packet.GetType().Name);
    }
}

/// <summary>
/// Mock incoming packet for testing with any header
/// </summary>
public class MockIncomingPacket : IncomingPacket
{
    public MockIncomingPacket(PacketHeader header) : base(header, false)
    {
    }

    public override void Read(BinaryReader reader)
    {
        // Mock implementation - do nothing
    }

    public override int GetSize() => 4;
}

