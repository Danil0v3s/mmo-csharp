using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;
using Core.Server.Packets.Out.AC;
using Core.Server.Packets.ServerPackets;
using Core.Timer;
using Login.Server.Repository.Api;
using Login.Server.Security;
using Login.Server.UseCase;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Login.Server.Handlers;

/// <summary>
/// Handles CA_LOGIN packets for user authentication.
/// </summary>
[PacketHandler(PacketHeader.CA_LOGIN)]
public class LoginHandler(
    ILogger<LoginHandler> logger,
    ILoginMmoAuth loginMmoAuth,
    LoginServerImpl loginServer,
    SessionManager sessionManager,
    LoginServerConfiguration loginConfig,
    ILoginDataRepository loginDataRepository,
    ILoginSecurityService loginSecurityService
) : IPacketHandler<LoginSessionData, CA_LOGIN>
{
    public async Task HandleAsync(LoginSessionData session, CA_LOGIN packet)
    {
        await HandleLoginRequestAsync(session, packet.Username, packet.Password, packet.Clienttype, 0, false);
    }

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

            var result = await loginMmoAuth.ExecuteAsync(new ILoginMmoAuth.Input(session, false));
            var updatedSession = sessionManager.GetSession(session.SessionId) as LoginSessionData ??
                                 throw new InvalidOperationException("Session not found");

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

    public Task HandleClientHashAsync(LoginSessionData session, byte[] hash)
    {
        session.HasClientHash = 1;
        session.ClientHash = hash;
        return Task.CompletedTask;
    }

    public Task HandleHashRequestAsync(LoginSessionData session)
    {
        session.Md5Key = GenerateMd5Salt(16);
        var packet = new AC_ACK_HASH
        {
            PacketLength = (short)(4 + session.Md5Key.Length),
            Salt = session.Md5Key
        };

        session.EnqueuePacket(packet);
        return Task.CompletedTask;
    }

    public Task HandleOtpAuthAsync(LoginSessionData session)
    {
        var packet = new TC_RESULT
        {
            PacketLength = (short)(2 + 2 + 4 + 20 + 6),
            Type = 0,
            Unknown1 = "S1000",
            Unknown2 = "token"
        };

        session.EnqueuePacket(packet);
        return Task.CompletedTask;
    }

    private async Task OnAuthSuccessAsync(LoginSessionData sd)
    {
        var remoteIp = GetRemoteIp(sd);

        if (loginServer.State != ServerState.Running)
        {
            SendNotifyBan(sd, 1);
            return;
        }

        if (loginConfig.GroupIdToConnect >= 0 && sd.GroupId != loginConfig.GroupIdToConnect)
        {
            // ShowStatus("Connection refused: the required group id for connection is %d (account: %s, group: %d).\n", login_config.group_id_to_connect, sd->userid, sd->group_id);
            SendNotifyBan(sd, 1);
            return;
        }
        else if (loginConfig.MinGroupIdToConnect >= 0 && loginConfig.GroupIdToConnect == -1 && sd.GroupId < loginConfig.MinGroupIdToConnect)
        {
            // ShowStatus("Connection refused: the minimum group id required for connection is %d (account: %s, group: %d).\n", login_config.min_group_id_to_connect, sd->userid, sd->group_id);
            SendNotifyBan(sd, 1);
            return;
        }

        if (!loginServer.GetActiveCharServers().Any())
        {
            // ShowStatus("Connection refused: there is no char-server online (account: %s).\n", sd->userid);
            SendNotifyBan(sd, 1);
            return;
        }

        var data = loginDataRepository.GetOnlineUser(sd.AccountId);
        if (data != null)
        {
            if (data.CharServer > -1)
            {
                logger.LogInformation("User {Username} is already online", sd.UserId);
                var activeServer = loginServer.GetCharServer(data.CharServer);
                if (activeServer == null || string.IsNullOrWhiteSpace(activeServer.Name))
                {
                    // Stale online marker when the owning char-server is gone.
                    loginDataRepository.RemoveAuthNode(sd.AccountId);
                    loginDataRepository.RemoveOnlineUser(sd.AccountId);
                    data = null;
                    goto ContinueLoginFlow;
                }

                await ForceDisconnectDuplicateSessionAsync(sd.AccountId);

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

                SendNotifyBan(sd, 8); // Server still recognizes your last login
                return;
            }
            else if (data.CharServer == -1)
            {
                loginDataRepository.RemoveAuthNode(sd.AccountId);
                loginDataRepository.RemoveOnlineUser(sd.AccountId);
                data = null;
            }
        }

    ContinueLoginFlow:
        // login_log(ip, sd->userid, 100, "login ok");
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
            CharServers = loginServer.GetActiveCharServersWithIds()
                .Select(charServer => new AC_ACCEPT_LOGIN_sub
                {
                    Ip = charServer.Data.Ip,
                    Port = charServer.Data.Port,
                    Name = charServer.Data.Name,
                    Users = charServer.Data.Users,
                    Type = charServer.Data.Type,
                    New = charServer.Data.New,
                    Unknown = new byte[128]
                })
                .ToArray()
        };
        
        sd.EnqueuePacket(acceptLoginPacket);
        loginDataRepository.AddAuthNode(sd);
        
        // mark client as online
        var onlineUser = loginDataRepository.AddOnlineUser(-1, sd.AccountId) with
        {
            WaitingDisconnect = Scheduler.Schedule(
                this.OnDisconnectTimer,
                new OnDisconnectTimerData(sd.AccountId),
                TimeSpan.FromMilliseconds(30_000L)
            )
        };
        loginDataRepository.Update(onlineUser);
    }

    private record struct OnDisconnectTimerData(int AccountId);

    private ValueTask OnDisconnectTimer(object? state, TimerId timerId, long arg3)
    {
        if (state is not OnDisconnectTimerData data)
        {
            return ValueTask.CompletedTask;
        }

        var p = loginDataRepository.GetOnlineUser(data.AccountId);
        if (p != null && p.WaitingDisconnect == timerId && p.AccountId.Value == data.AccountId)
        {
            p = p with { WaitingDisconnect = Scheduler.InvalidTimer };
            loginDataRepository.Update(p);
            loginDataRepository.RemoveOnlineUser(data.AccountId);
            loginDataRepository.RemoveAuthNode(data.AccountId);
        }

        return ValueTask.CompletedTask;
    }

    public Task SendAuthFailureAsync(LoginSessionData sd, int result)
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

    private async Task ForceDisconnectDuplicateSessionAsync(int accountId)
    {
        var charSessions = loginServer.ServerConnections.GetSessionsByType(ServerType.Char).ToList();
        if (charSessions.Count == 0)
        {
            return;
        }

        foreach (var charSession in charSessions)
        {
            try
            {
                var client = new CharacterService.CharacterServiceClient(charSession.Channel);
                var response = await client.ForceDisconnectAccountAsync(new ForceDisconnectAccountRequest
                {
                    AccountId = accountId
                });

                logger.LogInformation(
                    "Duplicate-session force disconnect requested for account {AccountId} on char server {ServerName} (disconnected: {Disconnected})",
                    accountId,
                    charSession.ServerName,
                    response.DisconnectedSessions);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to request duplicate-session disconnect for account {AccountId} on char server {ServerName}",
                    accountId,
                    charSession.ServerName);
            }
        }
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

    private static string GenerateMd5Salt(int length)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);
        var output = new char[length];

        for (var i = 0; i < length; i++)
        {
            output[i] = alphabet[randomBytes[i] % alphabet.Length];
        }

        return new string(output);
    }
}
