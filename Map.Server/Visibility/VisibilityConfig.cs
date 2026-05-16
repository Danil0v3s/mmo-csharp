namespace Map.Server.Visibility;

/// <summary>
/// View-range constants matching rAthena defaults. AREA_SIZE = 14 (the
/// <c>battle_config.area_size</c> default in <c>battle.cpp</c>) bounds the
/// "visible" set to a (2·14+1)² = 29×29 cell square around an entity.
/// </summary>
public static class VisibilityConfig
{
    public const short AreaSize = 14;
}
