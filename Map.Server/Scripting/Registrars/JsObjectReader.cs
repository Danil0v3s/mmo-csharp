using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Map.Server.Scripting.Records;

namespace Map.Server.Scripting.Registrars;

/// <summary>
/// Reusable accessors for pulling fields off a Jint <see cref="ObjectInstance"/>
/// with sensible error messages. Every registrar uses these.
///
/// All <c>RequireXxx</c> overloads throw <see cref="ScriptRegistrationException"/>
/// when the field is missing, wrong type, or out of range; all <c>OptionalXxx</c>
/// overloads return a fallback when the field is missing.
/// </summary>
internal static class JsObjectReader
{
    public static ObjectInstance RequireObject(JsValue value, string registrarName)
    {
        if (value is ObjectInstance obj) return obj;
        throw new ScriptRegistrationException(
            $"{registrarName}() requires an object literal; got {value.Type}.");
    }

    public static string RequireString(ObjectInstance obj, string field, string context)
    {
        var v = obj.Get(field);
        if (v.IsUndefined() || v.IsNull())
        {
            throw new ScriptRegistrationException($"{context}: required field '{field}' is missing.");
        }
        if (!v.IsString())
        {
            throw new ScriptRegistrationException(
                $"{context}: field '{field}' must be a string; got {v.Type}.");
        }
        return v.AsString();
    }

    public static string? OptionalString(ObjectInstance obj, string field)
    {
        var v = obj.Get(field);
        if (v.IsUndefined() || v.IsNull()) return null;
        if (!v.IsString())
        {
            throw new ScriptRegistrationException(
                $"Field '{field}' must be a string when set; got {v.Type}.");
        }
        return v.AsString();
    }

    public static int RequireInt(ObjectInstance obj, string field, string context)
    {
        var v = obj.Get(field);
        if (v.IsUndefined() || v.IsNull())
        {
            throw new ScriptRegistrationException($"{context}: required field '{field}' is missing.");
        }
        if (!v.IsNumber())
        {
            throw new ScriptRegistrationException(
                $"{context}: field '{field}' must be a number; got {v.Type}.");
        }
        return (int)v.AsNumber();
    }

    public static int OptionalInt(ObjectInstance obj, string field, int fallback)
    {
        var v = obj.Get(field);
        if (v.IsUndefined() || v.IsNull()) return fallback;
        if (!v.IsNumber())
        {
            throw new ScriptRegistrationException(
                $"Field '{field}' must be a number when set; got {v.Type}.");
        }
        return (int)v.AsNumber();
    }

    public static bool OptionalBool(ObjectInstance obj, string field, bool fallback)
    {
        var v = obj.Get(field);
        if (v.IsUndefined() || v.IsNull()) return fallback;
        if (!v.IsBoolean())
        {
            throw new ScriptRegistrationException(
                $"Field '{field}' must be a boolean when set; got {v.Type}.");
        }
        return v.AsBoolean();
    }

    public static ObjectInstance? OptionalObject(ObjectInstance obj, string field)
    {
        var v = obj.Get(field);
        if (v.IsUndefined() || v.IsNull()) return null;
        if (v is not ObjectInstance child)
        {
            throw new ScriptRegistrationException(
                $"Field '{field}' must be an object when set; got {v.Type}.");
        }
        return child;
    }

    public static ScriptHandle? OptionalHandle(ObjectInstance obj, string field, string sourceContext)
    {
        var v = obj.Get(field);
        if (v.IsUndefined() || v.IsNull()) return null;
        if (v is not Jint.Native.Function.Function)
        {
            throw new ScriptRegistrationException(
                $"Hook '{field}' in {sourceContext} must be a function; got {v.Type}. " +
                "Make sure you wrote `onClick(ctx) { ... }` or `onClick: async (ctx) => ...` — not a string or constant.");
        }
        return new ScriptHandle(v, $"{sourceContext}.{field}");
    }

    /// <summary>
    /// Read a record-shaped optional field (<c>onTimer: { 5000: fn, 30000: fn }</c>)
    /// where keys are integers and values are functions.
    /// </summary>
    public static IReadOnlyDictionary<int, ScriptHandle>? OptionalIntKeyedHandles(
        ObjectInstance obj, string field, string sourceContext)
    {
        var bag = OptionalObject(obj, field);
        if (bag == null) return null;
        var result = new Dictionary<int, ScriptHandle>();
        foreach (var key in bag.GetOwnPropertyKeys(Types.String))
        {
            var keyStr = key.AsString();
            if (!int.TryParse(keyStr, out var keyInt))
            {
                throw new ScriptRegistrationException(
                    $"{sourceContext}.{field}: key '{keyStr}' must be an integer (milliseconds).");
            }
            var handle = OptionalHandle(bag, keyStr, $"{sourceContext}.{field}[{keyStr}]");
            if (handle != null) result[keyInt] = handle;
        }
        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// Record-shaped optional field with string keys (<c>onClock: { "0000": fn }</c>).
    /// </summary>
    public static IReadOnlyDictionary<string, ScriptHandle>? OptionalStringKeyedHandles(
        ObjectInstance obj, string field, string sourceContext)
    {
        var bag = OptionalObject(obj, field);
        if (bag == null) return null;
        var result = new Dictionary<string, ScriptHandle>(StringComparer.Ordinal);
        foreach (var key in bag.GetOwnPropertyKeys(Types.String))
        {
            var keyStr = key.AsString();
            var handle = OptionalHandle(bag, keyStr, $"{sourceContext}.{field}[\"{keyStr}\"]");
            if (handle != null) result[keyStr] = handle;
        }
        return result.Count == 0 ? null : result;
    }
}
