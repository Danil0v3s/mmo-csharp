using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@option &lt;opt1&gt; &lt;opt2&gt; &lt;option_hex&gt;</c> —
/// brute-force set the three state-change fields. rAthena
/// <c>atcommand_option</c> (atcommand.cpp:2400) — diagnostic command,
/// usually for debugging visual states.
/// </summary>
public sealed class OptionCommand(
    IPlayerOptionService options,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "option";
    public string Description => "@option <opt1> <opt2> <option_hex> — set the SC option triplet.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 3
            || !ushort.TryParse(args[0], out var opt1)
            || !ushort.TryParse(args[1], out var opt2)
            || !TryParseHex(args[2], out var opt))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = "@option: usage — @option <opt1:u16> <opt2:u16> <option:hex>",
            });
            return Task.CompletedTask;
        }
        caller.Opt1 = opt1;
        caller.Opt2 = opt2;
        options.SetOption(caller, (PlayerOption)opt);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@option: opt1={opt1} opt2={opt2} option=0x{(uint)caller.Option:X8}",
        });
        return Task.CompletedTask;
    }

    private static bool TryParseHex(string s, out uint v)
        => uint.TryParse(s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s,
            System.Globalization.NumberStyles.HexNumber, null, out v);
}
