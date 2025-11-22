namespace Core.Timer;

/// <summary>
/// Utility functions for time manipulation
/// </summary>
public static class TimeUtils
{
    /// <summary>
    /// Converts a timestamp to a formatted string
    /// </summary>
    public static string TimestampToString(long timestamp, string format = "yyyy-MM-dd HH:mm:ss")
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        return dateTime.ToString(format);
    }

    /// <summary>
    /// Splits time in seconds into years, months, days, hours, minutes, seconds
    /// </summary>
    public static void SplitTime(int timeInSeconds, out int year, out int month, out int day, 
        out int hour, out int minute, out int second)
    {
        const int factorMin = 60;
        const int factorHour = factorMin * 60;
        const int factorDay = factorHour * 24;
        const int factorMonth = 2629743; // Approx (30.44 days)
        const int factorYear = 31556926; // Approx (365.24 days)

        year = timeInSeconds / factorYear;
        timeInSeconds -= year * factorYear;

        month = timeInSeconds / factorMonth;
        timeInSeconds -= month * factorMonth;

        day = timeInSeconds / factorDay;
        timeInSeconds -= day * factorDay;

        hour = timeInSeconds / factorHour;
        timeInSeconds -= hour * factorHour;

        minute = timeInSeconds / factorMin;
        timeInSeconds -= minute * factorMin;

        second = timeInSeconds;

        // Ensure non-negative
        year = Math.Max(0, year);
        month = Math.Max(0, month);
        day = Math.Max(0, day);
        hour = Math.Max(0, hour);
        minute = Math.Max(0, minute);
        second = Math.Max(0, second);
    }

    /// <summary>
    /// Parses a time modifier string and returns the total time in seconds
    /// Format: +/-[value][unit] where unit can be: s(seconds), n/mn(minutes), h(hours), d/j(days), m(months), y/a(years)
    /// Example: "+5h30mn" = 5 hours and 30 minutes from now
    /// </summary>
    public static double SolveTime(string modifierString)
    {
        if (string.IsNullOrEmpty(modifierString))
            return 0;

        var now = DateTime.Now;
        var then = now;
        int i = 0;

        while (i < modifierString.Length)
        {
            if (!char.IsDigit(modifierString[i]) && modifierString[i] != '-' && modifierString[i] != '+')
            {
                i++;
                continue;
            }

            // Parse the number
            int sign = 1;
            if (modifierString[i] == '-')
            {
                sign = -1;
                i++;
            }
            else if (modifierString[i] == '+')
            {
                i++;
            }

            int value = 0;
            while (i < modifierString.Length && char.IsDigit(modifierString[i]))
            {
                value = value * 10 + (modifierString[i] - '0');
                i++;
            }
            value *= sign;

            if (i >= modifierString.Length)
                break;

            // Parse the unit
            char unit = modifierString[i];
            i++;

            switch (unit)
            {
                case 's': // seconds
                    then = then.AddSeconds(value);
                    break;
                case 'n': // minutes (single char)
                    then = then.AddMinutes(value);
                    break;
                case 'm':
                    if (i < modifierString.Length && modifierString[i] == 'n')
                    {
                        // minutes (mn)
                        then = then.AddMinutes(value);
                        i++;
                    }
                    else
                    {
                        // months
                        then = then.AddMonths(value);
                    }
                    break;
                case 'h': // hours
                    then = then.AddHours(value);
                    break;
                case 'd': // days
                case 'j': // jours (French for days)
                    then = then.AddDays(value);
                    break;
                case 'y': // years
                case 'a': // ans (French for years)
                    then = then.AddYears(value);
                    break;
            }
        }

        return (then - now).TotalSeconds;
    }
}

