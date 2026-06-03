using System;

namespace Map.Server.Quest;

/// <summary>
/// FEATURE-03 — port of rAthena <c>quest_time</c> + <c>solve_time</c> +
/// <c>split_exact_quest_time</c> (quest.cpp): converts a <c>quest_db</c> <c>TimeLimit</c> string into
/// an absolute unix expiry, or 0 for "no limit".
///
/// <para>Two grammars (rAthena <c>QuestDatabase::parseBodyNode</c>):</para>
/// <list type="bullet">
///   <item><b>Relative</b> (contains <c>+</c>, e.g. <c>+3h</c>, <c>+30mn</c>, <c>+2h30mn</c>,
///   <c>+7d</c>): a duration added to "now". Units <c>d</c>/<c>j</c>=day, <c>h</c>=hour,
///   <c>mn</c>=minute, <c>s</c>=second.</item>
///   <item><b>Absolute</b> (no <c>+</c>, e.g. <c>4h</c>, <c>7d 4h</c>, <c>Monday 4h</c>): the next
///   occurrence of a daily/weekly reset time. <c>hour</c> is required (0–23).</item>
/// </list>
/// </summary>
internal static class QuestTime
{
    /// <summary>Absolute unix expiry, or 0 = no limit. <paramref name="localNow"/> supplies the
    /// server-local wall clock (rAthena uses <c>localtime</c>); pass the same instant as
    /// <paramref name="nowUnix"/> for consistency (injectable for tests).</summary>
    public static long ParseTimeUnix(string? timeLimit, long nowUnix, DateTimeOffset localNow)
    {
        if (string.IsNullOrWhiteSpace(timeLimit)) return 0;

        if (timeLimit.Contains('+'))
        {
            var seconds = SolveRelativeSeconds(timeLimit);
            return seconds <= 0 ? 0 : nowUnix + seconds;
        }

        if (!SplitExact(timeLimit, out var week, out var day, out var hour, out var minute, out var second))
            return 0;

        // rAthena: week>0 ⇒ weekly (time-of-day only); else day-offset + time-of-day.
        long qtime = week > 0
            ? hour * 3600L + minute * 60 + second
            : day * 86400L + hour * 3600 + minute * 60 + second;

        int wday = (int)localNow.DayOfWeek; // Sunday=0, matches tm_wday
        long timeToday = localNow.Hour * 3600L + localNow.Minute * 60 + localNow.Second;
        long dayShift = 0;
        if (timeToday >= (qtime % 86400)) dayShift = 1; // already past today's reset → next day
        if (week > -1)
        {
            if (week < wday + dayShift) dayShift = week + 7 - wday;
            else dayShift = week - wday;
        }
        return nowUnix + dayShift * 86400 + qtime - timeToday;
    }

    /// <summary>rAthena <c>solve_time</c> (relative): sum of <c>&lt;n&gt;{d|j|h|mn|s}</c> tokens.</summary>
    private static long SolveRelativeSeconds(string s)
    {
        long total = 0;
        int i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (c is '+' or '-' or ' ') { i++; continue; }
            if (!char.IsDigit(c)) { i++; continue; }
            int start = i;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            long val = long.Parse(s.AsSpan(start, i - start));
            if (i + 1 < s.Length && s[i] == 'm' && s[i + 1] == 'n') { total += val * 60; i += 2; }
            else if (i < s.Length && s[i] == 's') { total += val; i++; }
            else if (i < s.Length && s[i] == 'h') { total += val * 3600; i++; }
            else if (i < s.Length && (s[i] == 'd' || s[i] == 'j')) { total += val * 86400; i++; }
            else if (i < s.Length) { i++; } // unknown unit — skip
        }
        return total;
    }

    /// <summary>rAthena <c>split_exact_quest_time</c>: parse an absolute reset spec. Returns false if
    /// no valid hour (0–23) was given.</summary>
    private static bool SplitExact(string s, out int week, out int day, out int hour, out int minute, out int second)
    {
        week = -1;
        int d = -1, h = -1, mn = -1, sec = -1;
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '+' || s[i] == '-') i++;
            int start = i;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            int val = i > start ? int.Parse(s.AsSpan(start, i - start)) : 0;

            if (MatchCi(s, i, "SUNDAY")) { week = 0; i += 6; }
            else if (MatchCi(s, i, "MONDAY")) { week = 1; i += 6; }
            else if (MatchCi(s, i, "TUESDAY")) { week = 2; i += 7; }
            else if (MatchCi(s, i, "WEDNESDAY")) { week = 3; i += 9; }
            else if (MatchCi(s, i, "THURSDAY")) { week = 4; i += 8; }
            else if (MatchCi(s, i, "FRIDAY")) { week = 5; i += 6; }
            else if (MatchCi(s, i, "SATURDAY")) { week = 6; i += 8; }
            else if (i < s.Length && s[i] == 's') { sec = val; i++; }
            else if (i + 1 < s.Length && s[i] == 'm' && s[i + 1] == 'n') { mn = val; i += 2; }
            else if (i < s.Length && s[i] == 'h') { h = val; i++; }
            else if (i < s.Length && (s[i] == 'd' || s[i] == 'j')) { d = val; i++; }
            else if (i < s.Length) { i++; }
        }

        if (h < 0 || h > 23 || mn > 59 || sec > 59)
        {
            day = hour = minute = second = 0;
            return false; // hour is required (rAthena)
        }
        day = Math.Max(0, d);
        hour = h;
        minute = Math.Max(0, mn);
        second = Math.Max(0, sec);
        return true;
    }

    private static bool MatchCi(string s, int i, string token)
        => i + token.Length <= s.Length
           && string.Compare(s, i, token, 0, token.Length, StringComparison.OrdinalIgnoreCase) == 0;
}
