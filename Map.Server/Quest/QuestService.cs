using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Quest;

/// <summary>
/// Default <see cref="IQuestService"/>. Catalog loaded from the
/// <c>quest_db</c> SQL table (seeded by Tools.RathenaImporter from
/// <c>db/re/quest_db.yml</c>, ~4,800 quests). Per-character active
/// quest log lives on the persistent quest table accessed via IPC;
/// this service owns the *catalog* lookup + the objective-update
/// pipeline.
/// </summary>
public sealed class QuestService : IQuestService
{
    private readonly Dictionary<uint, QuestDbEntity> _catalog = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<QuestService> _logger;

    public QuestService(IServiceScopeFactory scopes, ILogger<QuestService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    public QuestService(ILogger<QuestService> logger) { _logger = logger; }

    public int Add(PlayerEntity pc, int questId) => 0;
    public int Change(PlayerEntity pc, int oldQuestId, int newQuestId) => 0;
    public int Check(PlayerEntity pc, int questId, byte status) => 0;
    public int Delete(PlayerEntity pc, int questId) => 0;
    public int PcLogin(PlayerEntity pc) => 0;
    public int UpdateObjectiveSub(PlayerEntity pc, int questId, byte index, int delta) => 0;
    public void UpdateObjective(PlayerEntity pc, int questId, byte index, int delta) { }
    public int UpdateStatus(PlayerEntity pc, int questId, byte status) => 0;

    public void Reload()
    {
        _catalog.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IQuestDbRepository>();
            foreach (var q in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[q.QuestId] = q;
            _logger.LogInformation("quest_db loaded: {N} quests", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "quest_db load failed");
        }
    }

    /// <summary>Catalog lookup — null if unknown id.</summary>
    public QuestDbEntity? GetCatalogEntry(uint questId)
        => _catalog.TryGetValue(questId, out var v) ? v : null;

    /// <inheritdoc />
    public IReadOnlyList<Core.Server.IPC.QuestEntryData> SnapshotFor(PlayerEntity pc)
    {
        // Mirrors rAthena `intif_quest_save` payload shape: one entry
        // per active quest with the per-objective counters + the
        // unix-time + state field.
        var log = pc.QuestLog;
        if (log.Count == 0) return Array.Empty<Core.Server.IPC.QuestEntryData>();
        var snapshot = new Core.Server.IPC.QuestEntryData[log.Count];
        for (int i = 0; i < log.Count; i++)
        {
            var q = log[i];
            var entry = new Core.Server.IPC.QuestEntryData
            {
                QuestId = q.QuestId,
                TimeUnix = q.TimeUnix,
                State = q.State,
            };
            if (q.Counts != null)
                foreach (var c in q.Counts)
                    entry.Counts.Add(c);
            snapshot[i] = entry;
        }
        return snapshot;
    }

    /// <inheritdoc />
    public void Hydrate(PlayerEntity pc, IEnumerable<Core.Server.IPC.QuestEntryData> entries)
    {
        pc.QuestLog.Clear();
        if (entries == null) return;
        foreach (var e in entries)
        {
            pc.QuestLog.Add(new QuestEntry
            {
                QuestId = e.QuestId,
                TimeUnix = e.TimeUnix,
                State = e.State,
                Counts = e.Counts?.ToArray() ?? Array.Empty<int>(),
            });
        }
    }
}
