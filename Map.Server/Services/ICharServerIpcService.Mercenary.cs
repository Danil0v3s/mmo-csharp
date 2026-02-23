using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceMercenary
{
    Task<MercenaryCreateResponse?> MercenaryCreateAsync(
        MercenaryData mercenary,
        CancellationToken cancellationToken = default);

    Task<MercenaryLoadResponse?> MercenaryLoadAsync(
        int mercenaryId,
        int characterId,
        CancellationToken cancellationToken = default);

    Task<MercenarySaveResponse?> MercenarySaveAsync(
        MercenaryData mercenary,
        CancellationToken cancellationToken = default);

    Task<MercenaryDeleteResponse?> MercenaryDeleteAsync(
        int mercenaryId,
        CancellationToken cancellationToken = default);
}
