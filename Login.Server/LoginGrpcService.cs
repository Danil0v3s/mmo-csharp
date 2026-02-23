using Core.Server;
using Core.Server.IPC;
using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Grpc.Core;
using Login.Server.Handlers;
using Login.Server.Repository.Api;
using Login.Server.UseCase;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Login.Server;

public class LoginGrpcService : LoginService.LoginServiceBase
{
    // In-memory session storage (should be Redis/database in production)
    private static readonly Dictionary<string, (long AccountId, string Username)> Sessions = new();

    private readonly ILogger<LoginGrpcService> _logger;
    private readonly ICharServerRegistry _charServerRegistry;
    private readonly IServerConnectionService _connectionService;
    private readonly CharServerGrpcHandler _charServerGrpcHandler;
    private readonly LoginServerConfiguration _loginConfig;
    private readonly ILoginDataRepository _loginDataRepository;
    private readonly ILoginRepository _loginRepository;
    private readonly GameDbContext _dbContext;

    public LoginGrpcService(
        ILogger<LoginGrpcService> logger,
        ICharServerRegistry charServerRegistry,
        IServerConnectionService connectionService,
        CharServerGrpcHandler charServerGrpcHandler,
        LoginServerConfiguration loginConfig,
        ILoginDataRepository loginDataRepository,
        ILoginRepository loginRepository,
        GameDbContext dbContext)
    {
        _logger = logger;
        _charServerRegistry = charServerRegistry;
        _connectionService = connectionService;
        _charServerGrpcHandler = charServerGrpcHandler;
        _loginConfig = loginConfig;
        _loginDataRepository = loginDataRepository;
        _loginRepository = loginRepository;
        _dbContext = dbContext;
    }

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

    public override async Task<AccountInfoResponse> GetAccountInfo(
        AccountInfoRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountInfoResponse
            {
                Success = false,
                AccountId = request.AccountId
            };
        }

