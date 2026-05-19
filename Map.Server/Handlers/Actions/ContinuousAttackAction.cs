using Map.Server.Combat;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Handlers.Actions;

/// <summary>rAthena DMG_REPEAT (action=7) — continuous auto-attack.</summary>
public sealed class ContinuousAttackAction : IActionHandler
{
    public byte ActionCode => 7;

    private readonly IAttackService _attack;
    private readonly ILogger<ContinuousAttackAction> _logger;

    public ContinuousAttackAction(IAttackService attack, ILogger<ContinuousAttackAction> logger)
    {
        _attack = attack;
        _logger = logger;
    }

    public void Apply(PlayerEntity player, int targetId)
    {
        if (!_attack.StartAttack(player, new EntityId(targetId), continuous: true))
        {
            _logger.LogDebug("Continuous-attack rejected: char {Char} → target {Target}",
                player.CharacterId, targetId);
        }
    }
}
