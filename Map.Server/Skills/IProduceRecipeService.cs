namespace Map.Server.Skills;

/// <summary>
/// In-memory snapshot of rAthena <c>db/produce_db.txt</c>. Hydrated
/// from the SQL <c>produce_recipe_db</c> + <c>produce_recipe_material_db</c>
/// catalog at boot. Read-only at runtime — produces / forges look
/// recipes up by id (specific recipe) or by skill id (every recipe
/// the caster is qualified to make).
/// </summary>
public interface IProduceRecipeService
{
    /// <summary>Total recipes loaded (0 before Reload).</summary>
    int Count { get; }

    /// <summary>Get a recipe by its <c>produce_db.txt</c> row id.</summary>
    ProduceRecipe? Get(int recipeId);

    /// <summary>
    /// Every recipe whose <c>RequireSkillId</c> matches
    /// <paramref name="requireSkillId"/> and whose <c>RequireSkillLv</c>
    /// is &lt;= <paramref name="callerSkillLv"/>. The caller filters
    /// further (item_lv ≤ pharmacy tier, etc.).
    /// </summary>
    IReadOnlyList<ProduceRecipe> GetByRequireSkill(ushort requireSkillId, byte callerSkillLv);

    /// <summary>Rebuild the snapshot from the SQL catalog (GM <c>/reloaddb</c>).</summary>
    void Reload();
}

/// <summary>
/// One produce-mix recipe. Materials are immutable after load.
/// </summary>
public sealed class ProduceRecipe
{
    public required int Id { get; init; }
    public required uint ProduceItemId { get; init; }
    public required byte ItemLv { get; init; }
    public required ushort RequireSkillId { get; init; }
    public required byte RequireSkillLv { get; init; }
    /// <summary>Material slots in declaration order. <c>Amount == 0</c>
    /// means "must own — does not consume" (rAthena guide-item flag).</summary>
    public required IReadOnlyList<ProduceMaterial> Materials { get; init; }
}

public readonly record struct ProduceMaterial(uint ItemId, uint Amount);