        return new AccountInfoResponse
        {
            Success = true,
            AccountId = account.AccountId,
            Username = account.UserId,
            Email = account.Email,
            CreatedAt = account.LastLogin.HasValue
                ? new DateTimeOffset(account.LastLogin.Value).ToUnixTimeSeconds()
                : 0,
            GroupId = account.GroupId,
            LoginCount = account.LoginCount,
            State = account.State,
            LastIp = account.LastIp,
            LastLogin = account.LastLogin.HasValue
                ? account.LastLogin.Value.ToString(_loginConfig.DateFormat, CultureInfo.InvariantCulture)
                : string.Empty,
            Birthdate = account.Birthdate.HasValue
                ? account.Birthdate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty
        };
    }

    public override async Task<AccountDataResponse> GetFullAccountData(
        AccountDataRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountDataResponse
            {
                Success = false,
                AccountId = request.AccountId
            };
        }

        var baseSlots = account.CharacterSlots > 0
            ? account.CharacterSlots
            : (uint)Math.Max(_loginConfig.CharactersPerAccount, 0);
        var vipSlotsIncrease = _loginConfig.Vip?.CharacterSlotIncrease ?? 0;
        var isVip = account.VipTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var finalSlots = isVip ? baseSlots + vipSlotsIncrease : baseSlots;

        return new AccountDataResponse
        {
            Success = true,
            AccountId = account.AccountId,
            Email = account.Email,
            ExpirationTime = account.ExpirationTime,
            GroupId = account.GroupId,
            CharSlots = finalSlots,
            Birthdate = account.Birthdate.HasValue
                ? account.Birthdate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty,
            Pincode = account.Pincode,
            PincodeChange = account.PincodeChange,
            IsVip = isVip,
            CharVip = vipSlotsIncrease,
            MaxBilling = 3
        };
    }

    public override async Task<CharacterServerRegistrationResponse> RegisterCharacterServer(
        CharacterServerRegistrationRequest request,
        ServerCallContext context)
    {
        return await _charServerGrpcHandler.RegisterCharacterServerAsync(request, context);
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

        if (_loginDataRepository.TryConsumeAuthNode(
                request.AccountId,
                request.LoginId1,
                request.LoginId2,
                expectedSex,
                out var authNode) && authNode != null)
        {
            _loginDataRepository.SetOnlineUserCharServer(request.AccountId, (int)request.CharServerId);

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
        _charServerRegistry.UpdateCharServerUserCount(request.ServerId, (ushort)request.UserCount);
        return Task.FromResult(new CharacterServerUserCountUpdateResponse { Success = true });
    }

    public override Task<CharacterServerAddressUpdateResponse> UpdateCharacterServerAddress(
        CharacterServerAddressUpdateRequest request,
        ServerCallContext context)
    {
        _charServerRegistry.UpdateCharServerAddress(request.ServerId, request.Ip);
        return Task.FromResult(new CharacterServerAddressUpdateResponse { Success = true });
    }

    public override async Task<CharacterServerSetAllOfflineResponse> SetAllOfflineForCharacterServer(
        CharacterServerSetAllOfflineRequest request,
        ServerCallContext context)
    {
        var removed = await _loginDataRepository.RemoveOnlineUsersByCharServer(request.ServerId);
        return new CharacterServerSetAllOfflineResponse
        {
            Success = true,
            RemovedAccounts = (uint)Math.Max(removed, 0)
        };
    }

    public override async Task<AccountPincodeUpdateResponse> UpdateAccountPincode(
        AccountPincodeUpdateRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountPincodeUpdateResponse
            {
                Success = false,
                ErrorMessage = "Account not found"
            };
        }

        account.Pincode = request.Pincode ?? string.Empty;
        account.PincodeChange = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _loginRepository.UpdateAsync(account, context.CancellationToken);

        return new AccountPincodeUpdateResponse { Success = true };
    }

    public override async Task<AccountPincodeAuthFailResponse> NotifyPincodeAuthFail(
        AccountPincodeAuthFailRequest request,
        ServerCallContext context)
    {
        await _loginDataRepository.RemoveOnlineUser((int)request.AccountId);
        _logger.LogInformation("PIN Code check failed for account {AccountId}", request.AccountId);
        return new AccountPincodeAuthFailResponse { Success = true };
    }

    public override async Task<CharacterServerOnlineSyncResponse> SyncOnlineAccounts(
        CharacterServerOnlineSyncRequest request,
        ServerCallContext context)
    {
        await _loginDataRepository.RemoveOnlineUsersByCharServer(request.ServerId);

        var uniqueAccounts = request.AccountIds
            .Where(accountId => accountId > 0)
            .Distinct()
            .ToList();

        foreach (var accountId in uniqueAccounts)
        {
            _loginDataRepository.SetOnlineUserCharServer(accountId, request.ServerId);
        }

        return new CharacterServerOnlineSyncResponse
        {
            Success = true,
            SyncedAccounts = (uint)uniqueAccounts.Count
        };
    }

    public override async Task<GlobalAccRegUpdateResponse> UpdateGlobalAccountRegisters(
        GlobalAccRegUpdateRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0)
        {
            return new GlobalAccRegUpdateResponse
            {
                Success = false,
                ErrorMessage = "Invalid account id"
            };
        }

        var entries = request.Entries.ToList();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                continue;
            }

            if (entry.IsNumeric)
            {
                var existing = await _dbContext.GlobalAccountRegistersNum
                    .FirstOrDefaultAsync(
                        e => e.AccountId == (int)request.AccountId && e.Key == entry.Key && e.Index == entry.Index,
                        context.CancellationToken);

                if (existing == null)
                {
                    _dbContext.GlobalAccountRegistersNum.Add(new GlobalAccRegNumEntity
                    {
                        AccountId = (int)request.AccountId,
                        Key = entry.Key,
                        Index = entry.Index,
                        Value = entry.NumValue
                    });
                }
                else
                {
                    existing.Value = entry.NumValue;
                }
            }
            else
            {
                var existing = await _dbContext.GlobalAccountRegistersStr
                    .FirstOrDefaultAsync(
                        e => e.AccountId == (int)request.AccountId && e.Key == entry.Key && e.Index == entry.Index,
                        context.CancellationToken);

                if (existing == null)
                {
                    _dbContext.GlobalAccountRegistersStr.Add(new GlobalAccRegStrEntity
                    {
                        AccountId = (int)request.AccountId,
                        Key = entry.Key,
                        Index = entry.Index,
                        Value = entry.StrValue ?? string.Empty
                    });
                }
                else
                {
                    existing.Value = entry.StrValue ?? string.Empty;
                }
            }
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new GlobalAccRegUpdateResponse
        {
            Success = true,
            UpdatedEntries = (uint)entries.Count
        };
    }

    public override async Task<GlobalAccRegFetchResponse> GetGlobalAccountRegisters(
        GlobalAccRegFetchRequest request,
        ServerCallContext context)
    {
        if (request.AccountId <= 0)
        {
            return new GlobalAccRegFetchResponse
            {
                Success = false,
                AccountId = request.AccountId,
                CharId = request.CharId,
                ErrorMessage = "Invalid account id"
            };
        }

        var response = new GlobalAccRegFetchResponse
        {
            Success = true,
            AccountId = request.AccountId,
            CharId = request.CharId
        };

        var numericEntries = await _dbContext.GlobalAccountRegistersNum
            .Where(e => e.AccountId == (int)request.AccountId)
            .ToListAsync(context.CancellationToken);

        foreach (var entry in numericEntries)
        {
            response.Entries.Add(new GlobalAccRegEntry
            {
                Key = entry.Key,
                Index = entry.Index,
                IsNumeric = true,
                NumValue = entry.Value,
                StrValue = string.Empty
            });
        }

        var stringEntries = await _dbContext.GlobalAccountRegistersStr
            .Where(e => e.AccountId == (int)request.AccountId)
            .ToListAsync(context.CancellationToken);

        foreach (var entry in stringEntries)
        {
            response.Entries.Add(new GlobalAccRegEntry
            {
                Key = entry.Key,
                Index = entry.Index,
                IsNumeric = false,
                NumValue = 0,
                StrValue = entry.Value
            });
        }

        return response;
    }

    public override async Task<AccountVipDataResponse> RequestVipData(
        AccountVipDataRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountVipDataResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Account not found"
            };
        }

        var vipConfig = _loginConfig.Vip ?? new VipConfiguration();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var vipTime = (long)account.VipTime;

        if ((request.Flags & 0x2) != 0 && request.DurationSeconds != 0)
        {
            if (vipTime == 0)
            {
                vipTime = now;
            }

            vipTime += request.DurationSeconds;
            if (vipTime < 0)
            {
                vipTime = 0;
            }
        }

        var isVip = vipTime > now;
        var previousGroup = account.GroupId;
        if (isVip)
        {
            if (account.GroupId != vipConfig.GroupId)
            {
                account.OldGroup = account.GroupId;
            }

            account.GroupId = (byte)Math.Clamp((int)vipConfig.GroupId, byte.MinValue, byte.MaxValue);
        }
        else
        {
            if (account.GroupId == (byte)Math.Clamp((int)vipConfig.GroupId, byte.MinValue, byte.MaxValue))
            {
                account.GroupId = account.OldGroup;
            }

            account.OldGroup = 0;
            vipTime = 0;
        }

        account.VipTime = (uint)Math.Max(vipTime, 0);

        var baseSlots = account.CharacterSlots > 0
            ? account.CharacterSlots
            : (uint)Math.Max(_loginConfig.CharactersPerAccount, 0);
        var charVip = vipConfig.CharacterSlotIncrease;
        var charSlots = isVip ? baseSlots + charVip : baseSlots;

        await _loginRepository.UpdateAsync(account, context.CancellationToken);
        await BroadcastVipDataAsync(
            (int)request.AccountId,
            vipTime,
            request.Flags,
            account.GroupId,
            request.MapServerId,
            isVip,
            charSlots,
            charVip,
            account.OldGroup);

        return new AccountVipDataResponse
        {
            Success = true,
            AccountId = request.AccountId,
            VipTime = vipTime,
            Flags = request.Flags,
            GroupId = account.GroupId,
            MapServerId = request.MapServerId,
            IsVip = isVip,
            CharSlots = charSlots,
            CharVip = charVip,
            OldGroup = account.OldGroup
        };
    }

    public override async Task<AccountStatusUpdateResponse> NotifyAccountStatus(
        AccountStatusUpdateRequest request,
        ServerCallContext context)
    {
        if (request.Online)
        {
            _loginDataRepository.SetOnlineUserCharServer(request.AccountId, request.CharServerId);
        }
        else
        {
            await _loginDataRepository.RemoveOnlineUser(request.AccountId);
            _loginDataRepository.RemoveAuthNode(request.AccountId);
        }

        return new AccountStatusUpdateResponse { Success = true };
    }

    public override Task<CharacterServerListResponse> ListCharacterServers(
        CharacterServerListRequest request,
        ServerCallContext context)
    {
        var response = new CharacterServerListResponse();
        foreach (var (serverId, server) in _charServerRegistry.GetActiveCharServersWithIds())
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

    public override async Task<AccountStateUpdateResponse> UpdateAccountState(
        AccountStateUpdateRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountStateUpdateResponse
            {
                Success = false,
                State = request.State,
                ErrorMessage = "Account not found"
            };
        }

        account.State = request.State;
        await _loginRepository.UpdateAsync(account, context.CancellationToken);
        await BroadcastAccountStatusUpdateAsync((int)request.AccountId, isBan: false, request.State);

        return new AccountStateUpdateResponse
        {
            Success = true,
            State = request.State
        };
    }

    public override async Task<AccountBanResponse> BanAccount(
        AccountBanRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountBanResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Account not found"
            };
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var currentUnban = (long)account.UnbanTime;
        var baseTime = currentUnban > now ? currentUnban : now;
        var newUnban = baseTime + request.DurationSeconds;
        if (newUnban <= now)
        {
            return new AccountBanResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Invalid ban duration"
            };
        }

        account.UnbanTime = (uint)newUnban;
        await _loginRepository.UpdateAsync(account, context.CancellationToken);
        await BroadcastAccountStatusUpdateAsync((int)request.AccountId, isBan: true, (uint)newUnban);

        return new AccountBanResponse
        {
            Success = true,
            AccountId = request.AccountId,
            UnbanTime = newUnban
        };
    }

    public override async Task<AccountUnbanResponse> UnbanAccount(
        AccountUnbanRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountUnbanResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Account not found"
            };
        }

        account.UnbanTime = 0;
        await _loginRepository.UpdateAsync(account, context.CancellationToken);
        await BroadcastAccountStatusUpdateAsync((int)request.AccountId, isBan: true, 0);

        return new AccountUnbanResponse
        {
            Success = true,
            AccountId = request.AccountId
        };
    }

    public override async Task<AccountEmailChangeResponse> ChangeAccountEmail(
        AccountEmailChangeRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountEmailChangeResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Account not found"
            };
        }

        if (!IsValidEmail(request.CurrentEmail))
        {
            return new AccountEmailChangeResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Invalid current email"
            };
        }

        if (!IsValidEmail(request.NewEmail) ||
            string.Equals(request.NewEmail, "a@a.com", StringComparison.OrdinalIgnoreCase))
        {
            return new AccountEmailChangeResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Invalid new email"
            };
        }

        if (!string.Equals(account.Email, request.CurrentEmail, StringComparison.OrdinalIgnoreCase))
        {
            return new AccountEmailChangeResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Current email mismatch"
            };
        }

        account.Email = request.NewEmail;
        await _loginRepository.UpdateAsync(account, context.CancellationToken);

        return new AccountEmailChangeResponse
        {
            Success = true,
            AccountId = account.AccountId,
            Email = account.Email
        };
    }

    public override async Task<AccountSexChangeResponse> ChangeAccountSex(
        AccountSexChangeRequest request,
        ServerCallContext context)
    {
        var account = await _loginRepository.GetByIdAsync((int)request.AccountId, context.CancellationToken);
        if (account == null)
        {
            return new AccountSexChangeResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Account not found"
            };
        }

        if (account.Sex == 'S')
        {
            return new AccountSexChangeResponse
            {
                Success = false,
                AccountId = request.AccountId,
                ErrorMessage = "Server account sex cannot be changed"
            };
        }

        account.Sex = account.Sex == 'M' ? 'F' : 'M';
        await _loginRepository.UpdateAsync(account, context.CancellationToken);
        var sexCode = account.Sex switch
        {
            'F' => 0u,
            'M' => 1u,
            'S' => 2u,
            _ => 0u
        };
        await BroadcastAccountSexUpdateAsync((int)request.AccountId, sexCode);

        return new AccountSexChangeResponse
        {
            Success = true,
            AccountId = account.AccountId,
            Sex = sexCode
        };
    }

    public static void StoreSession(string token, long accountId, string username)
    {
        Sessions[token] = (accountId, username);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        var atIndex = trimmed.IndexOf('@');
        return atIndex > 0 && atIndex < trimmed.Length - 1;
    }

    private async Task BroadcastAccountStatusUpdateAsync(int accountId, bool isBan, uint value)
    {
        var charSessions = _connectionService.GetSessionsByType(ServerType.Char).ToList();
        foreach (var charSession in charSessions)
        {
            try
            {
                var client = new CharacterService.CharacterServiceClient(charSession.Channel);
                await client.BroadcastAccountStatusUpdateAsync(new AccountStatusBroadcastRequest
                {
                    AccountId = accountId,
                    IsBan = isBan,
                    Value = value
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to broadcast account status update for account {AccountId} to char server {ServerName}",
                    accountId,
                    charSession.ServerName);
            }
        }
    }

    private async Task BroadcastAccountSexUpdateAsync(int accountId, uint sex)
    {
        var charSessions = _connectionService.GetSessionsByType(ServerType.Char).ToList();
        foreach (var charSession in charSessions)
        {
            try
            {
                var client = new CharacterService.CharacterServiceClient(charSession.Channel);
                await client.BroadcastAccountSexUpdateAsync(new AccountSexBroadcastRequest
                {
                    AccountId = accountId,
                    Sex = sex
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to broadcast account sex update for account {AccountId} to char server {ServerName}",
                    accountId,
                    charSession.ServerName);
            }
        }
    }

    private async Task BroadcastVipDataAsync(
        int accountId,
        long vipTime,
        uint flags,
        uint groupId,
        int mapServerId,
        bool isVip,
        uint charSlots,
        uint charVip,
        uint oldGroup)
    {
        var charSessions = _connectionService.GetSessionsByType(ServerType.Char).ToList();
        foreach (var charSession in charSessions)
        {
            try
            {
                var client = new CharacterService.CharacterServiceClient(charSession.Channel);
                await client.PushVipDataAsync(new AccountVipPushRequest
                {
                    AccountId = accountId,
                    VipTime = vipTime,
                    Flags = flags,
                    GroupId = groupId,
                    MapServerId = mapServerId,
                    IsVip = isVip,
                    CharSlots = charSlots,
                    CharVip = charVip,
                    OldGroup = oldGroup
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to broadcast VIP data for account {AccountId} to char server {ServerName}",
                    accountId,
                    charSession.ServerName);
            }
        }
    }
}
