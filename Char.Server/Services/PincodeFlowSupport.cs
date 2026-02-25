using Core.Server.Packets.Out.HC;

namespace Char.Server.Services;

public enum PincodeState : ushort
{
    PassedOrDisabled = 0,
    Ask = 1,
    New = 2,
    MustChange = 3,
    NewV2 = 4,
    Illegal = 5,
    KssnError = 6,
    WindowButton = 7,
    Incorrect = 8
}

public static class PincodeFlowSupport
{
    private const int PinLength = 4;

    public static void SendState(CharSessionData session, PincodeState state)
    {
        session.EnqueuePacket(new HC_SECOND_PASSWD_LOGIN
        {
            Seed = (uint)Random.Shared.Next(0, ushort.MaxValue + 1),
            AccountId = (uint)(session.AccountId ?? 0),
            State = (ushort)state
        });
    }

    public static bool IsValidPinFormat(string pin)
    {
        var normalized = NormalizePin(pin);
        return normalized.Length == PinLength && normalized.All(char.IsDigit);
    }

    public static string NormalizePin(string pin)
    {
        return (pin ?? string.Empty).TrimEnd('\0').Trim();
    }

    public static bool IsAllowedPin(string pin, PincodeConfiguration config)
    {
        var normalized = NormalizePin(pin);
        if (!IsValidPinFormat(normalized))
        {
            return false;
        }

        if (!config.AllowRepeated && normalized.Distinct().Count() == 1)
        {
            return false;
        }

        if (!config.AllowSequential && (IsAscendingSequential(normalized) || IsDescendingSequential(normalized)))
        {
            return false;
        }

        return true;
    }

    private static bool IsAscendingSequential(string pin)
    {
        for (var i = 1; i < pin.Length; i++)
        {
            var prev = pin[i - 1] - '0';
            var expected = (prev + 1) % 10;
            if (pin[i] - '0' != expected)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDescendingSequential(string pin)
    {
        for (var i = 1; i < pin.Length; i++)
        {
            var prev = pin[i - 1] - '0';
            var expected = (prev + 9) % 10;
            if (pin[i] - '0' != expected)
            {
                return false;
            }
        }

        return true;
    }
}
