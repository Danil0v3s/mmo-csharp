using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@cart &lt;type&gt;</c> — toggle / pick Pushcart variant.
/// rAthena <c>atcommand_cart</c> (atcommand.cpp:5544). Type 0 removes;
/// 1..5 select CART1..CART5 sprites.
/// </summary>
public sealed class CartCommand(
    IPlayerOptionService options,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "cart";
    public string Description => "@cart <0..5> — set or clear the pushcart sprite.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var type = 1;
        if (args.Count > 0 && int.TryParse(args[0], out var t)) type = Math.Clamp(t, 0, 5);
        options.SetCart(caller, type);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = type == 0 ? "@cart: removed." : $"@cart: variant {type}.",
        });
        return Task.CompletedTask;
    }
}
