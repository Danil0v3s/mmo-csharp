using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Player orb counters: spirit / soul / servant / abyss. rAthena
/// <c>pc_addspiritball</c> / <c>pc_delspiritball</c> family
/// (pc.cpp:480 area). All four share the wire packet shape
/// (<c>ZC_SPIRITS</c>) except <see cref="OrbKind.Soul"/> which uses
/// <c>ZC_SOULENERGY</c>.
///
/// Caps are mirrored from rAthena (MAX_SPIRITBALL = 15, etc.); skill
/// caller-side validation lives wherever the orb is consumed.
/// </summary>
public interface IPlayerOrbService
{
    /// <summary>Add <paramref name="count"/> orbs of <paramref name="kind"/>, clamped to the rAthena cap.</summary>
    void Add(PlayerEntity pc, OrbKind kind, int count);

    /// <summary>Remove (consume) <paramref name="count"/> orbs.</summary>
    void Remove(PlayerEntity pc, OrbKind kind, int count);

    /// <summary>Re-broadcast the current count for one orb kind (used after warp).</summary>
    void Notify(PlayerEntity pc, OrbKind kind);

    /// <summary>Look up current count.</summary>
    int Get(PlayerEntity pc, OrbKind kind);

    /// <summary>
    /// rAthena <c>pc_addspiritcharm</c> (pc.cpp:8918) — Kagerou /
    /// Oboro element charm system. Differs from spirit/soul/servant
    /// orbs because the count carries an element TYPE
    /// (<paramref name="elementType"/>, rAthena <c>ELE_*</c>:
    /// 1=WATER, 2=EARTH, 3=FIRE, 4=WIND). Charms of a different
    /// element are dropped when a new element is added — only one
    /// element can be active at a time. <paramref name="count"/> is
    /// clamped to <c>MAX_SPIRITCHARM</c> (10).
    /// <paramref name="durationMs"/> is recorded on
    /// <see cref="PlayerEntity.SpiritCharmExpireTick"/> for the
    /// auto-expire scan; 0 = no expiry tracking (test convenience).
    /// </summary>
    void AddCharm(PlayerEntity pc, int elementType, int count, int durationMs);

    /// <summary>rAthena <c>pc_delspiritcharm</c> — clear all charms (any element).</summary>
    void RemoveCharms(PlayerEntity pc);
}

public enum OrbKind
{
    Spirit,   // pc_addspiritball — Monk/Sura
    Soul,     // pc_addsoulball — Soul Reaper (ZC_SOULENERGY)
    Servant,  // pc_addservantball — Servant Weapon
    Abyss,    // pc_addabyssball — Abyss Chaser
}
