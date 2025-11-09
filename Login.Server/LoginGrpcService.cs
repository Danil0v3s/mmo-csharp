using Core.Server.IPC;
using Grpc.Core;

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

    public static void StoreSession(string token, long accountId, string username)
    {
        Sessions[token] = (accountId, username);
    }
}

