using Core.Server;

namespace Char.Server;

/// <summary>
/// Read-only server state. Safe to inject into handlers without circular dependencies.
/// </summary>
public interface ICharServerState
{
    ServerState State { get; }
    int RegisteredServerId { get; }
    bool IsRegisteredToLoginServer { get; }
}

/// <summary>
/// Full runtime operations that require the server's session manager.
/// Use ICharServerState for state-only access in handlers.
/// </summary>
public interface ICharServerRuntime : ICharServerState
{
    Task<int> ForceDisconnectAccountAsync(int accountId);
    Task HandleAccountStatusBroadcastAsync(int accountId, bool isBan, uint value);
    Task HandleAccountSexBroadcastAsync(int accountId, uint sex);
    Task HandleVipDataPushAsync(
        int accountId,
        long vipTime,
        uint flags,
        uint groupId,
        int mapServerId,
        bool isVip,
        uint charSlots,
        uint charVip,
        uint oldGroup);
    void TriggerAddressSync();
}
