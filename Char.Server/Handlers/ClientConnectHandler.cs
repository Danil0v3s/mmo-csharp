using Char.Server.Services;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;
using System.Buffers.Binary;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_TO_CONNECT)]
public class ClientConnectHandler(
    ILogger<ClientConnectHandler> logger,
    ICharServerState charServerState,
    ILoginServerIpcService loginServerIpc,
    CharServerConfiguration configuration,
    SessionManager sessionManager,
    ICharacterListFlowService characterListFlowService
) : IPacketHandler<CharSessionData, CH_REQ_TO_CONNECT>
{
    private const int DefaultCharSlots = 3;
    private const int ConnectRpcTimeoutMs = 3000;

    public async Task HandleAsync(CharSessionData session, CH_REQ_TO_CONNECT packet)
    {
        logger.LogInformation(
            "ConnectFlow: received CH_REQ_TO_CONNECT for account {AccountId} (session {SessionId})",
            packet.AccountId,
            session.SessionId);

        // rAthena echoes account id immediately after receiving 0x65.
        var accountAck = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(accountAck, packet.AccountId);
        session.EnqueuePacket(accountAck);

        var currentBoundAccountId = session.AccountId;
        if (IsRepeatedConnectPacket(currentBoundAccountId))
        {
            logger.LogInformation(
                "Ignoring duplicate CH_REQ_TO_CONNECT for account {AccountId} on session {SessionId}",
                currentBoundAccountId ?? 0,
                session.SessionId);
            return;
        }

        session.AccountId = (int)packet.AccountId;
        session.LoginId1 = (int)packet.LoginId1;
        session.LoginId2 = (int)packet.LoginId2;
        session.Sex = packet.Sex;
        session.IsAuthenticated = false;

        if (charServerState.State != ServerState.Running || !charServerState.IsRegisteredToLoginServer)
        {
            logger.LogInformation(
                "Rejecting CH_REQ_TO_CONNECT for account {AccountId}: char server not ready (state={State}, registered={Registered})",
                packet.AccountId,
                charServerState.State,
                charServerState.IsRegisteredToLoginServer);

            // rAthena equivalent: auth-result "server closed".
            CharRejectFlow.RejectAuthResult(session, resultCode: 1);
            return;
        }

        logger.LogInformation(
            "ConnectFlow: requesting login auth for account {AccountId} (session {SessionId})",
            packet.AccountId,
            session.SessionId);

        CharacterServerAuthResponse? authResponse;
        using (var authCts = new CancellationTokenSource(ConnectRpcTimeoutMs))
        {
            try
            {
                authResponse = await loginServerIpc.AuthenticateAccountAsync(
                    (int)packet.AccountId,
                    (int)packet.LoginId1,
                    (int)packet.LoginId2,
                    packet.Sex,
                    requestId: (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % int.MaxValue),
                    charServerState.RegisteredServerId,
                    authCts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning(
                    "ConnectFlow: login auth request timed out for account {AccountId} (session {SessionId})",
                    packet.AccountId,
                    session.SessionId);
                CharRejectFlow.RejectEnter(session, errorCode: 0);
                return;
            }
        }

        if (authResponse?.Success != true)
        {
            logger.LogInformation(
                "Char auth rejected for account {AccountId} (session {SessionId})",
                packet.AccountId,
                session.SessionId);

            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        session.AccountId = authResponse.AccountId;
        session.LoginId1 = authResponse.LoginId1;
        session.LoginId2 = authResponse.LoginId2;
        session.Sex = (byte)authResponse.Sex;
        session.ClientType = authResponse.ClientType;

        // rAthena char_auth_ok parity: reject new connect when another live char session
        // for this account already exists on this char-server.
        var duplicateSessionExists = HasDuplicateLiveAccountSession(
            sessionManager.GetAllSessions()
                .OfType<CharSessionData>()
                .Select(s => new SessionSnapshot(
                    s.SessionId,
                    s.IsAlive,
                    s.AccountId,
                    s.IsAuthenticated,
                    s.AccountDataLoaded)),
            session.SessionId,
            session.AccountId.Value);

        if (duplicateSessionExists)
        {
            logger.LogInformation(
                "Rejecting duplicate char connect for account {AccountId} (session {SessionId})",
                session.AccountId.Value,
                session.SessionId);

            // rAthena equivalent: auth-result "already online".
            CharRejectFlow.RejectAuthResult(session, resultCode: 8);
            return;
        }

        session.IsAuthenticated = true;

        logger.LogInformation(
            "ConnectFlow: requesting full account data for account {AccountId} (session {SessionId})",
            session.AccountId.Value,
            session.SessionId);

        AccountDataResponse? accountData;
        using (var dataCts = new CancellationTokenSource(ConnectRpcTimeoutMs))
        {
            try
            {
                accountData = await loginServerIpc.RequestFullAccountDataAsync(session.AccountId.Value, dataCts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning(
                    "ConnectFlow: account-data request timed out for account {AccountId} (session {SessionId})",
                    session.AccountId.Value,
                    session.SessionId);
                CharRejectFlow.RejectEnter(session, errorCode: 0);
                return;
            }
        }

        if (accountData?.Success != true)
        {
            logger.LogWarning(
                "Char auth accepted but account-data fetch failed for account {AccountId} (session {SessionId})",
                session.AccountId.Value,
                session.SessionId);

            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        session.Email = accountData.Email ?? string.Empty;
        session.GroupId = Math.Max(accountData.GroupId, 0);
        session.CharacterSlots = (int)Math.Max(accountData.CharSlots, DefaultCharSlots);
        session.Birthdate = accountData.Birthdate ?? string.Empty;
        session.Pincode = accountData.Pincode ?? string.Empty;
        session.PincodeChangeUnixTime = accountData.PincodeChange;
        session.IsVip = accountData.IsVip;
        session.VipCharacterSlots = (int)Math.Max(accountData.CharVip, 0);
        session.BillingCharacterSlots = (int)Math.Max(accountData.MaxBilling, 0);
        session.AccountDataLoaded = true;
        session.PincodeVerified = !configuration.Pincode.Enabled;
        session.PincodeAttempts = 0;

        logger.LogInformation(
            "Char auth accepted for account {AccountId} (session {SessionId}); account data loaded (slots={Slots}, group={GroupId})",
            session.AccountId.Value,
            session.SessionId,
            session.CharacterSlots,
            session.GroupId);

        logger.LogInformation(
            "ConnectFlow: sending initial character window/list for account {AccountId} (session {SessionId})",
            session.AccountId.Value,
            session.SessionId);

        var initialSent = await characterListFlowService.TrySendInitialCharacterWindowAsync(session);
        logger.LogInformation(
            "ConnectFlow: post-auth initial char window flow result for account {AccountId} (session {SessionId}) = {Result}",
            session.AccountId.Value,
            session.SessionId,
            initialSent ? "sent" : "blocked");
    }

    internal static bool IsRepeatedConnectPacket(int? boundAccountId)
    {
        return boundAccountId.HasValue;
    }

    internal static bool HasDuplicateLiveAccountSession(
        IEnumerable<SessionSnapshot> sessions,
        Guid currentSessionId,
        int accountId)
    {
        return sessions.Any(s =>
            s.SessionId != currentSessionId &&
            s.IsAlive &&
            s.AccountId.HasValue &&
            s.AccountId.Value == accountId &&
            (s.IsAuthenticated || s.AccountDataLoaded));
    }

    internal readonly record struct SessionSnapshot(
        Guid SessionId,
        bool IsAlive,
        int? AccountId,
        bool IsAuthenticated,
        bool AccountDataLoaded);
}
