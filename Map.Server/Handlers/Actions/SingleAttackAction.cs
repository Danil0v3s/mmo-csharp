using Map.Server.Combat;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Handlers.Actions;

/// <summary>rAthena DMG_NORMAL (action=0) — one-shot melee swing.</summary>
public sealed class SingleAttackAction : IActionHandler
{
    public byte ActionCode => 0;

    private readonly IAttackService _attack;
    private readonly ILogger<SingleAttackAction> _logger;

    public SingleAttackAction(IAttackService attack, ILogger<SingleAttackAction> logger)
    {
        _attack = attack;
        _logger = logger;
    }

    public void Apply(PlayerEntity player, int targetId)
    {
        if (!_attack.StartAttack(player, new EntityId(targetId), continuous: false))
        {
            _logger.LogDebug("Single-attack rejected: char {Char} → target {Target}",
                player.CharacterId, targetId);
        }
    }
}
