using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<PetCreateResponse?> PetCreateAsync(
        int accountId,
        int characterId,
        int classId,
        int level,
        int eggItemId,
        int equipItemId,
        int intimacy,
        int hungry,
        int renameFlag,
        bool incubate,
        string name,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.PetCreateAsync(new PetCreateRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            ClassId = classId,
            Level = level,
            EggItemId = eggItemId,
            EquipItemId = equipItemId,
            Intimacy = intimacy,
            Hungry = hungry,
            RenameFlag = renameFlag,
            Incubate = incubate,
            Name = name ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<PetLoadResponse?> PetLoadAsync(
        int accountId,
        int characterId,
        int petId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.PetLoadAsync(new PetLoadRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            PetId = petId
        }, cancellationToken: cancellationToken);
    }

    public async Task<PetSaveResponse?> PetSaveAsync(
        int accountId,
        PetData pet,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.PetSaveAsync(new PetSaveRequest
        {
            AccountId = accountId,
            Pet = pet ?? new PetData()
        }, cancellationToken: cancellationToken);
    }

    public async Task<PetDeleteResponse?> PetDeleteAsync(
        int petId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.PetDeleteAsync(new PetDeleteRequest
        {
            PetId = petId
        }, cancellationToken: cancellationToken);
    }
}
