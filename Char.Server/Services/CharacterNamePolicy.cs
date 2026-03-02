using Core.Server.Packets;
using Core.Database.Repositories.Api;

namespace Char.Server.Services;

internal static class CharacterNamePolicy
{
    private static readonly HashSet<char> TrimChars =
    [
        '\u00FF', // 255
        '\u00A0', // 160
        '\u001A',
        '\t',
        '\n',
        '\r',
        ' '
    ];

    internal static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var span = input.AsSpan();
        var index = 0;

        while (index < span.Length && TrimChars.Contains(span[index]))
        {
            index++;
        }

        if (index >= span.Length)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        while (index < span.Length)
        {
            var start = index;
            while (index < span.Length && !TrimChars.Contains(span[index]))
            {
                index++;
            }

            parts.Add(span[start..index].ToString());

            while (index < span.Length && TrimChars.Contains(span[index]))
            {
                index++;
            }
        }

        return string.Join(' ', parts);
    }

    internal static bool IsStructurallyValid(string name, CharServerConfiguration configuration)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (name.Length < configuration.Char.CharNameMinLength || name.Length > PacketConstants.NAME_LENGTH)
        {
            return false;
        }

        if (HasAsciiControlChars(name))
        {
            return false;
        }

        if (name.StartsWith('#'))
        {
            return false;
        }

        if (string.Equals(name, configuration.WispServerName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (configuration.Char.CharNameOption == 1)
        {
            for (var i = 0; i < name.Length; i++)
            {
                if (!configuration.Char.CharNameLetters.Contains(name[i]))
                {
                    return false;
                }
            }
        }
        else if (configuration.Char.CharNameOption == 2)
        {
            for (var i = 0; i < name.Length; i++)
            {
                if (configuration.Char.CharNameLetters.Contains(name[i]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static async Task<bool> NameExistsAsync(
        ICharacterRepository characterRepository,
        string name,
        CharServerConfiguration configuration)
    {
        if (configuration.Char.NameIgnoringCase)
        {
            // rAthena parity for name_ignoring_case: case-sensitive equality.
            return await characterRepository.NameExistsAsync(name);
        }

        // rAthena parity for default mode: case-insensitive collision.
        var allCharacters = await characterRepository.GetAllAsync();
        return allCharacters.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasAsciiControlChars(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c <= '\u001F' || c == '\u007F')
            {
                return true;
            }
        }

        return false;
    }
}
