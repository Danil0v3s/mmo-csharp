using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Status;

/// <summary>
/// Concrete <see cref="IPlayerOptionService"/>. One service powers
/// every option toggle so the broadcast pattern is uniform; rAthena's
/// per-helper functions (pc_setcart / pc_setriding / etc.) collapse
/// here.
///
/// Speed effects are NOT applied yet — status_calc_pc will read the
/// option mask once renewal cart/riding speed mods port (rAthena
/// status.cpp:5400 area).
/// </summary>
public sealed class PlayerOptionService : IPlayerOptionService
{
    private readonly IVisibilityService _visibility;

    public PlayerOptionService(IVisibilityService visibility)
    {
        _visibility = visibility;
    }

    public void SetOption(PlayerEntity pc, PlayerOption next)
    {
        if (pc.Option == next) return;
        pc.Option = next;
        NotifyOption(pc);
    }

    public void AddOption(PlayerEntity pc, PlayerOption bits)
        => SetOption(pc, pc.Option | bits);

    public void RemoveOption(PlayerEntity pc, PlayerOption bits)
        => SetOption(pc, pc.Option & ~bits);

    public void SetCart(PlayerEntity pc, int type)
    {
        var next = pc.Option & ~PlayerOptionExtensions.AnyCart;
        next |= type switch
        {
            1 => PlayerOption.Cart1,
            2 => PlayerOption.Cart2,
            3 => PlayerOption.Cart3,
            4 => PlayerOption.Cart4,
            5 => PlayerOption.Cart5,
            _ => PlayerOption.Nothing,
        };
        SetOption(pc, next);
    }

    public void SetRiding(PlayerEntity pc, bool on)
    {
        if (on) AddOption(pc, PlayerOption.Riding);
        else RemoveOption(pc, PlayerOption.Riding);
    }

    public void SetFalcon(PlayerEntity pc, bool on)
    {
        if (on) AddOption(pc, PlayerOption.Falcon);
        else RemoveOption(pc, PlayerOption.Falcon);
    }

    public void SetMadogear(PlayerEntity pc, bool on)
    {
        if (on) AddOption(pc, PlayerOption.Madogear);
        else RemoveOption(pc, PlayerOption.Madogear);
    }

    public void NotifyOption(PlayerEntity pc)
    {
        // rAthena clif_changeoption_target sends ZC_STATE_CHANGE3 to
        // every observer in the AOI. The packet includes opt1/opt2 so
        // the client refreshes both ailment + appearance state together.
        var packet = new ZC_STATE_CHANGE3
        {
            AccountId = (uint)pc.AccountId,
            BodyState = pc.Opt1,
            HealthState = pc.Opt2,
            EffectState = (uint)pc.Option,
            PkMode = pc.Karma,
        };
        _visibility.SendToArea(pc, packet);
    }
}
