using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Quest;

/// <summary>Default <see cref="IQuestService"/>. Entry shells; persistence + quest_db YAML data-pending.</summary>
public sealed class QuestService : IQuestService
{
    private readonly ILogger<QuestService> _logger;
    public QuestService(ILogger<QuestService> logger) => _logger = logger;

    public int Add(PlayerEntity pc, int questId) => 0;
    public int Change(PlayerEntity pc, int oldQuestId, int newQuestId) => 0;
    public int Check(PlayerEntity pc, int questId, byte status) => 0;
    public int Delete(PlayerEntity pc, int questId) => 0;
    public int PcLogin(PlayerEntity pc) => 0;
    public int UpdateObjectiveSub(PlayerEntity pc, int questId, byte index, int delta) => 0;
    public void UpdateObjective(PlayerEntity pc, int questId, byte index, int delta) { }
    public int UpdateStatus(PlayerEntity pc, int questId, byte status) => 0;
    public void Reload() { }
}
