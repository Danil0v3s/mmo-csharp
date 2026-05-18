using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Map.Server.Scripting.Records;

namespace Map.Server.Scripting.Registrars;

/// <summary>
/// Injects the host-side <c>register*</c> functions into a V8 engine.
/// Called once per <see cref="ScriptHost"/> evaluation pass, after a fresh
/// engine is created and before <c>main.js</c> runs.
///
/// Warps, mob spawns, and map flags live in DB catalogs (see commit
/// 96443ef); only NPCs / floating NPCs / shops are script-driven.
///
/// Every registrar accepts *varargs* — <c>registerNpc(a, b, c)</c> registers
/// three NPCs in one call. ClearScript binds JS positional args to a C#
/// method's <c>params object[]</c> parameter automatically.
/// </summary>
public sealed class RegistrarBindings
{
    private readonly INpcRegistry _registry;

    private RegistrarBindings(INpcRegistry registry) => _registry = registry;

    public static void Bind(V8ScriptEngine engine, INpcRegistry registry)
    {
        var binder = new RegistrarBindings(registry);
        // AddHostObject exposes the binder under each of the five names. Because
        // ClearScript invokes methods by name, we add the same object under all
        // five aliases — JS sees a callable global at each registrar name that
        // dispatches to the corresponding method.
        //
        // To get TRUE bare globals (not `binder.registerNpc(...)` but just
        // `registerNpc(...)`), we ExecuteCommand to install each name as a
        // function reference on the global object.
        engine.AddHostObject("__registrarBinder", binder);
        engine.Execute(@"
            globalThis.registerNpc         = (...args) => __registrarBinder.registerNpc(...args);
            globalThis.registerFloatingNpc = (...args) => __registrarBinder.registerFloatingNpc(...args);
            globalThis.registerShop        = (...args) => __registrarBinder.registerShop(...args);
        ");
    }

    // ReSharper disable InconsistentNaming
    // Each method takes an `object[]` (the JS spread array). Lowercase names
    // match the JS-side dispatch shim above.

    public void registerNpc(params object[] args)
    {
        foreach (var arg in args) RegisterNpc(arg, _registry);
    }

    public void registerFloatingNpc(params object[] args)
    {
        foreach (var arg in args) RegisterFloatingNpc(arg, _registry);
    }

    public void registerShop(params object[] args)
    {
        foreach (var arg in args) RegisterShop(arg, _registry);
    }
    // ReSharper restore InconsistentNaming

    // ---- per-item registrars ----

    private static void RegisterNpc(object raw, INpcRegistry registry)
    {
        var obj = JsObjectReader.RequireObject(raw, "registerNpc");
        var name = JsObjectReader.RequireString(obj, "name", "registerNpc");
        var ctx = $"registerNpc('{name}')";

        registry.AddNpc(new NpcRegistration
        {
            Map = JsObjectReader.RequireString(obj, "map", ctx),
            X = (short)JsObjectReader.RequireInt(obj, "x", ctx),
            Y = (short)JsObjectReader.RequireInt(obj, "y", ctx),
            Dir = (byte)JsObjectReader.OptionalInt(obj, "dir", 0),
            Sprite = JsObjectReader.RequireInt(obj, "sprite", ctx),
            Name = name,
            TriggerArea = ReadTriggerArea(obj),
            Hooks = ReadNpcHooks(obj, ctx),
        });
    }

    private static void RegisterFloatingNpc(object raw, INpcRegistry registry)
    {
        var obj = JsObjectReader.RequireObject(raw, "registerFloatingNpc");
        var name = JsObjectReader.RequireString(obj, "name", "registerFloatingNpc");
        var ctx = $"registerFloatingNpc('{name}')";

        foreach (var bad in new[] { "map", "x", "y", "sprite", "triggerArea" })
        {
            var v = obj.GetProperty(bad);
            if (v is not null && v is not Undefined)
            {
                throw new ScriptRegistrationException(
                    $"{ctx}: floating NPCs have no world position; remove '{bad}'. " +
                    "Use registerNpc() if you want a placed NPC.");
            }
        }

        registry.AddFloatingNpc(new FloatingNpcRegistration
        {
            Name = name,
            Hooks = ReadFloatingHooks(obj, ctx),
        });
    }

    private static void RegisterShop(object raw, INpcRegistry registry)
    {
        var obj = JsObjectReader.RequireObject(raw, "registerShop");
        var name = JsObjectReader.RequireString(obj, "name", "registerShop");
        var ctx = $"registerShop('{name}')";

        var kindStr = JsObjectReader.RequireString(obj, "kind", ctx);
        var kind = kindStr switch
        {
            "shop" => ShopKind.Shop,
            "cash" => ShopKind.Cash,
            "item" => ShopKind.Item,
            "point" => ShopKind.Point,
            "market" => ShopKind.Market,
            _ => throw new ScriptRegistrationException(
                $"{ctx}: kind must be one of 'shop', 'cash', 'item', 'point', 'market'; got '{kindStr}'."),
        };

        int? costItem = null;
        string? costVariable = null;
        if (kind == ShopKind.Item)
            costItem = JsObjectReader.RequireInt(obj, "costItem", ctx);
        else if (kind == ShopKind.Point)
            costVariable = JsObjectReader.RequireString(obj, "costVariable", ctx);

        registry.AddShop(new ShopRegistration
        {
            Kind = kind,
            Map = JsObjectReader.RequireString(obj, "map", ctx),
            X = (short)JsObjectReader.RequireInt(obj, "x", ctx),
            Y = (short)JsObjectReader.RequireInt(obj, "y", ctx),
            Dir = (byte)JsObjectReader.OptionalInt(obj, "dir", 0),
            Sprite = JsObjectReader.RequireInt(obj, "sprite", ctx),
            Name = name,
            CostItem = costItem,
            CostVariable = costVariable,
            Items = ReadShopItems(obj, kind, ctx),
        });
    }

    // ---- helpers ----

    private static (short Xs, short Ys)? ReadTriggerArea(ScriptObject obj)
    {
        var area = JsObjectReader.OptionalObject(obj, "triggerArea");
        if (area == null) return null;
        return (
            (short)JsObjectReader.RequireInt(area, "xs", "triggerArea"),
            (short)JsObjectReader.RequireInt(area, "ys", "triggerArea"));
    }

    private static NpcHooks ReadNpcHooks(ScriptObject obj, string ctx) => new(
        OnClick:    JsObjectReader.OptionalHandle(obj, "onClick",   ctx),
        OnTouch:    JsObjectReader.OptionalHandle(obj, "onTouch",   ctx),
        OnInit:     JsObjectReader.OptionalHandle(obj, "onInit",    ctx),
        OnTimer:    JsObjectReader.OptionalIntKeyedHandles(obj, "onTimer", ctx),
        OnClock:    null,
        OnPCLogin:  JsObjectReader.OptionalHandle(obj, "onPCLogin", ctx),
        OnPCDeath:  JsObjectReader.OptionalHandle(obj, "onPCDeath", ctx),
        OnPCKill:   JsObjectReader.OptionalHandle(obj, "onPCKill",  ctx),
        OnNPCKill:  JsObjectReader.OptionalHandle(obj, "onNPCKill", ctx));

    private static NpcHooks ReadFloatingHooks(ScriptObject obj, string ctx) => new(
        OnClick:    null,
        OnTouch:    null,
        OnInit:     JsObjectReader.OptionalHandle(obj, "onInit",    ctx),
        OnTimer:    JsObjectReader.OptionalIntKeyedHandles(obj, "onTimer", ctx),
        OnClock:    JsObjectReader.OptionalStringKeyedHandles(obj, "onClock", ctx),
        OnPCLogin:  JsObjectReader.OptionalHandle(obj, "onPCLogin", ctx),
        OnPCDeath:  JsObjectReader.OptionalHandle(obj, "onPCDeath", ctx),
        OnPCKill:   null,
        OnNPCKill:  null);

    private static IReadOnlyList<ShopItem> ReadShopItems(ScriptObject obj, ShopKind kind, string ctx)
    {
        var arr = obj.GetProperty("items");
        if (arr is not ScriptObject items)
        {
            throw new ScriptRegistrationException($"{ctx}: 'items' must be an array.");
        }
        var lenProp = items.GetProperty("length");
        if (lenProp is not int and not double)
        {
            throw new ScriptRegistrationException($"{ctx}: 'items' must be an array.");
        }
        var len = Convert.ToInt32(lenProp);
        var result = new List<ShopItem>();
        for (var i = 0; i < len; i++)
        {
            var entry = items.GetProperty(i);
            if (entry is not ScriptObject item)
            {
                throw new ScriptRegistrationException($"{ctx}.items[{i}] must be an object literal.");
            }
            var itemCtx = $"{ctx}.items[{i}]";
            int? stock = null;
            if (kind == ShopKind.Market)
            {
                stock = JsObjectReader.RequireInt(item, "stock", itemCtx);
            }
            else
            {
                var stockProp = item.GetProperty("stock");
                if (stockProp is not null && stockProp is not Undefined)
                {
                    throw new ScriptRegistrationException(
                        $"{itemCtx}: 'stock' is only valid in kind='market' shops.");
                }
            }
            int? discount = null;
            if (kind is ShopKind.Item or ShopKind.Point)
            {
                var d = item.GetProperty("discount");
                if (d is not null && d is not Undefined)
                {
                    discount = Convert.ToInt32(d);
                }
            }
            result.Add(new ShopItem(
                ItemId: JsObjectReader.RequireInt(item, "itemId", itemCtx),
                Price: JsObjectReader.RequireInt(item, "price", itemCtx),
                Discount: discount,
                Stock: stock));
        }
        return result;
    }
}
