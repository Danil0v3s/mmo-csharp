namespace Login.Server.UseCase;

/// <summary>
/// Mirrors rAthena <c>login_get_usercount</c> (login.cpp:484). On
/// PACKETVER ≥ 20170726 the wire <c>user_count</c> field stops being
/// a raw count and becomes a status code that the client renders as a
/// colored dot in the char-server list:
///
/// <list type="bullet">
///   <item><term>0</term><description>Green — Smooth (low)</description></item>
///   <item><term>1</term><description>Yellow — Normal (medium)</description></item>
///   <item><term>2</term><description>Red — Busy (high)</description></item>
///   <item><term>3</term><description>Purple — Crowded (over high)</description></item>
///   <item><term>4</term><description>Hidden — when <c>UserCountDisable</c> is set</description></item>
/// </list>
/// </summary>
public static class CharServerUserCountClassifier
{
    public static ushort Classify(int users, LoginServerConfiguration config)
    {
        if (config.UserCountDisable) return 4;
        if (users <= config.UserCountLow) return 0;
        if (users <= config.UserCountMedium) return 1;
        if (users <= config.UserCountHigh) return 2;
        return 3;
    }
}
