using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.UseCase;

namespace Login.Server.UseCase;

public interface ILoginMmoAuth : IUseCaseAsync<ILoginMmoAuth.Input, ILoginMmoAuth.Output>
{
    public record Input(object LoginSessionData, bool IsServer);

    public record Output(int ResultCode);

    public interface ITempSessionData
    {
        string UserId { get; set; }
        string Password { get; set; }
        int AccountId { get; set; }
        char Sex { get; set; }
        byte ClientType { get; set; }
        byte GroupId { get; set; }
        string WebAuthToken { get; set; }
        int LoginId1 { get; set; }
        int LoginId2 { get; set; }
    }
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
        // Handle both LoginSessionData and ITempSessionData
        string userId, password;
        byte clientType = 0;
        object sessionObject = input.LoginSessionData;

        if (sessionObject is LoginSessionData loginSd)
        {
            userId = loginSd.UserId;
            password = loginSd.Password;
            clientType = loginSd.ClientType;
            var ip = loginSd._socket.LocalEndPoint;
        }
        else if (sessionObject is ILoginMmoAuth.ITempSessionData tempSd)
        {
            userId = tempSd.UserId;
            password = tempSd.Password;
            clientType = tempSd.ClientType;
        }
        else
        {
            logger.LogError("Invalid session data type: {Type}", sessionObject.GetType().Name);
            return new ILoginMmoAuth.Output(0); // Invalid
        }

        if (configuration.UseDnsbl)
        {
            // TODO: read login_config.use_dnsbl
        }

        var len = Math.Max(userId.Length, PacketConstants.MAP_NAME_LENGTH);

        if (configuration.NewAccountFlag)
        {
            // TODO: read login_config.new_account_flag
        }

        var account = await loginRepository.GetByEmailAsync(userId);

        if (account == null)
        {
            logger.LogInformation("Unknown account (account: {Account})", userId);
            return new ILoginMmoAuth.Output(0);
        }

        if (!input.IsServer && account.Sex == 'S')
        {
            logger.LogWarning("Connection refused: tried to log into server account {Account}", userId);
            return new ILoginMmoAuth.Output(0);
        }

        // For character server connections, process the password similar to C++ implementation
        string processedPassword = password;
        if (input.IsServer && configuration.UseMd5Passwords)
        {
            // In C++ implementation, character server passwords are MD5'd if use_md5_passwds is enabled
            processedPassword = ConvertToMd5(password);
        }

        if (!CheckPasswords(account.UserPass, processedPassword))
        {
            logger.LogInformation("Invalid password (account: {Account})", userId);
            return new ILoginMmoAuth.Output(1);
        }

        // TODO expiration time
        // TODO unban time

        if (account.State != 0)
        {
            logger.LogInformation("Connection refused (account: {Account}, state: {State})", userId, account.State);
            return new ILoginMmoAuth.Output((int)account.State - 1);
        }

        if (configuration.ClientHashCheck > 0 && !input.IsServer)
        {
            // TODO hash check && !isServer
        }

        logger.LogInformation("Authentication accepted (account: {Account}, id: {AccountId})", userId, account.AccountId);

        // Update session data if it's a LoginSessionData instance
        if (sessionObject is LoginSessionData sd)
        {
            var ip = sd._socket.LocalEndPoint;

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
        }
        // Update temp session data if it's a ITempSessionData instance
        else if (sessionObject is ILoginMmoAuth.ITempSessionData tempSd)
        {
            var random = new Random();
            tempSd.AccountId = account.AccountId;
            tempSd.LoginId1 = random.Next(1, int.MaxValue);
            tempSd.LoginId2 = random.Next(1, int.MaxValue);
            tempSd.Sex = account.Sex;
            tempSd.GroupId = account.GroupId;

            account.LastLogin = DateTime.Now;
            account.UnbanTime = 0;
            account.LoginCount++;
            _ = loginRepository.UpdateAsync(account);
        }

        return new ILoginMmoAuth.Output(-1);
    }

    private bool CheckPasswords(string pass, string confirm)
    {
        // If MD5 passwords are enabled, compare as MD5 hashes
        if (configuration.UseMd5Passwords)
        {
            string expectedHash = ConvertToMd5(pass);
            return expectedHash.Equals(confirm, StringComparison.InvariantCultureIgnoreCase);
        }

        // Otherwise, compare as plain text
        return pass.Equals(confirm, StringComparison.InvariantCultureIgnoreCase);
    }

    private string ConvertToMd5(string input)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert to hex string
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}