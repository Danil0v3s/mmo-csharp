using Core.Server.Packets.Out.HC;

namespace Char.Server.Services;

/// <summary>
/// rAthena `pincode_state` (char.hpp). The C# names match the value semantics rAthena
/// actually emits — `NotSet` (2) is declared but never sent in rAthena; `New` (4) is the
/// "force user to set a new pincode" state used both at start-of-flow when
/// pincode_force is true and on CH_REQ_PINCODE_WINDOW when no pincode exists.
/// </summary>
public enum PincodeState : ushort
{
    PassedOrDisabled = 0,    // PINCODE_OK / PINCODE_PASSED
    Ask = 1,                 // PINCODE_ASK — enter existing pincode
    NotSet = 2,              // PINCODE_NOTSET — declared in rAthena but never emitted
    MustChange = 3,          // PINCODE_EXPIRED — change due to elapsed pincode_changetime
    New = 4,                 // PINCODE_NEW — set a new pincode (force or window-request)
    Illegal = 5,             // PINCODE_ILLEGAL
    KssnError = 6,           // PINCODE_KSSN (unsupported)
    WindowButton = 7,        // PINCODE_PASSED on legacy packet versions
    Incorrect = 8            // PINCODE_WRONG
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

    /// <summary>
    /// Computes the pincode state for the start-of-flow handshake. Mirrors rAthena
    /// `chlogif_pincode_start` (char_logif.cpp): disabled → PassedOrDisabled; no
    /// pincode + force → New; no pincode + !force → PassedOrDisabled; has pincode +
    /// expired → MustChange; has pincode + verified → PassedOrDisabled; otherwise Ask.
    /// </summary>
    public static PincodeState ComputeStartState(CharSessionData session, PincodeConfiguration config)
    {
        if (!config.Enabled)
        {
            return PincodeState.PassedOrDisabled;
        }

        if (string.IsNullOrEmpty(session.Pincode))
        {
            return config.Force ? PincodeState.New : PincodeState.PassedOrDisabled;
        }

        if (config.ChangeTime > 0)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (session.PincodeChangeUnixTime + config.ChangeTime <= now)
            {
                return PincodeState.MustChange;
            }
        }

        return session.PincodeVerified ? PincodeState.PassedOrDisabled : PincodeState.Ask;
    }

    /// <summary>
    /// Computes the state for an explicit CH_REQ_PINCODE_WINDOW request. Mirrors rAthena
    /// `chclif_parse_reqpincode_window` (char_clif.cpp:160-164): no pincode → New, else Ask.
    /// Note this differs from ComputeStartState in that it ignores pincode_force and
    /// expiration — the user opened the window explicitly.
    /// </summary>
    public static PincodeState ComputeWindowState(CharSessionData session)
        => string.IsNullOrEmpty(session.Pincode) ? PincodeState.New : PincodeState.Ask;

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
