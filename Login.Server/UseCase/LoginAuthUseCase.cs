using System.Net;
using System.Security.Cryptography;
using System.Text;
using Core.Server;
using Core.Server.Network;
using Core.Server.Packets.Out.AC;
using Core.Server.Packets.ServerPackets;
using Core.Timer;
using Login.Server.Model;
using Login.Server.Repository.Api;
using Login.Server.Security;

namespace Login.Server.UseCase;

public interface ILoginAuthUseCase
{
    Task HandleLoginRequestAsync(
        LoginSessionData session,
        string username,
        string password,
        byte clientType,
        int passwordEncMode,
        bool md5PacketMode);
}

public sealed class LoginAuthUseCase(
    ILogger<LoginAuthUseCase> logger,
    ILoginMmoAuth loginMmoAuth,
    ILoginServerRuntime loginServerRuntime,
    ICharServerRegistry charServerRegistry,
    ICharServerIpcService charServerIpcService,
    SessionManager sessionManager,
    LoginServerConfiguration loginConfig,
    ILoginDataRepository loginDataRepository,
    ILoginSecurityService loginSecurityService
) : ILoginAuthUseCase
{
    private static readonly TimeSpan AuthExecutionTimeout = TimeSpan.FromSeconds(5);

    public async Task HandleLoginRequestAsync(
        LoginSessionData session,
        string username,
        string password,
        byte clientType,
        int passwordEncMode,
        bool md5PacketMode)
    {
        try
        {
            session.UserId = username;
            session.ClientType = clientType;
            session.PasswordEnc = passwordEncMode;
            session.Password = password;
    
            if (md5PacketMode && loginConfig.UseMd5Passwords)
            {
                await SendAuthFailureAsync(session, 3);
                return;
            }
    
            if (loginConfig.UseMd5Passwords && passwordEncMode == 0)
            {
                session.Password = ConvertToMd5(password);
            }
    
            logger.LogInformation(
                "Request for connection of {Username} (ip: {Ip}, mode: {Mode})",
                session.UserId,
                session._socket.LocalEndPoint,
                passwordEncMode);
    
            logger.LogInformation("Executing login auth for session {SessionId}", session.SessionId);
            var authTask = loginMmoAuth.ExecuteAsync(new ILoginMmoAuth.Input(session, false));
            var completedTask = await Task.WhenAny(authTask, Task.Delay(AuthExecutionTimeout));
            if (completedTask != authTask)
            {
                logger.LogWarning(
                    "Login auth timed out after {TimeoutMs}ms for session {SessionId} (user {Username})",
                    (int)AuthExecutionTimeout.TotalMilliseconds,
                    session.SessionId,
                    username);
                await SendAuthFailureAsync(session, 1);
                return;
            }
    
            var result = await authTask;
            var updatedSession = sessionManager.GetSession(session.SessionId) as LoginSessionData ?? session;
    
            if (result.ResultCode == -1)
            {
                await OnAuthSuccessAsync(updatedSession);
            }
            else
            {
                await SendAuthFailureAsync(updatedSession, result.ResultCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing login for {Username}", username);
            session.Disconnect(DisconnectReason.Kicked);
        }
    }

    private async Task OnAuthSuccessAsync(LoginSessionData sd)
    {
        var remoteIp = GetRemoteIp(sd);
    
        if (loginServerRuntime.State != ServerState.Running)
        {
            SendNotifyBan(sd, 1);
            return;
        }
    
        if (loginConfig.GroupIdToConnect >= 0 && sd.GroupId != loginConfig.GroupIdToConnect)
        {
            SendNotifyBan(sd, 1);
            return;
        }
        else if (loginConfig.MinGroupIdToConnect >= 0 && loginConfig.GroupIdToConnect == -1 && sd.GroupId < loginConfig.MinGroupIdToConnect)
        {
            SendNotifyBan(sd, 1);
            return;
        }
    
        if (!charServerRegistry.HasActiveCharServers())
        {
            SendNotifyBan(sd, 1);
            return;
        }
    
        var canContinueLoginFlow = await HandleExistingOnlineUserStateAsync(sd);
        if (!canContinueLoginFlow)
        {
            return;
        }

        logger.LogInformation("Connection of the account {Account} accepted", sd.UserId);
        if (remoteIp != null)
        {
            await loginSecurityService.LogLoginAttemptAsync(remoteIp, sd.UserId, 100, "login ok");
        }
    
        var acceptLoginPacket = new AC_ACCEPT_LOGIN
        {
            LoginId1 = (uint)sd.LoginId1,
            AID = (uint)sd.AccountId,
            LoginId2 = (uint)sd.LoginId2,
            LastIp = 0,
            LastLogin = "",
            Sex = (byte)(sd.Sex == 'F' ? 0 : sd.Sex == 'M' ? 1 : 3),
            Token = sd.WebAuthToken,
            CharServers = charServerRegistry.GetActiveCharServersWithIds()
                .Select(charServer => new AC_ACCEPT_LOGIN_sub
                {
                    Ip = charServer.Data.Ip,
                    Port = charServer.Data.Port,
                    Name = charServer.Data.Name,
                    // PACKETVER ≥ 20170726 reinterprets this field as a
                    // 0-4 color code; mirrors rAthena login_get_usercount.
                    Users = CharServerUserCountClassifier.Classify(charServer.Data.Users, loginConfig),
                    Type = charServer.Data.Type,
                    New = charServer.Data.New,
                    Unknown = new byte[128]
                })
                .ToArray()
        };
    
        sd.EnqueuePacket(acceptLoginPacket);
        loginDataRepository.AddAuthNode(sd);
    
        var onlineUser = (await loginDataRepository.AddOnlineUser(-1, sd.AccountId)) with
        {
            WaitingDisconnect = Scheduler.Schedule(
                this.OnDisconnectTimer,
                new OnDisconnectTimerData(sd.AccountId),
                TimeSpan.FromMilliseconds(30_000L)
            )
        };
        loginDataRepository.Update(onlineUser);
    }

    private async Task<bool> HandleExistingOnlineUserStateAsync(LoginSessionData sd)
    {
        var data = loginDataRepository.GetOnlineUser(sd.AccountId);
        if (data == null)
        {
            return true;
        }

        if (data.CharServer > -1)
        {
            return await HandleConnectedCharServerUserAsync(sd, data);
        }

        if (data.CharServer == -1)
        {
            loginDataRepository.RemoveAuthNode(sd.AccountId);
            await loginDataRepository.RemoveOnlineUser(sd.AccountId);
        }

        return true;
    }

    private async Task<bool> HandleConnectedCharServerUserAsync(LoginSessionData sd, OnlineLoginData data)
    {
        logger.LogInformation("User {Username} is already online", sd.UserId);
        var activeServer = charServerRegistry.GetCharServer(data.CharServer);
        if (activeServer == null || string.IsNullOrWhiteSpace(activeServer.Name))
        {
            loginDataRepository.RemoveAuthNode(sd.AccountId);
            await loginDataRepository.RemoveOnlineUser(sd.AccountId);
            return true;
        }

        await charServerIpcService.ForceDisconnectAccountFromCharServersAsync(sd.AccountId);

        if (data.WaitingDisconnect == Scheduler.InvalidTimer)
        {
            data = data with
            {
                WaitingDisconnect = Scheduler.Schedule(
                    this.OnDisconnectTimer,
                    new OnDisconnectTimerData(sd.AccountId),
                    TimeSpan.FromMilliseconds(30_000L)
                )
            };
            loginDataRepository.Update(data);
        }

        SendNotifyBan(sd, 8);
        return false;
    }
    
    private record struct OnDisconnectTimerData(int AccountId);
    
    private async ValueTask OnDisconnectTimer(object? state, TimerId timerId, long arg3)
    {
        if (state is not OnDisconnectTimerData data)
        {
            return;
        }
    
        var p = loginDataRepository.GetOnlineUser(data.AccountId);
        if (p != null && p.WaitingDisconnect == timerId && p.AccountId.Value == data.AccountId)
        {
            p = p with { WaitingDisconnect = Scheduler.InvalidTimer };
            loginDataRepository.Update(p);
            await loginDataRepository.RemoveOnlineUser(data.AccountId);
            loginDataRepository.RemoveAuthNode(data.AccountId);
        }
    }
    
    private Task SendAuthFailureAsync(LoginSessionData sd, int result)
    {
        var packet = new AC_REFUSE_LOGIN
        {
            Error = (uint)result,
            UnblockTime = string.Empty
        };
    
        sd.EnqueuePacket(packet);
        return HandleAuthFailureSecurityAsync(sd, result);
    }
    
    private void SendNotifyBan(LoginSessionData sd, byte result)
    {
        var packet = new SC_NOTIFY_BAN
        {
            Result = result
        };
    
        sd.EnqueuePacket(packet);
    }
    
    private async Task HandleAuthFailureSecurityAsync(LoginSessionData sd, int result)
    {
        var remoteIp = GetRemoteIp(sd);
        if (remoteIp == null)
        {
            return;
        }
    
        await loginSecurityService.LogLoginAttemptAsync(
            remoteIp,
            sd.UserId,
            result,
            $"login failure {result}");
    
        if (result == 0 || result == 1)
        {
            await loginSecurityService.EnforceDynamicPasswordFailureBanAsync(remoteIp);
        }
    }
    
    private static IPAddress? GetRemoteIp(LoginSessionData session)
    {
        return session._socket.RemoteEndPoint is IPEndPoint endpoint ? endpoint.Address : null;
    }
    
    private static string ConvertToMd5(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
