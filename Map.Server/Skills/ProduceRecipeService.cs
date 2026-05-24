using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="IProduceRecipeService"/>. Loads the
/// <c>produce_recipe_db</c> + materials child table once at boot and
/// caches in two Dictionaries: by recipe id (O(1) lookup) and by
/// require-skill id (O(1) bucket scan).
/// </summary>
public sealed class ProduceRecipeService : IProduceRecipeService
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<ProduceRecipeService> _logger;
    private Dictionary<int, ProduceRecipe> _byId = new();
    private Dictionary<ushort, List<ProduceRecipe>> _bySkill = new();

    public ProduceRecipeService(IServiceScopeFactory scopes, ILogger<ProduceRecipeService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    public ProduceRecipeService(ILogger<ProduceRecipeService> logger) { _logger = logger; }

    public int Count => _byId.Count;

    public ProduceRecipe? Get(int recipeId)
        => _byId.TryGetValue(recipeId, out var v) ? v : null;

    public IReadOnlyList<ProduceRecipe> GetByRequireSkill(ushort requireSkillId, byte callerSkillLv)
    {
        if (!_bySkill.TryGetValue(requireSkillId, out var bucket))
            return Array.Empty<ProduceRecipe>();
        var result = new List<ProduceRecipe>(bucket.Count);
        foreach (var r in bucket)
            if (r.RequireSkillLv <= callerSkillLv) result.Add(r);
        return result;
    }

    public void Reload()
    {
        _byId.Clear();
        _bySkill.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IProduceRecipeDbRepository>();
            var rows = repo.GetAllAsync().GetAwaiter().GetResult();
            var mats = repo.GetAllMaterialsAsync().GetAwaiter().GetResult();
            // Group materials by recipe in declaration order.
            var matsByRecipe = new Dictionary<int, List<ProduceMaterial>>();
            foreach (var m in mats.OrderBy(m => m.RecipeId).ThenBy(m => m.SlotIndex))
            {
                if (!matsByRecipe.TryGetValue(m.RecipeId, out var list))
                    matsByRecipe[m.RecipeId] = list = new List<ProduceMaterial>();
                list.Add(new ProduceMaterial(m.MaterialItemId, m.MaterialAmount));
            }
            foreach (var row in rows)
            {
                var recipe = new ProduceRecipe
                {
                    Id = row.Id,
                    ProduceItemId = row.ProduceItemId,
                    ItemLv = row.ItemLv,
                    RequireSkillId = row.RequireSkillId,
                    RequireSkillLv = row.RequireSkillLv,
                    Materials = matsByRecipe.TryGetValue(row.Id, out var list)
                        ? list
                        : (IReadOnlyList<ProduceMaterial>)Array.Empty<ProduceMaterial>(),
                };
                _byId[recipe.Id] = recipe;
                if (!_bySkill.TryGetValue(recipe.RequireSkillId, out var bucket))
                    _bySkill[recipe.RequireSkillId] = bucket = new List<ProduceRecipe>();
                bucket.Add(recipe);
            }
            _logger.LogInformation(
                "produce_recipe_db loaded: {Recipes} recipe(s), {Skills} skill bucket(s), {Materials} material row(s)",
                _byId.Count, _bySkill.Count, mats.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "produce_recipe_db load failed — catalog empty");
        }
    }
}
