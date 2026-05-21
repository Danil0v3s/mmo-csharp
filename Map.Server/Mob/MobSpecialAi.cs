namespace Map.Server.Mob;

/// <summary>
/// Port of rAthena <c>enum mob_ai</c> (map.hpp:436). Set on
/// <see cref="Map.Server.Entities.MobEntity.SpecialAi"/> when the
/// mob is summoned by a script or player skill (homunculus,
/// Cannibalize sphere, Alchemist ABR/Bionic, etc.) rather than
/// spawned by the world. MSC_ALCHEMIST fires only when this is
/// non-<see cref="None"/>; the slave-stick / cannibalize / sphere
/// branches also key off the specific value.
/// </summary>
public enum MobSpecialAi : byte
{
    None = 0,
    Attack,
    Sphere,
    Flora,
    Zanzou,
    Legion,
    Faw,
    Guild,
    WaveMode,
    Abr,
    Bionic,
}
