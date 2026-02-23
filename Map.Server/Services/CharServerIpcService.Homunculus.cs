using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<HomunculusCreateResponse?> HomunculusCreateAsync(
        int accountId,
        HomunculusData homunculus,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.HomunculusCreateAsync(new HomunculusCreateRequest
        {
            AccountId = accountId,
            Homunculus = homunculus ?? new HomunculusData()
        }, cancellationToken: cancellationToken);
    }

    public async Task<HomunculusLoadResponse?> HomunculusLoadAsync(
        int accountId,
        int homunculusId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.HomunculusLoadAsync(new HomunculusLoadRequest
        {
            AccountId = accountId,
            HomunculusId = homunculusId
        }, cancellationToken: cancellationToken);
    }

    public async Task<HomunculusSaveResponse?> HomunculusSaveAsync(
        int accountId,
        HomunculusData homunculus,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.HomunculusSaveAsync(new HomunculusSaveRequest
        {
            AccountId = accountId,
            Homunculus = homunculus ?? new HomunculusData()
        }, cancellationToken: cancellationToken);
    }

    public async Task<HomunculusDeleteResponse?> HomunculusDeleteAsync(
        int homunculusId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.HomunculusDeleteAsync(new HomunculusDeleteRequest
        {
            HomunculusId = homunculusId
        }, cancellationToken: cancellationToken);
    }

    public async Task<HomunculusRenameResponse?> HomunculusRenameAsync(
        int accountId,
        int characterId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.HomunculusRenameAsync(new HomunculusRenameRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            Name = name ?? string.Empty
        }, cancellationToken: cancellationToken);
    }
}
