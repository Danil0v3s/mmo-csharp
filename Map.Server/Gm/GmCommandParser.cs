namespace Map.Server.Gm;

/// <summary>
/// Splits a raw chat line into the at-command form. Mirrors rAthena's
/// <c>is_atcommand</c> / <c>atcommand_run</c> tokenizer just enough for
/// the commands we ship today.
/// </summary>
public static class GmCommandParser
{
    public const char AtSymbol = '@';
    public const char CharSymbol = '#';

    /// <summary>
    /// Parse a chat payload of the form <c>"&lt;name&gt; : &lt;message&gt;"</c>
    /// into a command (without the leading symbol) plus its argument list.
    /// Returns false for plain chat or malformed input.
    /// </summary>
    public static bool TryParse(string chatLine, out string commandName, out IReadOnlyList<string> args)
    {
        commandName = string.Empty;
        args = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(chatLine)) return false;

        // rAthena packets format text as "<name> : <message>"; isolate the
        // message half. Some clients send just the message, so the separator
        // is optional.
        var sepIdx = chatLine.IndexOf(" : ", StringComparison.Ordinal);
        var message = (sepIdx >= 0 ? chatLine[(sepIdx + 3)..] : chatLine).Trim();
        if (message.Length < 2) return false;
        if (message[0] != AtSymbol && message[0] != CharSymbol) return false;

        // Tokenize on whitespace; first token (after the symbol) is the name.
        var rest = message[1..].Trim();
        if (rest.Length == 0) return false;
        var tokens = rest.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;

        commandName = tokens[0].ToLowerInvariant();
        args = tokens.Length > 1 ? tokens[1..] : Array.Empty<string>();
        return true;
    }
}
