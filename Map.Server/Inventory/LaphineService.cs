using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="ILaphineService"/>. Loads both Lapine catalogs
/// at boot from laphine_synthesis_db + laphine_upgrade_db (+ their
/// requirement / target child tables) and exposes per-opener lookups.
/// </summary>
public sealed class LaphineService : ILaphineService
{
    private readonly Dictionary<string, LaphineSynthesisConfig> _synthByOpener =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LaphineUpgradeConfig> _upgradeByOpener =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<LaphineService> _logger;

    public (int Synthesis, int Upgrade) CatalogCount =>
        (_synthByOpener.Count, _upgradeByOpener.Count);

    public LaphineService(IServiceScopeFactory scopes, ILogger<LaphineService> logger)
    {
        _logger = logger;
        Load(scopes);
    }

    private void Load(IServiceScopeFactory scopes)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var synth = scope.ServiceProvider.GetRequiredService<ILaphineSynthesisDbRepository>();
            var upgrade = scope.ServiceProvider.GetRequiredService<ILaphineUpgradeDbRepository>();

            // ----- synthesis -----
            var synthParents = synth.GetAllAsync().GetAwaiter().GetResult();
            foreach (var p in synthParents)
            {
                var reqs = synth.GetRequirementsAsync(p.RecipeItem).GetAwaiter().GetResult();
                var list = new List<LaphineRequirement>(reqs.Count);
                foreach (var r in reqs)
                    list.Add(new LaphineRequirement(r.RequirementItem, r.RefineMin, r.RefineMax));
                _synthByOpener[p.RecipeItem] = new LaphineSynthesisConfig(
                    p.RecipeItem, p.RewardGroup, p.RequiredRequirementsCount, list);
            }

            // ----- upgrade -----
            var upgradeParents = upgrade.GetAllAsync().GetAwaiter().GetResult();
            foreach (var p in upgradeParents)
            {
                var targets = upgrade.GetTargetsAsync(p.UpgradeItem).GetAwaiter().GetResult();
                var targetList = new List<string>(targets.Count);
                foreach (var t in targets) targetList.Add(t.TargetItem);
                _upgradeByOpener[p.UpgradeItem] = new LaphineUpgradeConfig(
                    p.UpgradeItem, p.MinimumRefine, targetList);
            }

            _logger.LogInformation(
                "laphine catalogs loaded: synthesis={Synth}, upgrade={Up}",
                _synthByOpener.Count, _upgradeByOpener.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "laphine catalog load failed; Lapine pipelines disabled");
        }
    }

    public LaphineSynthesisConfig? GetSynthesis(string openerItemAegis)
        => _synthByOpener.GetValueOrDefault(openerItemAegis);

    public LaphineUpgradeConfig? GetUpgrade(string openerItemAegis)
        => _upgradeByOpener.GetValueOrDefault(openerItemAegis);
}
