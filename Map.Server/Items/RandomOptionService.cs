using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Items;

/// <summary>
/// Random-option runtime service. Port of rAthena
/// <c>RandomOptionDatabase</c> + <c>RandomOptionGroupDatabase</c> +
/// <c>s_random_opt_group::apply</c> in <c>itemdb.cpp:4458-4720</c>.
///
/// <para>Caches two catalogs at boot:</para>
/// <list type="bullet">
///   <item><c>item_randomopt_db</c> (249 named options, each with an
///   id + script) — used to resolve option names ↔ ids, and to look up
///   the script the bonus applies when the option fires on an equip.</item>
///   <item><c>item_randomopt_group_db</c> + <c>item_randomopt_group_option_db</c>
///   (106 groups, each mapping slot → list of candidate options with
///   min/max value + chance). The <see cref="Apply"/> roll mirrors
///   <c>s_random_opt_group::apply</c> — for each slot, try one of the
///   slot's options weighted by <c>chance/10000</c>; if no slot option
///   fires, force-pick any slot option without the chance check (so the
///   slot is never empty).</item>
/// </list>
///
/// <para>The seeded SQL ships from <c>seed_item_randomopt_db.sql</c> +
/// <c>seed_item_randomopt_group.sql</c>. The interface stubs on
/// <see cref="Map.Server.Items.Db.IItemDbService"/> delegate here so
/// existing callers keep their entry point.</para>
/// </summary>
public interface IRandomOptionService
{
    /// <summary>Number of option ids in the cached catalog.</summary>
    int OptionCount { get; }
    /// <summary>Number of groups in the cached catalog.</summary>
    int GroupCount { get; }

    /// <summary>rAthena <c>RandomOptionDatabase::option_exists</c>.</summary>
    bool OptionExists(int optionId);

    /// <summary>rAthena <c>RandomOptionDatabase::option_get_id</c> — resolve <c>VAR_*</c> name to id.</summary>
    int OptionGetId(string optionName);

    /// <summary>Lookup full option row by id (for the script body).</summary>
    ItemRandomOptDbEntity? GetOption(int optionId);

    /// <summary>True if the group id is known.</summary>
    bool GroupExists(int groupId);

    /// <summary>rAthena <c>RandomOptionGroupDatabase::option_exists</c> — group lookup by name.</summary>
    bool GroupExistsByName(string groupName);

    /// <summary>rAthena <c>RandomOptionGroupDatabase::option_get_id</c>.</summary>
    int GroupGetId(string groupName);

    /// <summary>
    /// rAthena <c>s_random_opt_group::apply</c>. Rolls each slot
    /// according to its options' chance and writes the picked
    /// (optionId, value, param) into <paramref name="output"/>.
    /// Up to 5 entries (MAX_ITEM_RDM_OPT). Empty-output slots are
    /// compacted to the front so the client doesn't see holes.
    /// </summary>
    void Apply(int groupId, IList<(int id, int value, int param)> output);

    /// <summary>Rebuild the in-memory cache from the database.</summary>
    void Reload();
}

/// <summary>Default <see cref="IRandomOptionService"/>.</summary>
public sealed class RandomOptionService : IRandomOptionService
{
    /// <summary>rAthena <c>MAX_ITEM_RDM_OPT</c> in <c>mmo.hpp</c>.</summary>
    public const int MaxItemRandomOpt = 5;

    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<RandomOptionService> _logger;
    private readonly Random _rng = new();

    // option_id → row
    private Dictionary<int, ItemRandomOptDbEntity> _optionsById = new();
    // option_name → id (case-insensitive)
    private Dictionary<string, int> _optionsByName = new(StringComparer.OrdinalIgnoreCase);

    // group_id → group_name
    private Dictionary<int, string> _groupNameById = new();
    // group_name → id (case-insensitive)
    private Dictionary<string, int> _groupIdByName = new(StringComparer.OrdinalIgnoreCase);
    // group_id → slot → list of options
    private Dictionary<int, Dictionary<int, List<GroupSlotOption>>> _groupSlots = new();

