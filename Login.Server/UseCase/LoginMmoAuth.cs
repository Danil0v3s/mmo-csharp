using Core.Database.Repositories.Api;
using Core.Server.Packets;
using Core.Server.UseCase;

namespace Login.Server.UseCase;

interface ILoginMmoAuth : IUseCaseAsync<ILoginMmoAuth.Input, ILoginMmoAuth.Output>
{
    public record Input(LoginSessionData LoginSessionData, bool IsServer);

    public record Output(int ResultCode);
}

// int32 login_mmo_auth(struct login_session_data* sd, bool isServer)
internal sealed class LoginMmoAuth(
    ILogger<LoginMmoAuth> logger,
    ILoginRepository loginRepository
) : ILoginMmoAuth
{
    public async Task<ILoginMmoAuth.Output> ExecuteAsync(ILoginMmoAuth.Input input)
    {
        var sd = input.LoginSessionData;
        var ip = sd._socket.LocalEndPoint;
        // TODO: read login_config.use_dnsbl

        var len = Math.Max(sd.UserId.Length, PacketConstants.MAP_NAME_LENGTH);

        // TODO: read login_config.new_account_flag
        
        var account = await loginRepository.GetByEmailAsync(sd.UserId);

        if (account == null)
        {
            logger.LogInformation("Unknown account (account: {Account}, ip: {Ip})", sd.UserId, ip);
            return new ILoginMmoAuth.Output(0);
        }

        if (!input.IsServer && account.Sex == "S")
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
        
        // TODO hash check && !isServer
        
        logger.LogInformation("Authentication accepted (account: {Account}, id: {AccountId}, ip: {Ip})", sd.UserId, account.AccountId, ip);
        
        // update session data
        // sd->account_id = acc.account_id;
        // sd->login_id1 = rnd_value(1u, UINT32_MAX);
        // sd->login_id2 = rnd_value(1u, UINT32_MAX);
        // safestrncpy(sd->lastlogin, acc.lastlogin, sizeof(sd->lastlogin));
        // sd->sex = acc.sex;
        // sd->group_id = acc.group_id;
        //
        // // update account data
        // timestamp2string(acc.lastlogin, sizeof(acc.lastlogin), time(nullptr), "%Y-%m-%d %H:%M:%S");
        // safestrncpy(acc.last_ip, ip, sizeof(acc.last_ip));
        // acc.unban_time = 0;
        // acc.logincount++;
        // accounts->save(accounts, &acc, true);
        //
        // if( login_config.use_web_auth_token ){
        //     safestrncpy( sd->web_auth_token, acc.web_auth_token, WEB_AUTH_TOKEN_LENGTH );
        // }
        //
        // if( sd->sex != 'S' && sd->account_id < START_ACCOUNT_NUM )
        //     ShowWarning("Account %s has account id %d! Account IDs must be over %d to work properly!\n", sd->userid, sd->account_id, START_ACCOUNT_NUM);
        
        return new ILoginMmoAuth.Output(-1);
    }
    
    private bool CheckPasswords(string pass, string confirm)
    {
        return pass.Equals(confirm, StringComparison.InvariantCultureIgnoreCase);
        // TODO check for encryption
    }
}

