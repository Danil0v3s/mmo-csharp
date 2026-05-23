using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Map.Server.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IItemCombosService"/>. Loads the full
/// item_combo_db / item_combo_member_db catalog at boot and exposes
/// the equipped-set detection primitive consumers can call after
/// every equip/unequip.
///
/// <para>
/// Detection algorithm: for every equipped item aegis, look up
/// candidate combos via the inverted index, then verify ALL members
/// of each candidate are present in the equipped set. O(equipped ×
/// per-item-candidates), which is small in practice (most items
/// participate in 0..few combos).
/// </para>
///
/// <para>
/// Script application is currently log-only — the rAthena bonus
/// script body in <see cref="ActiveCombo.Script"/> needs the Jint
/// engine port to actually compute the stat delta. Today the consumer
/// gets back the firing combo list so equipment-set UIs (and future
/// bonus-script consumers) can read it without re-walking the catalog.
/// </para>
/// </summary>
public sealed class ItemCombosService : IItemCombosService
{
    private readonly IItemCatalog _itemCatalog;
    private readonly ILogger<ItemCombosService> _logger;

    // ComboId → (member aegis names, script body).
    private readonly Dictionary<int, ComboEntry> _combos = new();
    // Inverted index: itemAegis (case-insensitive) → combo ids that include it.
    private readonly Dictionary<string, List<int>> _byMember =
        new(StringComparer.OrdinalIgnoreCase);

    public int CatalogCount => _combos.Count;

    public ItemCombosService(
        IItemCatalog itemCatalog,
        IServiceScopeFactory scopes,
        ILogger<ItemCombosService> logger)
    {
        _itemCatalog = itemCatalog;
        _logger = logger;
        Load(scopes);
    }

    private void Load(IServiceScopeFactory scopes)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IItemComboDbRepository>();
            var parents = repo.GetAllAsync().GetAwaiter().GetResult();
            var members = repo.GetAllMembersAsync().GetAwaiter().GetResult();

            var membersByCombo = new Dictionary<int, List<string>>();
            foreach (var m in members)
            {
                if (!membersByCombo.TryGetValue(m.ComboId, out var list))
                {
                    list = new List<string>();
                    membersByCombo[m.ComboId] = list;
                }
                list.Add(m.MemberItemAegis);
            }

            foreach (var p in parents)
            {
                if (!membersByCombo.TryGetValue(p.ComboId, out var memberList) || memberList.Count == 0)
                    continue;
                var memberArr = memberList.ToArray();
                _combos[p.ComboId] = new ComboEntry(memberArr, p.Script);
                foreach (var aegis in memberArr)
                {
                    if (!_byMember.TryGetValue(aegis, out var idxList))
                    {
                        idxList = new List<int>();
                        _byMember[aegis] = idxList;
                    }
                    idxList.Add(p.ComboId);
                }
            }

            _logger.LogInformation(
                "item_combo_db loaded: {Combos} combos / {Members} members; inverted index {Items} items",
                _combos.Count, members.Count, _byMember.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "item_combo_db load failed; combo detection disabled");
        }
    }

    public IReadOnlyList<ActiveCombo> RecomputeCombos(MapSessionData session)
    {
        if (_combos.Count == 0) return Array.Empty<ActiveCombo>();
        if (session.Inventory is not { } inv) return Array.Empty<ActiveCombo>();

        // 1. Build equipped-aegis set (case-insensitive).
        var equippedAegis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < inv.Count; i++)
        {
            var item = inv[i];
            if (item.NameId == 0 || item.Amount == 0 || item.Equip == 0) continue;
            var row = _itemCatalog.Get(item.NameId);
            if (row == null || string.IsNullOrEmpty(row.NameAegis)) continue;
            equippedAegis.Add(row.NameAegis);
        }
        if (equippedAegis.Count == 0) return Array.Empty<ActiveCombo>();

        // 2. Collect candidate combo ids by inverted index (dedup).
        var candidates = new HashSet<int>();
        foreach (var aegis in equippedAegis)
        {
            if (_byMember.TryGetValue(aegis, out var ids))
                foreach (var id in ids) candidates.Add(id);
        }
        if (candidates.Count == 0) return Array.Empty<ActiveCombo>();

        // 3. For each candidate combo, check ALL members are equipped.
        var active = new List<ActiveCombo>();
        foreach (var id in candidates)
        {
            var entry = _combos[id];
            var allEquipped = true;
            for (var i = 0; i < entry.Members.Length; i++)
            {
                if (!equippedAegis.Contains(entry.Members[i])) { allEquipped = false; break; }
            }
            if (allEquipped) active.Add(new ActiveCombo(id, entry.Script));
        }
        return active;
    }

    private readonly record struct ComboEntry(string[] Members, string Script);
}
