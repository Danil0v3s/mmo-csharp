using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IGuildMemberRepository
{
    Task<IReadOnlyList<GuildMemberEntity>> GetByGuildIdAsync(int guildId, CancellationToken ct = default);
    Task<GuildMemberEntity?> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<GuildMemberEntity> AddAsync(GuildMemberEntity entity, CancellationToken ct = default);
    Task UpdateAsync(GuildMemberEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int guildId, int charId, CancellationToken ct = default);
}

