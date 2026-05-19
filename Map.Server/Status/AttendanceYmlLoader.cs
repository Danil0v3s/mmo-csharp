using System.Globalization;
using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Map.Server.Status;

/// <summary>
/// Loads rAthena <c>db/re/attendance.yml</c> into
/// <see cref="PlayerAttendanceService.AttendanceWindow"/>. The
/// schedule is optional: rAthena ships the file with the example
/// block commented out, so a missing or empty file just means
/// attendance is disabled (as in vanilla).
///
/// Format reminder:
/// <code>
/// Body:
///   - Start: 20180502           # YYYYMMDD
///     End:   20180529
///     Rewards:
///       - Day: 1
///         ItemId: 22979
///         Amount: 1             # default 1 if missing
/// </code>
/// </summary>
public sealed class AttendanceYmlLoader
{
    private readonly ILogger<AttendanceYmlLoader> _logger;

    public AttendanceYmlLoader(ILogger<AttendanceYmlLoader> logger) => _logger = logger;

    public PlayerAttendanceService.AttendanceWindow? Load(string yamlPath)
    {
        if (!File.Exists(yamlPath))
        {
            _logger.LogInformation("attendance.yml not found at {Path} — attendance disabled", yamlPath);
            return null;
        }
        using var reader = File.OpenText(yamlPath);
        var stream = new YamlStream();
        try { stream.Load(reader); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "attendance.yml parse error — attendance disabled");
            return null;
        }
        if (stream.Documents.Count == 0) return null;
        if (stream.Documents[0].RootNode is not YamlMappingNode root) return null;
        if (!root.Children.TryGetValue("Body", out var bodyNode) || bodyNode is not YamlSequenceNode body) return null;

        // Pick the first window whose date range covers today; rAthena
        // supports overlapping windows but only the first match runs.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var item in body.Children.OfType<YamlMappingNode>())
        {
            if (!TryDate(item, "Start", out var start) || !TryDate(item, "End", out var end)) continue;
            if (today < start || today > end) continue;
            var rewards = new Dictionary<int, PlayerAttendanceService.AttendanceReward>();
            if (item.Children.TryGetValue("Rewards", out var rNode) && rNode is YamlSequenceNode rSeq)
            {
                foreach (var r in rSeq.Children.OfType<YamlMappingNode>())
                {
                    if (!TryInt(r, "Day", out var day)) continue;
                    if (!TryInt(r, "ItemId", out var itemId)) continue;
                    var amount = TryInt(r, "Amount", out var a) ? a : 1;
                    rewards[day] = new PlayerAttendanceService.AttendanceReward((uint)itemId, amount);
                }
            }
            _logger.LogInformation(
                "attendance.yml: active window {Start}..{End} with {N} rewards",
                start, end, rewards.Count);
            return new PlayerAttendanceService.AttendanceWindow(start, end, rewards);
        }
        _logger.LogInformation("attendance.yml: no active window for {Today}", today);
        return null;
    }

    private static bool TryDate(YamlMappingNode node, string key, out DateOnly value)
    {
        value = default;
        if (!node.Children.TryGetValue(key, out var v) || v is not YamlScalarNode s) return false;
        // rAthena format is YYYYMMDD as an integer.
        return DateOnly.TryParseExact(s.Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    private static bool TryInt(YamlMappingNode node, string key, out int value)
    {
        value = 0;
        if (!node.Children.TryGetValue(key, out var v) || v is not YamlScalarNode s) return false;
        return int.TryParse(s.Value, out value);
    }
}
