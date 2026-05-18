namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerWarp({ ... })</c> call. Mirrors the shape of a row in the
/// existing <c>warp</c> table (see [declarative-catalogs.md]); script-side
/// warps coexist with DB-loaded warps in <c>WarpService</c>.
/// </summary>
public sealed record WarpRegistration
{
    public required string FromMap { get; init; }
    public required short FromX { get; init; }
    public required short FromY { get; init; }
    public required short AreaXs { get; init; }
    public required short AreaYs { get; init; }
    public required string ToMap { get; init; }
    public required short ToX { get; init; }
    public required short ToY { get; init; }
    /// <summary>"warp" (default) or "warp2" (triggers for hidden players too).</summary>
    public string Type { get; init; } = "warp";
}
