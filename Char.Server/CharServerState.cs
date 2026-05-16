using Core.Server;

namespace Char.Server;

/// <summary>
/// Holds the char server registration state.
/// Implements ICharServerState for safe injection into handlers.
/// </summary>
public class CharServerState : ICharServerState
{
    private volatile ServerState _state = ServerState.Stopped;
    private volatile bool _registeredToLoginServer;
    private volatile int _registeredServerId = -1;
    private uint _partyShareLevel = 10; // rAthena default in inter.cpp

    public ServerState State => _state;
    public bool IsRegisteredToLoginServer => _registeredToLoginServer;
    public int RegisteredServerId => _registeredServerId;
    public uint PartyShareLevel => Volatile.Read(ref _partyShareLevel);

    public void SetState(ServerState state) => _state = state;
    public void SetRegistered(bool registered, int serverId)
    {
        _registeredToLoginServer = registered;
        _registeredServerId = serverId;
    }
    public void SetPartyShareLevel(uint shareLevel) => Volatile.Write(ref _partyShareLevel, shareLevel);
}
