using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceHomunculus
{
    Task<HomunculusCreateResponse?> HomunculusCreateAsync(
        int accountId,
        HomunculusData homunculus,
        CancellationToken cancellationToken = default);

    Task<HomunculusLoadResponse?> HomunculusLoadAsync(
        int accountId,
        int homunculusId,
        CancellationToken cancellationToken = default);

    Task<HomunculusSaveResponse?> HomunculusSaveAsync(
        int accountId,
        HomunculusData homunculus,
        CancellationToken cancellationToken = default);

    Task<HomunculusDeleteResponse?> HomunculusDeleteAsync(
        int homunculusId,
        CancellationToken cancellationToken = default);

    Task<HomunculusRenameResponse?> HomunculusRenameAsync(
        int accountId,
        int characterId,
        string name,
        CancellationToken cancellationToken = default);
}
