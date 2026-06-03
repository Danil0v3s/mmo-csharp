using Map.Server.Entities;
using Map.Server.Services.Intif;
using Microsoft.Extensions.DependencyInjection;

namespace Map.Server.Quest;

/// <summary>
/// FEATURE-22 — default <see cref="IQuestSaveTrigger"/>. Resolves <see cref="IIntifService"/> lazily
/// from the provider so the QuestService → save → intif edge doesn't form a constructor cycle, then
/// fires the fire-and-forget autosave wrapper (<c>QuestSave</c>), which snapshots the quest log and
/// dispatches the char-server <c>QuestSave</c> RPC.
/// </summary>
public sealed class QuestSaveTrigger(IServiceProvider provider) : IQuestSaveTrigger
{
    public void Save(PlayerEntity pc)
        => provider.GetRequiredService<IIntifService>().QuestSave(pc);
}
