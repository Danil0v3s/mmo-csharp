using Map.Server.Entities;

namespace Map.Server.Inventory.ItemEffects;

/// <summary>
/// Flat-HP heal item. Each instance encodes one specific potion —
/// register one per aegis name in <see cref="ItemEffectRegistry"/>.
/// rAthena item Scripts that boil down to <c>itemheal X,0</c> reduce to
/// an instance of this handler.
/// </summary>
public sealed class HealHpHandler : IItemEffectHandler
{
    public string AegisName { get; }
    public int Amount { get; }

    public HealHpHandler(string aegisName, int amount)
    {
        AegisName = aegisName;
        Amount = amount;
    }

    public bool Apply(PlayerEntity user)
    {
        if (user.Hp >= user.MaxHp) return false;
        user.Hp = Math.Min(user.MaxHp, user.Hp + Amount);
        return true;
    }
}
