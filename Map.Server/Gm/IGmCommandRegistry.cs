namespace Map.Server.Gm;

/// <summary>
/// Lookup table from command name → <see cref="IGmCommand"/>. Populated
/// from DI at startup; reads are lock-free.
/// </summary>
public interface IGmCommandRegistry
{
    IGmCommand? Get(string name);
    IEnumerable<IGmCommand> All();
}
