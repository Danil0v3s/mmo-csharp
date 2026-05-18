using Microsoft.ClearScript;
using Map.Server.Scripting.Records;

namespace Map.Server.Scripting.Registrars;

/// <summary>
/// Reusable accessors for pulling fields off a ClearScript <see cref="ScriptObject"/>.
/// Every registrar uses these.
///
/// All <c>RequireXxx</c> overloads throw <see cref="ScriptRegistrationException"/>
/// when the field is missing, wrong type, or out of range; all <c>OptionalXxx</c>
/// overloads return a fallback when the field is missing.
/// </summary>
internal static class JsObjectReader
{
    public static ScriptObject RequireObject(object? value, string registrarName)
    {
        if (value is ScriptObject obj) return obj;
        throw new ScriptRegistrationException(
            $"{registrarName}() requires an object literal; got {DescribeType(value)}.");
    }

    public static string RequireString(ScriptObject obj, string field, string context)
    {
        var v = obj.GetProperty(field);
        if (IsUndefinedOrNull(v))
        {
            throw new ScriptRegistrationException($"{context}: required field '{field}' is missing.");
        }
        if (v is not string s)
        {
            throw new ScriptRegistrationException(
                $"{context}: field '{field}' must be a string; got {DescribeType(v)}.");
        }
        return s;
    }

    public static string? OptionalString(ScriptObject obj, string field)
    {
        var v = obj.GetProperty(field);
        if (IsUndefinedOrNull(v)) return null;
        if (v is not string s)
        {
            throw new ScriptRegistrationException(
                $"Field '{field}' must be a string when set; got {DescribeType(v)}.");
        }
        return s;
    }

    public static int RequireInt(ScriptObject obj, string field, string context)
    {
        var v = obj.GetProperty(field);
        if (IsUndefinedOrNull(v))
        {
            throw new ScriptRegistrationException($"{context}: required field '{field}' is missing.");
        }
        return CoerceInt(v, field, context);
    }

    public static int OptionalInt(ScriptObject obj, string field, int fallback)
    {
        var v = obj.GetProperty(field);
        if (IsUndefinedOrNull(v)) return fallback;
        return CoerceInt(v, field, $"Field '{field}'");
    }

    public static bool OptionalBool(ScriptObject obj, string field, bool fallback)
    {
        var v = obj.GetProperty(field);
        if (IsUndefinedOrNull(v)) return fallback;
        if (v is not bool b)
        {
            throw new ScriptRegistrationException(
                $"Field '{field}' must be a boolean when set; got {DescribeType(v)}.");
        }
        return b;
    }

    public static ScriptObject? OptionalObject(ScriptObject obj, string field)
    {
        var v = obj.GetProperty(field);
        if (IsUndefinedOrNull(v)) return null;
        if (v is not ScriptObject child)
        {
            throw new ScriptRegistrationException(
                $"Field '{field}' must be an object when set; got {DescribeType(v)}.");
        }
        return child;
    }

    /// <summary>
    /// Capture a hook property as a <see cref="ScriptHandle"/>. Validates
    /// that the value is invocable; throws with a useful pointer if the
    /// author passed e.g. a string instead of a function.
    /// </summary>
    public static ScriptHandle? OptionalHandle(ScriptObject obj, string field, string sourceContext)
    {
        var v = obj.GetProperty(field);
        if (IsUndefinedOrNull(v)) return null;
        if (v is not ScriptObject fn)
        {
            throw new ScriptRegistrationException(
                $"Hook '{field}' in {sourceContext} must be a function; got {DescribeType(v)}. " +
                "Make sure you wrote `async onClick(ctx) { ... }` or `onClick: async (ctx) => ...`.");
        }
        // ClearScript exposes functions as ScriptObjects with a callable
        // semantic; the only way to truly verify "is this callable" is to
        // try to invoke it, which we don't want at registration time. We
        // accept any ScriptObject and let the dispatcher's invocation
        // surface bad inputs.
        return new ScriptHandle(fn, $"{sourceContext}.{field}");
    }

    /// <summary>
    /// Read a record-shaped optional field (<c>onTimer: { 5000: fn, 30000: fn }</c>)
    /// where keys are integers and values are functions.
    /// </summary>
    public static IReadOnlyDictionary<int, ScriptHandle>? OptionalIntKeyedHandles(
        ScriptObject obj, string field, string sourceContext)
    {
        var bag = OptionalObject(obj, field);
        if (bag == null) return null;
        var result = new Dictionary<int, ScriptHandle>();
        foreach (var key in bag.PropertyNames)
        {
            if (!int.TryParse(key, out var keyInt))
            {
                throw new ScriptRegistrationException(
                    $"{sourceContext}.{field}: key '{key}' must be an integer (milliseconds).");
            }
            var handle = OptionalHandle(bag, key, $"{sourceContext}.{field}[{key}]");
            if (handle != null) result[keyInt] = handle;
        }
        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// Record-shaped optional field with string keys (<c>onClock: { "0000": fn }</c>).
    /// </summary>
    public static IReadOnlyDictionary<string, ScriptHandle>? OptionalStringKeyedHandles(
        ScriptObject obj, string field, string sourceContext)
    {
        var bag = OptionalObject(obj, field);
        if (bag == null) return null;
        var result = new Dictionary<string, ScriptHandle>(StringComparer.Ordinal);
        foreach (var key in bag.PropertyNames)
        {
            var handle = OptionalHandle(bag, key, $"{sourceContext}.{field}[\"{key}\"]");
            if (handle != null) result[key] = handle;
        }
        return result.Count == 0 ? null : result;
    }

    // ---- helpers ----

    private static bool IsUndefinedOrNull(object? v) =>
        v is null || v is Undefined;

    private static int CoerceInt(object value, string field, string context)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            float f => (int)f,
            _ => throw new ScriptRegistrationException(
                $"{context}: field '{field}' must be a number; got {DescribeType(value)}."),
        };
    }

    private static string DescribeType(object? v) => v switch
    {
        null => "null",
        Undefined => "undefined",
        string => "string",
        bool => "boolean",
        int or long or double or float => "number",
        ScriptObject so when so.GetType().Name.Contains("Function") => "function",
        ScriptObject => "object",
        _ => v.GetType().Name,
    };
}
