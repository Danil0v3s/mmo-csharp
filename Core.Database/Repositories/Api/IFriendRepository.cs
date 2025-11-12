using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IFriendRepository
{
    Task<IReadOnlyList<FriendEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<FriendEntity> AddAsync(FriendEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int charId, int friendId, CancellationToken ct = default);
    Task<bool> AreFriendsAsync(int charId, int friendId, CancellationToken ct = default);
}

