using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Port of rAthena <c>pc_setoption</c> (pc.cpp:8702) — toggles bits on
/// <see cref="PlayerEntity.Option"/>, recalculates speed where the
/// option matters (cart speed penalty, riding speed boost), and
/// broadcasts <c>ZC_STATE_CHANGE3</c> to the AOI.
///
/// Exposes per-feature wrappers (<see cref="SetCart"/>,
/// <see cref="SetRiding"/>, <see cref="SetFalcon"/>,
/// <see cref="SetMadogear"/>) that map to the corresponding rAthena
/// helpers (pc_setcart / pc_setriding / pc_setfalcon / pc_setmadogear).
/// </summary>
public interface IPlayerOptionService
{
    /// <summary>
    /// Set the option mask to <paramref name="next"/>. Diff vs current
    /// drives the broadcast — same value is a no-op.
    /// </summary>
    void SetOption(PlayerEntity pc, PlayerOption next);

    /// <summary>Add bits without removing existing ones.</summary>
    void AddOption(PlayerEntity pc, PlayerOption bits);

    /// <summary>Clear bits.</summary>
    void RemoveOption(PlayerEntity pc, PlayerOption bits);

    /// <summary>
    /// rAthena <c>pc_setcart</c> — toggle the cart sprite. <paramref name="type"/>
    /// 0 removes the cart; 1..5 select CART1..CART5.
    /// </summary>
    void SetCart(PlayerEntity pc, int type);

    /// <summary>
    /// rAthena <c>pc_setriding</c> — toggle Peco Peco riding.
    /// </summary>
    void SetRiding(PlayerEntity pc, bool on);

    /// <summary>rAthena <c>pc_setfalcon</c> — Hunter falcon toggle.</summary>
    void SetFalcon(PlayerEntity pc, bool on);

    /// <summary>
    /// rAthena <c>pc_setmadogear</c> — Mechanic madogear toggle. Type
    /// encodes Robot vs Suit visual variant.
    /// </summary>
    void SetMadogear(PlayerEntity pc, bool on);

    /// <summary>Re-broadcast current option state (used after warp).</summary>
    void NotifyOption(PlayerEntity pc);
}
