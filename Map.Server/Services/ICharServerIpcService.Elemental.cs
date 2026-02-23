using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceElemental
{
    Task<ElementalCreateResponse?> ElementalCreateAsync(
        ElementalData elemental,
        CancellationToken cancellationToken = default);

    Task<ElementalLoadResponse?> ElementalLoadAsync(
        int elementalId,
        int characterId,
        CancellationToken cancellationToken = default);

    Task<ElementalSaveResponse?> ElementalSaveAsync(
        ElementalData elemental,
        CancellationToken cancellationToken = default);

    Task<ElementalDeleteResponse?> ElementalDeleteAsync(
        int elementalId,
        CancellationToken cancellationToken = default);
}
