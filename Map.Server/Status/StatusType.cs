namespace Map.Server.Status;

/// <summary>
/// Subset of rAthena's <c>enum sc_type</c> (status.hpp:233) the C#
/// engine ports first. Values are kept in lockstep with rAthena indices
/// so persistence (<c>SaveStatusChangeDataAsync</c> / <c>RequestStatusChangeDataAsync</c>)
/// can round-trip without remapping. New SCs add their rAthena value as
/// they port; gaps are intentional.
/// </summary>
public enum StatusType : short
{
    None = -1,

    // Common ailments (status.hpp:237).
    Stone = 0,
    Freeze,
    Stun,
    Sleep,
    Poison,
    Curse,
    Silence,
    Confusion,
    Blind,
    Bleeding,
    DeadlyPoison,

    // Buffs (status.hpp:253).
    Provoke = 20,
    Endure = 21,
    Concentrate = 23,
    Blessing = 30,
    IncreaseAgi = 32,
    DecreaseAgi = 33,
    Magnificat = 40,

    // Combat-physical (status.hpp:280).
    Adrenaline = 43,
    WeaponPerfection = 44,
    Overthrust = 45,

    // Heal-over-time and damage-over-time anchors used by skills/items.
    HealOverTime = 100,
    AttackUpRate = 101,

    // Ground-unit cell effects — applied while standing on a Basilica
    // / Land Protector cell. Removed when the player steps off.
    Basilica = 130,
}
