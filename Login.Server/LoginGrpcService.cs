using Core.Server.IPC;
using Grpc.Core;
using Login.Server.Handlers;
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

    public static void StoreSession(string token, long accountId, string username)
    {
        Sessions[token] = (accountId, username);
    }

    // Dependencies for the new method
    private readonly ILogger<LoginGrpcService> Logger;
    private readonly ILoginMmoAuth LoginMmoAuth;
    private readonly LoginServerImpl LoginServer;
    private readonly LoginServerConfiguration LoginConfig;

    public LoginGrpcService(
        ILogger<LoginGrpcService> logger,
        ILoginMmoAuth loginMmoAuth,
        LoginServerImpl loginServer,
        LoginServerConfiguration loginConfig)
    {
        Logger = logger;
        LoginMmoAuth = loginMmoAuth;
        LoginServer = loginServer;
        LoginConfig = loginConfig;
    }
}

