using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<ElementalCreateResponse?> ElementalCreateAsync(
        ElementalData elemental,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.ElementalCreateAsync(new ElementalCreateRequest
        {
            Elemental = elemental ?? new ElementalData()
        }, cancellationToken: cancellationToken);
    }

    public async Task<ElementalLoadResponse?> ElementalLoadAsync(
        int elementalId,
        int characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.ElementalLoadAsync(new ElementalLoadRequest
        {
            ElementalId = elementalId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<ElementalSaveResponse?> ElementalSaveAsync(
        ElementalData elemental,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.ElementalSaveAsync(new ElementalSaveRequest
        {
            Elemental = elemental ?? new ElementalData()
        }, cancellationToken: cancellationToken);
    }

    public async Task<ElementalDeleteResponse?> ElementalDeleteAsync(
        int elementalId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.ElementalDeleteAsync(new ElementalDeleteRequest
        {
            ElementalId = elementalId
        }, cancellationToken: cancellationToken);
    }
}
