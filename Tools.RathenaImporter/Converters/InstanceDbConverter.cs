using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary><c>db/re/instance_db.yml</c> → <c>seed_instance_db.sql</c>.</summary>
public sealed class InstanceDbConverter : IYamlToSqlConverter
{
    public string Name => "instance_db";
    public string SourceYamlPath => "db/re/instance_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_instance_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id"); if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("instance_db",
                new[] {
                    "instance_id", "name", "time_limit", "idle_timeout",
                    "enter_map", "enter_x", "enter_y", "additional_maps",
                },
                new object?[] {
                    (uint)id.Value,
                    row.Str("Name") ?? "",
                    row.Int("TimeLimit") ?? 0,
                    row.Int("IdleTimeOut") ?? 0,
                    row.Get("Enter") is YamlDotNet.RepresentationModel.YamlMappingNode enter ? enter.Str("Map") : null,
                    row.Get("Enter") is YamlDotNet.RepresentationModel.YamlMappingNode enter2 ? enter2.Int("X") : null,
                    row.Get("Enter") is YamlDotNet.RepresentationModel.YamlMappingNode enter3 ? enter3.Int("Y") : null,
                    row.Get("AdditionalMaps") is YamlDotNet.RepresentationModel.YamlSequenceNode addl
                        ? string.Join(',', addl.Children.OfType<YamlDotNet.RepresentationModel.YamlMappingNode>().Select(c => c.Str("Map")).Where(s => s != null))
                        : null,
                }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