    public RandomOptionService(IServiceScopeFactory scopes, ILogger<RandomOptionService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    /// <summary>Test ctor — empty catalogs.</summary>
    public RandomOptionService(ILogger<RandomOptionService> logger)
    {
        _logger = logger;
    }

    public int OptionCount => _optionsById.Count;
    public int GroupCount => _groupSlots.Count;

    public bool OptionExists(int optionId) => _optionsById.ContainsKey(optionId);

    public int OptionGetId(string optionName)
        => !string.IsNullOrEmpty(optionName) && _optionsByName.TryGetValue(optionName, out var id) ? id : 0;

    public ItemRandomOptDbEntity? GetOption(int optionId)
        => _optionsById.TryGetValue(optionId, out var row) ? row : null;

    public bool GroupExists(int groupId) => _groupSlots.ContainsKey(groupId);

    public bool GroupExistsByName(string groupName)
        => !string.IsNullOrEmpty(groupName) && _groupIdByName.ContainsKey(groupName);

    public int GroupGetId(string groupName)
        => !string.IsNullOrEmpty(groupName) && _groupIdByName.TryGetValue(groupName, out var id) ? id : 0;

    public void Apply(int groupId, IList<(int id, int value, int param)> output)
    {
        if (output == null) return;
        if (!_groupSlots.TryGetValue(groupId, out var slots)) return;

        // Working buffer (MAX_ITEM_RDM_OPT). Mirrors the C++ loop which
        // clears item.option[0..MAX] first then writes per-slot.
        var picks = new (int id, int value, int param)[MaxItemRandomOpt];

        foreach (var (slotIdx, candidates) in slots)
        {
            if (slotIdx < 0 || slotIdx >= MaxItemRandomOpt) continue;
            if (candidates.Count == 0) continue;

            // rAthena: try N*3 random picks against each option's chance.
            // If one fires, take it. Otherwise force-pick a random one.
            var maxAttempts = candidates.Count * 3;
            (int id, int value, int param) picked = default;
            bool got = false;
            for (int j = 0; j < maxAttempts; j++)
            {
                var c = candidates[_rng.Next(candidates.Count)];
                // chance is /10000; rnd_chance<uint16>(c.Chance, 10000).
                if (c.Chance <= 0) continue;
                if (_rng.Next(10000) < c.Chance)
                {
                    picked = (c.OptionId, RollValue(c.MinValue, c.MaxValue), 0);
                    got = true;
                    break;
                }
            }
            if (!got)
            {
                var c = candidates[_rng.Next(candidates.Count)];
                picked = (c.OptionId, RollValue(c.MinValue, c.MaxValue), 0);
            }
            picks[slotIdx] = picked;
        }

        // Compact (rAthena "fix gaps" pass — the client can't handle
        // a zero-id followed by a non-zero-id).
        int write = 0;
        for (int i = 0; i < MaxItemRandomOpt; i++)
        {
            if (picks[i].id != 0)
            {
                if (write != i) picks[write] = picks[i];
                write++;
            }
        }
        for (int i = write; i < MaxItemRandomOpt; i++)
            picks[i] = default;

        for (int i = 0; i < MaxItemRandomOpt; i++)
            output.Add(picks[i]);
    }

    private int RollValue(int min, int max)
    {
        if (min == max) return min;
        if (min > max) (min, max) = (max, min);
        return _rng.Next(min, max + 1);
    }

    public void Reload()
    {
        if (_scopes == null)
        {
            _optionsById = new();
            _optionsByName = new(StringComparer.OrdinalIgnoreCase);
            _groupNameById = new();
            _groupIdByName = new(StringComparer.OrdinalIgnoreCase);
            _groupSlots = new();
            return;
        }
        try
        {
            using var scope = _scopes.CreateScope();
            var optRepo = scope.ServiceProvider.GetRequiredService<IItemRandomOptDbRepository>();
            var grpRepo = scope.ServiceProvider.GetRequiredService<IItemRandomOptGroupDbRepository>();

            // --- options table (id → script row) ---
            var optionRows = optRepo.GetAllAsync().GetAwaiter().GetResult();
            var byId = new Dictionary<int, ItemRandomOptDbEntity>(optionRows.Count);
            var byName = new Dictionary<string, int>(optionRows.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var r in optionRows)
            {
                byId[r.OptionId] = r;
                if (!string.IsNullOrEmpty(r.OptionName))
                    byName[r.OptionName] = r.OptionId;
            }

            // --- groups + slot options ---
            var groups = grpRepo.GetAllAsync().GetAwaiter().GetResult();
            var allOpts = grpRepo.GetAllOptionsAsync().GetAwaiter().GetResult();

            var groupNameById = new Dictionary<int, string>(groups.Count);
            var groupIdByName = new Dictionary<string, int>(groups.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var g in groups)
            {
                groupNameById[g.Id] = g.GroupName;
                groupIdByName[g.GroupName] = g.Id;
            }

            // group → slot → list of options. Resolve option_name → id
            // upfront so the runtime apply doesn't need a name lookup
            // per roll.
            var slotIndex = new Dictionary<int, Dictionary<int, List<GroupSlotOption>>>();
            int slotRows = 0;
            foreach (var row in allOpts)
            {
                var optId = byName.TryGetValue(row.OptionName, out var oid) ? oid : 0;
                if (optId == 0) continue; // option_name not in randomopt_db — skip
                if (!slotIndex.TryGetValue(row.GroupId, out var slotsForGroup))
                {
                    slotsForGroup = new Dictionary<int, List<GroupSlotOption>>();
                    slotIndex[row.GroupId] = slotsForGroup;
                }
                if (!slotsForGroup.TryGetValue(row.Slot, out var bucket))
                {
                    bucket = new List<GroupSlotOption>();
                    slotsForGroup[row.Slot] = bucket;
                }
                bucket.Add(new GroupSlotOption(optId, row.MinValue, row.MaxValue, row.Chance));
                slotRows++;
            }

            _optionsById = byId;
            _optionsByName = byName;
            _groupNameById = groupNameById;
            _groupIdByName = groupIdByName;
            _groupSlots = slotIndex;

            _logger.LogInformation(
                "item_randomopt_db loaded: {Options} options / {Groups} groups / {SlotRows} slot rows",
                _optionsById.Count, _groupSlots.Count, slotRows);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "item_randomopt_db load failed — catalog empty");
            _optionsById = new();
            _optionsByName = new(StringComparer.OrdinalIgnoreCase);
            _groupNameById = new();
            _groupIdByName = new(StringComparer.OrdinalIgnoreCase);
            _groupSlots = new();
        }
    }

    /// <summary>One resolved candidate for a slot inside a group.</summary>
    private readonly record struct GroupSlotOption(int OptionId, int MinValue, int MaxValue, int Chance);
}
