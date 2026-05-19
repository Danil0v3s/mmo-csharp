using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.UseCase;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Login.Server.UseCase;

public interface ILoginMmoAuth : IUseCaseAsync<ILoginMmoAuth.Input, ILoginMmoAuth.Output>
{
    public record Input(object LoginSessionData, bool IsServer);

    public record Output(int ResultCode);

    public interface ITempSessionData
    {
        string UserId { get; set; }
        string Password { get; set; }
        int PasswordEnc { get; set; }
        string Md5Key { get; set; }
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
    private static readonly object RegistrationLock = new();
    // Per-IP sliding window. rAthena tracks new-account creation count
    // per source IP (mod_num_regs(ip)); without per-IP scoping a single
    // automation client at 1 req/s rate-limits the entire server.
    private static readonly Dictionary<string, Queue<DateTime>> RegistrationWindowByIp = new();

    public async Task<ILoginMmoAuth.Output> ExecuteAsync(ILoginMmoAuth.Input input)
    {
        // Handle both LoginSessionData and ITempSessionData
        string userId, password;
        int passwordEncMode = 0;
        string md5Key = string.Empty;
        object sessionObject = input.LoginSessionData;

        if (sessionObject is LoginSessionData loginSd)
        {
            userId = loginSd.UserId;
            password = loginSd.Password;
            passwordEncMode = loginSd.PasswordEnc;
            md5Key = loginSd.Md5Key;
        }
        else if (sessionObject is ILoginMmoAuth.ITempSessionData tempSd)
        {
            userId = tempSd.UserId;
            password = tempSd.Password;
            passwordEncMode = tempSd.PasswordEnc;
            md5Key = tempSd.Md5Key;
        }
        else
        {
            logger.LogError("Invalid session data type: {Type}", sessionObject.GetType().Name);
            return new ILoginMmoAuth.Output(0); // Invalid
        }

        if (configuration.UseDnsbl)
        {
            var remoteIp = ExtractRemoteIp(sessionObject);
            if (remoteIp != null && await IsDnsBlacklistedAsync(remoteIp))
            {
                logger.LogInformation("DNSBL rejected login from {Ip}", remoteIp);
                return new ILoginMmoAuth.Output(3);
            }
        }

        string lookupUserId = userId;
        var account = await loginRepository.GetByUserIdAsync(lookupUserId);

        if (account == null && configuration.NewAccountFlag && !input.IsServer)
        {
            var createResult = await TryCreateNewAccountAsync(lookupUserId, password, passwordEncMode, sessionObject);
            if (createResult == NewAccountCreateResult.Success && lookupUserId.Length > 2)
            {
                lookupUserId = lookupUserId[..^2];
                userId = lookupUserId;
                account = await loginRepository.GetByUserIdAsync(lookupUserId);
            }
            else if (createResult != NewAccountCreateResult.NotApplicable)
            {
                return new ILoginMmoAuth.Output((int)createResult);
            }
        }

        if (account == null)
        {
            logger.LogInformation("Unknown account (account: {Account})", lookupUserId);
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

        if (!CheckPasswords(account.UserPass, processedPassword, passwordEncMode, md5Key))
        {
            logger.LogInformation("Invalid password (account: {Account})", userId);
            return new ILoginMmoAuth.Output(1);
        }

        var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (account.ExpirationTime != 0 && account.ExpirationTime < unixNow)
        {
            logger.LogInformation("Connection refused (account: {Account}, expired ID)", userId);
            return new ILoginMmoAuth.Output(2);
        }

        if (account.UnbanTime != 0 && account.UnbanTime > unixNow)
        {
            logger.LogInformation(
                "Connection refused (account: {Account}, banned until {UnbanTime})",
                userId,
                DateTimeOffset.FromUnixTimeSeconds(account.UnbanTime).ToString(configuration.DateFormat, CultureInfo.InvariantCulture));
            return new ILoginMmoAuth.Output(6);
        }

        if (account.State != 0)
        {
            logger.LogInformation("Connection refused (account: {Account}, state: {State})", userId, account.State);
            return new ILoginMmoAuth.Output((int)account.State - 1);
        }

        if (configuration.ClientHashCheck > 0 && !input.IsServer)
        {
            if (sessionObject is not LoginSessionData loginSession)
            {
                return new ILoginMmoAuth.Output(5);
            }

            var clientHashHex = loginSession.HasClientHash != 0 ? ConvertToHex(loginSession.ClientHash) : string.Empty;

            bool matched = configuration.ClientHashNodes
                .OrderByDescending(node => node.GroupId)
                .Where(node => account.GroupId >= node.GroupId)
                .Any(node =>
                    string.IsNullOrWhiteSpace(node.Md5Hash) ||
                    (loginSession.HasClientHash != 0 &&
                     string.Equals(node.Md5Hash.Trim(), clientHashHex, StringComparison.OrdinalIgnoreCase)));

            if (!matched)
            {
                logger.LogInformation(
                    loginSession.HasClientHash == 0
                        ? "Client didn't send client hash (account: {Account})"
                        : "Invalid client hash (account: {Account}, sent md5: {ClientHash})",
                    userId,
                    clientHashHex);
                return new ILoginMmoAuth.Output(5);
            }
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
                account.LastIp = session._socket.RemoteEndPoint?.ToString() ?? string.Empty;
                account.UnbanTime = 0;
                account.LoginCount++;
                await PersistAccountLoginStateAsync(account);

                if (configuration.UseWebAuthToken)
                {
                    session.WebAuthToken = account.WebAuthToken ?? string.Empty;
                }
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
            await PersistAccountLoginStateAsync(account);

            if (configuration.UseWebAuthToken)
            {
                tempSd.WebAuthToken = account.WebAuthToken ?? string.Empty;
            }
        }

        return new ILoginMmoAuth.Output(-1);
    }

    private bool CheckPasswords(string accountPassword, string providedPassword, int passwordEncMode, string md5Key)
    {
        if (passwordEncMode == 0)
        {
            return accountPassword.Equals(providedPassword, StringComparison.OrdinalIgnoreCase);
        }

        bool matched = false;

        if ((passwordEncMode & 0x01) != 0)
        {
            var expected = ConvertToMd5($"{md5Key}{accountPassword}");
            matched |= expected.Equals(providedPassword, StringComparison.OrdinalIgnoreCase);
        }

        if ((passwordEncMode & 0x02) != 0)
        {
            var expected = ConvertToMd5($"{accountPassword}{md5Key}");
            matched |= expected.Equals(providedPassword, StringComparison.OrdinalIgnoreCase);
        }

        return matched;
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

    private static string ConvertToHex(byte[] input)
    {
        return BitConverter.ToString(input).Replace("-", "").ToLowerInvariant();
    }

    private async Task PersistAccountLoginStateAsync(Core.Database.Entities.LoginEntity account)
    {
        if (!configuration.UseWebAuthToken || account.Sex == 'S')
        {
            await loginRepository.UpdateAsync(account);
            return;
        }

        const int maxRetries = 20;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            account.WebAuthToken = GenerateWebAuthToken();
            account.WebAuthTokenEnabled = 1;

            try
            {
                await loginRepository.UpdateAsync(account);
                return;
            }
            catch (DbUpdateException ex) when (IsDuplicateWebAuthTokenError(ex) && attempt < maxRetries)
            {
                // Retry with a new random token.
            }
            catch (DbUpdateException ex) when (IsDuplicateWebAuthTokenError(ex))
            {
                logger.LogError(
                    ex,
                    "Failed to generate unique web_auth_token for account {AccountId} after {Retries} retries. Disabling token for this login.",
                    account.AccountId,
                    maxRetries);

                account.WebAuthToken = null;
                account.WebAuthTokenEnabled = 0;
                await loginRepository.UpdateAsync(account);
                return;
            }
        }
    }

    private static bool IsDuplicateWebAuthTokenError(DbUpdateException ex)
    {
        Exception? current = ex;
        while (current != null)
        {
            if (current is MySqlException mysqlEx && mysqlEx.Number == 1062)
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private static string GenerateWebAuthToken()
    {
        const int webAuthTokenLength = 17;
        var source = $"{Guid.NewGuid():N}{Random.Shared.NextInt64()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash).ToLowerInvariant()[..(webAuthTokenLength - 1)];
    }

    private async Task<NewAccountCreateResult> TryCreateNewAccountAsync(
        string candidateUserId,
        string password,
        int passwordEncMode,
        object sessionObject)
    {
        if (candidateUserId.Length <= 2 || password.Length == 0 || passwordEncMode != 0)
        {
            return NewAccountCreateResult.NotApplicable;
        }

        if (candidateUserId[^2] != '_')
        {
            return NewAccountCreateResult.NotApplicable;
        }

        var sexSuffix = char.ToUpperInvariant(candidateUserId[^1]);
        if (sexSuffix is not ('M' or 'F'))
        {
            return NewAccountCreateResult.NotApplicable;
        }

        var accountName = candidateUserId[..^2];
        if (accountName.Length < configuration.AccountNameMinLength || password.Length < configuration.PasswordMinLength)
        {
            logger.LogInformation("Rejected auto-create for invalid account/password length ({Account})", accountName);
            return NewAccountCreateResult.UnregisteredId;
        }

        // rAthena login.cpp:233 — count source-IP registrations within
        // the time_allowed window; rate-limit the IP, not the world.
        var registrationIp = ExtractRemoteIp(sessionObject)?.ToString() ?? "unknown";
        lock (RegistrationLock)
        {
            var now = DateTime.UtcNow;
            if (!RegistrationWindowByIp.TryGetValue(registrationIp, out var window))
            {
                window = new Queue<DateTime>();
                RegistrationWindowByIp[registrationIp] = window;
            }
            while (window.Count > 0 &&
                   (now - window.Peek()).TotalSeconds > configuration.RegistrationInterval)
            {
                window.Dequeue();
            }

            if (window.Count >= configuration.AllowedRegistrations)
            {
                logger.LogInformation(
                    "Registration limit reached for IP {Ip}, rejecting auto-create for {Account}",
                    registrationIp, accountName);
                return NewAccountCreateResult.RejectedFromServer;
            }
        }

        if (await loginRepository.GetByUserIdAsync(accountName) != null)
        {
            logger.LogInformation("Attempted auto-create for existing account {Account}", accountName);
            return NewAccountCreateResult.IncorrectPassword;
        }

        var expirationUnix = configuration.StartLimitedTime >= 0
            ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() + configuration.StartLimitedTime
            : 0;

        var remoteIp = ExtractRemoteIp(sessionObject)?.ToString() ?? string.Empty;
        await loginRepository.AddAsync(new Core.Database.Entities.LoginEntity
        {
            UserId = accountName,
            UserPass = password,
            Sex = sexSuffix,
            Email = "a@a.com",
            GroupId = 0,
            State = 0,
            UnbanTime = 0,
            ExpirationTime = (uint)Math.Max(expirationUnix, 0),
            LoginCount = 0,
            LastLogin = null,
            LastIp = remoteIp,
            Birthdate = null,
            CharacterSlots = (byte)Math.Clamp(configuration.CharactersPerAccount, 0, byte.MaxValue),
            Pincode = string.Empty,
            PincodeChange = 0,
            VipTime = 0,
            OldGroup = 0,
            WebAuthToken = null,
            WebAuthTokenEnabled = 0
        });

        lock (RegistrationLock)
        {
            if (!RegistrationWindowByIp.TryGetValue(registrationIp, out var window))
            {
                window = new Queue<DateTime>();
                RegistrationWindowByIp[registrationIp] = window;
            }
            window.Enqueue(DateTime.UtcNow);
        }

        logger.LogInformation("Account auto-created via suffix flow: {Account} ({Sex})", accountName, sexSuffix);
        return NewAccountCreateResult.Success;
    }

    private async Task<bool> IsDnsBlacklistedAsync(System.Net.IPAddress remoteIp)
    {
        var octets = remoteIp.ToString().Split('.');
        if (octets.Length != 4)
        {
            return false;
        }

        var reversedIp = string.Join('.', octets.Reverse());
        var servers = configuration.DnsblServers.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var dnsblServer in servers)
        {
            try
            {
                var queryHost = $"{reversedIp}.{dnsblServer}";
                var result = await System.Net.Dns.GetHostAddressesAsync(queryHost);
                if (result.Length > 0)
                {
                    return true;
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                // NXDOMAIN/lookup failures mean "not listed" for this server.
            }
        }

        return false;
    }

    private static System.Net.IPAddress? ExtractRemoteIp(object sessionObject)
    {
        if (sessionObject is LoginSessionData loginSession &&
            loginSession._socket.RemoteEndPoint is System.Net.IPEndPoint endpoint)
        {
            return endpoint.Address;
        }

        return null;
    }

    private enum NewAccountCreateResult
    {
        NotApplicable = int.MinValue,
        Success = -1,
        UnregisteredId = 0,
        IncorrectPassword = 1,
        RejectedFromServer = 3
    }
}
