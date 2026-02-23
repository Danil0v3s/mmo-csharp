using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServicePet
{
    Task<PetCreateResponse?> PetCreateAsync(
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
        CancellationToken cancellationToken = default);

    Task<PetLoadResponse?> PetLoadAsync(
        int accountId,
        int characterId,
        int petId,
        CancellationToken cancellationToken = default);

    Task<PetSaveResponse?> PetSaveAsync(
        int accountId,
        PetData pet,
        CancellationToken cancellationToken = default);

    Task<PetDeleteResponse?> PetDeleteAsync(
        int petId,
        CancellationToken cancellationToken = default);
}
