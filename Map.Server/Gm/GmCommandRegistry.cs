namespace Map.Server.Gm;

public sealed class GmCommandRegistry : IGmCommandRegistry
{
    private readonly Dictionary<string, IGmCommand> _byName;

    public GmCommandRegistry(IEnumerable<IGmCommand> commands)
    {
        _byName = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IGmCommand? Get(string name) => _byName.GetValueOrDefault(name);

    public IEnumerable<IGmCommand> All() => _byName.Values;
}
