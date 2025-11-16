using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.UseCase;

namespace Login.Server.UseCase;

public interface ILoginMmoAuth : IUseCaseAsync<ILoginMmoAuth.Input, ILoginMmoAuth.Output>
{
    public record Input(LoginSessionData LoginSessionData, bool IsServer);

    public record Output(int ResultCode);
}

// int32 login_mmo_auth(struct login_session_data* sd, bool isServer)
internal sealed class LoginMmoAuth(
    ILogger<LoginMmoAuth> logger,
    ILoginRepository loginRepository,
    LoginServerConfiguration configuration,
    SessionManager sessionManager
) : ILoginMmoAuth
{
    public async Task<ILoginMmoAuth.Output> ExecuteAsync(ILoginMmoAuth.Input input)
    {
        var sd = input.LoginSessionData;
        var ip = sd._socket.LocalEndPoint;

        if (configuration.UseDnsbl)
        {
            // TODO: read login_config.use_dnsbl
        }

        var len = Math.Max(sd.UserId.Length, PacketConstants.MAP_NAME_LENGTH);

        if (configuration.NewAccountFlag)
        {
            // TODO: read login_config.new_account_flag
        }

        var account = await loginRepository.GetByEmailAsync(sd.UserId);

        if (account == null)
        {
            logger.LogInformation("Unknown account (account: {Account}, ip: {Ip})", sd.UserId, ip);
            return new ILoginMmoAuth.Output(0);
        }

        if (!input.IsServer && account.Sex == 'S')
        {
            logger.LogWarning("Connection refused: ip {Ip} tried to log into server account {Account}", ip, sd.UserId);
            return new ILoginMmoAuth.Output(0);
        }

        if (!CheckPasswords(account.UserPass, sd.Password))
        {
            logger.LogInformation("Invalid password (account: {Account}, ip: {Ip})", sd.UserId, ip);
            return new ILoginMmoAuth.Output(1);
        }

        // TODO expiration time
        // TODO unban time

        if (account.State != 0)
        {
            logger.LogInformation("Connection refused (account: {Account}, state: {State}, ip: {Ip})", sd.UserId, account.State, ip);
            return new ILoginMmoAuth.Output((int)account.State - 1);
        }

        if (configuration.ClientHashCheck > 0 && !input.IsServer)
        {
            // TODO hash check && !isServer
        }

        logger.LogInformation("Authentication accepted (account: {Account}, id: {AccountId}, ip: {Ip})", sd.UserId, account.AccountId, ip);

        if (sessionManager.GetSession(sd.SessionId) is LoginSessionData session)
        {
            var random = new Random();
            session.AccountId = account.AccountId;
            session.LoginId1 = random.Next(1, int.MaxValue);
            session.LoginId2 = random.Next(1, int.MaxValue);
            session.LastLogin = account.LastLogin ?? DateTime.Now;
            session.Sex = account.Sex;
            session.GroupId = account.GroupId;
            
            account.LastLogin = DateTime.Now;
            account.LastIp = session._socket.LocalEndPoint?.ToString() ?? string.Empty;
            account.UnbanTime = 0;
            account.LoginCount++;
            _ = loginRepository.UpdateAsync(account);
            
            // TODO: web_auth_token
            sessionManager.UpdateSession(session);

            if (session.Sex != 'S' && sd.AccountId < 2000000)
            {
                logger.LogWarning("Account {Account} has account id {AccountId}! Account IDs must be over {MinAccountId} to work properly", sd.UserId, sd.AccountId, 2000000);
            }
        }

        return new ILoginMmoAuth.Output(-1);
    }

    private bool CheckPasswords(string pass, string confirm)
    {
        return pass.Equals(confirm, StringComparison.InvariantCultureIgnoreCase);
        // TODO check for encryption
    }
}