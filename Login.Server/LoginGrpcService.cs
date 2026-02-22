using Core.Server.IPC;
using Grpc.Core;
using Login.Server.Handlers;
using Login.Server.Repository.Api;
using Login.Server.UseCase;

namespace Login.Server;

public class LoginGrpcService : LoginService.LoginServiceBase
{
    // In-memory session storage (should be Redis/database in production)
    private static readonly Dictionary<string, (long AccountId, string Username)> Sessions = new();

    public override Task<ValidateSessionResponse> ValidateSession(
        ValidateSessionRequest request, 
        ServerCallContext context)
    {
        var response = new ValidateSessionResponse();

        if (Sessions.TryGetValue(request.SessionToken, out var session))
        {
            response.IsValid = true;
            response.AccountId = session.AccountId;
            response.Username = session.Username;
        }
        else
        {
            response.IsValid = false;
        }

        return Task.FromResult(response);
    }

    public override Task<AccountInfoResponse> GetAccountInfo(
        AccountInfoRequest request,
        ServerCallContext context)
    {
        // TODO: Query from database
        var response = new AccountInfoResponse
        {
            AccountId = request.AccountId,
            Username = "TestUser",
            Email = "test@example.com",
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return Task.FromResult(response);
    }

    public override async Task<CharacterServerRegistrationResponse> RegisterCharacterServer(
        CharacterServerRegistrationRequest request,
        ServerCallContext context)
    {
        // Use the CharServerGrpcHandler to process the registration
        var handler = new CharServerGrpcHandler(
            Logger,
            LoginMmoAuth,
            LoginServer,
            LoginConfig
        );

        return await handler.RegisterCharacterServerAsync(request, context);
    }

    public override Task<CharacterServerAuthResponse> AuthenticateAccountForCharServer(
        CharacterServerAuthRequest request,
        ServerCallContext context)
    {
        var expectedSex = request.Sex switch
        {
            0 => 'F',
            1 => 'M',
            2 => 'S',
            _ => '\0'
        };

        if (LoginDataRepository.TryConsumeAuthNode(
                request.AccountId,
                request.LoginId1,
                request.LoginId2,
                expectedSex,
                out var authNode) && authNode != null)
        {
            LoginDataRepository.SetOnlineUserCharServer(request.AccountId, (int)request.CharServerId);

            return Task.FromResult(new CharacterServerAuthResponse
            {
                Success = true,
                AccountId = authNode.AccountId,
                LoginId1 = authNode.LoginId1,
                LoginId2 = authNode.LoginId2,
                Sex = request.Sex,
                RequestId = request.RequestId,
                ClientType = authNode.ClientType
            });
        }

        return Task.FromResult(new CharacterServerAuthResponse
        {
            Success = false,
            AccountId = request.AccountId,
            LoginId1 = request.LoginId1,
            LoginId2 = request.LoginId2,
            Sex = request.Sex,
            RequestId = request.RequestId,
            ErrorMessage = "Authentication entry not found or already consumed"
        });
    }

    public override Task<CharacterServerUserCountUpdateResponse> UpdateCharacterServerUserCount(
        CharacterServerUserCountUpdateRequest request,
        ServerCallContext context)
    {
        LoginServer.UpdateCharServerUserCount(request.ServerId, (ushort)request.UserCount);
        return Task.FromResult(new CharacterServerUserCountUpdateResponse { Success = true });
    }

    public override Task<AccountStatusUpdateResponse> NotifyAccountStatus(
        AccountStatusUpdateRequest request,
        ServerCallContext context)
    {
        if (request.Online)
        {
            LoginDataRepository.SetOnlineUserCharServer(request.AccountId, request.CharServerId);
        }
        else
        {
            LoginDataRepository.RemoveOnlineUser(request.AccountId);
            LoginDataRepository.RemoveAuthNode(request.AccountId);
        }

        return Task.FromResult(new AccountStatusUpdateResponse { Success = true });
    }

    public override Task<CharacterServerListResponse> ListCharacterServers(
        CharacterServerListRequest request,
        ServerCallContext context)
    {
        var response = new CharacterServerListResponse();
        foreach (var (serverId, server) in LoginServer.GetActiveCharServersWithIds())
        {
            response.Servers.Add(new CharacterServerEntry
            {
                ServerId = serverId,
                ServerName = server.Name,
                Ip = server.Ip,
                Port = server.Port,
                Users = server.Users,
                ServerType = server.Type,
                IsNew = server.New != 0
            });
        }

        return Task.FromResult(response);
    }

    public static void StoreSession(string token, long accountId, string username)
    {
        Sessions[token] = (accountId, username);
    }

    // Dependencies for the new method
    private readonly ILogger<LoginGrpcService> Logger;
    private readonly ILoginMmoAuth LoginMmoAuth;
    private readonly LoginServerImpl LoginServer;
    private readonly LoginServerConfiguration LoginConfig;
    private readonly ILoginDataRepository LoginDataRepository;

    public LoginGrpcService(
        ILogger<LoginGrpcService> logger,
        ILoginMmoAuth loginMmoAuth,
        LoginServerImpl loginServer,
        LoginServerConfiguration loginConfig,
        ILoginDataRepository loginDataRepository)
    {
        Logger = logger;
        LoginMmoAuth = loginMmoAuth;
        LoginServer = loginServer;
        LoginConfig = loginConfig;
        LoginDataRepository = loginDataRepository;
    }
}
