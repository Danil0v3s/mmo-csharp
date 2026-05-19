using Map.Server.Entities;

namespace Map.Server.Inventory.ItemEffects;

/// <summary>Flat-SP heal item. <c>itemheal 0,X</c> equivalent.</summary>
public sealed class HealSpHandler : IItemEffectHandler
{
    public string AegisName { get; }
    public int Amount { get; }

    public HealSpHandler(string aegisName, int amount)
    {
        AegisName = aegisName;
        Amount = amount;
    }

    public bool Apply(PlayerEntity user)
    {
        if (user.Sp >= user.MaxSp) return false;
        user.Sp = Math.Min(user.MaxSp, user.Sp + Amount);
        return true;
    }
}
