using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<MercenaryCreateResponse?> MercenaryCreateAsync(
        MercenaryData mercenary,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.MercenaryCreateAsync(new MercenaryCreateRequest
        {
            Mercenary = mercenary ?? new MercenaryData()
        }, cancellationToken: cancellationToken);
    }

    public async Task<MercenaryLoadResponse?> MercenaryLoadAsync(
        int mercenaryId,
        int characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.MercenaryLoadAsync(new MercenaryLoadRequest
        {
            MercenaryId = mercenaryId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<MercenarySaveResponse?> MercenarySaveAsync(
        MercenaryData mercenary,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.MercenarySaveAsync(new MercenarySaveRequest
        {
            Mercenary = mercenary ?? new MercenaryData()
        }, cancellationToken: cancellationToken);
    }

    public async Task<MercenaryDeleteResponse?> MercenaryDeleteAsync(
        int mercenaryId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.MercenaryDeleteAsync(new MercenaryDeleteRequest
        {
            MercenaryId = mercenaryId
        }, cancellationToken: cancellationToken);
    }
}
