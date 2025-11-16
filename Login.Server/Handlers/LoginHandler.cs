using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;
using Core.Server.Packets.Out.AC;
using Core.Server.Packets.ServerPackets;
using Login.Server.UseCase;

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
    LoginServerConfiguration loginConfig
) : IPacketHandler<LoginSessionData, CA_LOGIN>
{
    public async Task HandleAsync(LoginSessionData session, CA_LOGIN packet)
    {
        try
        {
            session.UserId = packet.Username;
            session.ClientType = packet.Clienttype;
            session.Password = packet.Password;

            logger.LogInformation("Request for connection of {Username} (ip: {Ip})", session.SessionId, session._socket.LocalEndPoint);

            // read config for use_md5_passwds
            session.PasswordEnc = false;
            var result = await loginMmoAuth.ExecuteAsync(new ILoginMmoAuth.Input(session, false));
            var updatedSession = sessionManager.GetSession(session.SessionId) as LoginSessionData ?? throw new InvalidOperationException("Session not found");

            if (result.ResultCode == -1)
            {
                OnAuthSuccess(updatedSession);
            }
            else
            {
                OnAuthFailure(updatedSession, result.ResultCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing login for {Username}", packet.Username);
            session.Disconnect(DisconnectReason.Kicked);
        }
    }

    private async void OnAuthSuccess(LoginSessionData sd)
    {
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
        } else if (loginConfig.MinGroupIdToConnect >= 0 && loginConfig.GroupIdToConnect == -1 && sd.GroupId < loginConfig.MinGroupIdToConnect)
        {
            // ShowStatus("Connection refused: the minimum group id required for connection is %d (account: %s, group: %d).\n", login_config.min_group_id_to_connect, sd->userid, sd->group_id);
            SendNotifyBan(sd, 1);
            return;
        }

        if (!loginServer.ServerConnections.GetSessionsByType(ServerType.Char).Any())
        {
            // ShowStatus("Connection refused: there is no char-server online (account: %s).\n", sd->userid);
            SendNotifyBan(sd, 1);
            return;
        }
        
        
    }

    private async void OnAuthFailure(LoginSessionData sd, int result)
    {
        // TODO: implement other paths
        var packet = new AC_REFUSE_LOGIN
        {
            Error = (uint)result,
            UnblockTime = string.Empty
        };
        
        sd.EnqueuePacket(packet);
    }

    private void SendNotifyBan(LoginSessionData sd, byte result)
    {
        var packet = new SC_NOTIFY_BAN
        {
            Result = result
        };
            
        sd.EnqueuePacket(packet);
    }
}