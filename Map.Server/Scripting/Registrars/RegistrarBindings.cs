using Jint;
using Jint.Native;
using Jint.Native.Object;
using Map.Server.Scripting.Records;

namespace Map.Server.Scripting.Registrars;

/// <summary>
/// Injects the five host-side <c>register*</c> functions into a Jint engine.
/// Called once per <see cref="ScriptHost"/> evaluation pass, after a fresh
/// engine is created and before <c>main.js</c> runs.
///
/// Every registrar accepts *varargs* — <c>registerNpc(a, b, c)</c> registers
/// three NPCs in one call. Spreading an array works too:
/// <c>registerNpc(...arrayOfNpcs)</c>. This lets authors write each NPC as
/// a pure <c>export const</c> in its own file and aggregate them in an
/// index that calls each registrar once.
///
/// Jint's <c>DelegateWrapper</c> checks each delegate parameter for
/// <c>ParamArrayAttribute</c> to decide whether to spread JS args into an
/// array or marshal a single JS value. C# only emits that attribute for
/// methods declared with <c>params</c> — lambdas don't carry it. So the
/// register* functions live as real instance methods on this dispatcher
/// class; Jint then sees the <c>params</c> and behaves correctly.
/// </summary>
internal sealed class RegistrarBindings
{
    private readonly INpcRegistry _registry;

    private RegistrarBindings(INpcRegistry registry) => _registry = registry;

    public static void Bind(Engine engine, INpcRegistry registry)
    {
        var binder = new RegistrarBindings(registry);
        engine.SetValue("registerNpc",         (Action<JsValue[]>)binder.registerNpc);
        engine.SetValue("registerFloatingNpc", (Action<JsValue[]>)binder.registerFloatingNpc);
        engine.SetValue("registerShop",        (Action<JsValue[]>)binder.registerShop);
        engine.SetValue("registerWarp",        (Action<JsValue[]>)binder.registerWarp);
        engine.SetValue("registerSpawn",       (Action<JsValue[]>)binder.registerSpawn);
    }

    // The lowercase names match the JS identifiers exactly; the params
    // modifier on each is what makes Jint treat them as variadic.

    // ReSharper disable InconsistentNaming
    public void registerNpc(params JsValue[] args)
    {
        foreach (var arg in args) RegisterNpc(arg, _registry);
    }

    public void registerFloatingNpc(params JsValue[] args)
    {
        foreach (var arg in args) RegisterFloatingNpc(arg, _registry);
    }

    public void registerShop(params JsValue[] args)
    {
        foreach (var arg in args) RegisterShop(arg, _registry);
    }

    public void registerWarp(params JsValue[] args)
    {
        foreach (var arg in args) RegisterWarp(arg, _registry);
    }

    public void registerSpawn(params JsValue[] args)
    {
        foreach (var arg in args) RegisterSpawn(arg, _registry);
    }
    // ReSharper restore InconsistentNaming

    private static void RegisterNpc(JsValue raw, INpcRegistry registry)
    {
        var obj = JsObjectReader.RequireObject(raw, "registerNpc");
        var name = JsObjectReader.RequireString(obj, "name", "registerNpc");
        var ctx = $"registerNpc('{name}')";

        var reg = new NpcRegistration
        {
            Map = JsObjectReader.RequireString(obj, "map", ctx),
            X = (short)JsObjectReader.RequireInt(obj, "x", ctx),
            Y = (short)JsObjectReader.RequireInt(obj, "y", ctx),
            Dir = (byte)JsObjectReader.OptionalInt(obj, "dir", 0),
            Sprite = JsObjectReader.RequireInt(obj, "sprite", ctx),
            Name = name,
            TriggerArea = ReadTriggerArea(obj),
            Hooks = ReadNpcHooks(obj, ctx),
        };
        registry.AddNpc(reg);
    }

    private static void RegisterFloatingNpc(JsValue raw, INpcRegistry registry)
    {
        var obj = JsObjectReader.RequireObject(raw, "registerFloatingNpc");
        var name = JsObjectReader.RequireString(obj, "name", "registerFloatingNpc");
        var ctx = $"registerFloatingNpc('{name}')";

        // World-position fields must NOT be present — floating means floating.
        foreach (var bad in new[] { "map", "x", "y", "sprite", "triggerArea" })
        {
            var v = obj.Get(bad);
            if (!v.IsUndefined() && !v.IsNull())
            {
                throw new ScriptRegistrationException(
                    $"{ctx}: floating NPCs have no world position; remove '{bad}'. " +
                    "Use registerNpc() if you want a placed NPC.");
            }
        }

        var reg = new FloatingNpcRegistration
        {
            Name = name,
            Hooks = ReadFloatingHooks(obj, ctx),
        };
        registry.AddFloatingNpc(reg);
    }

    private static void RegisterShop(JsValue raw, INpcRegistry registry)
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
        {
            costItem = JsObjectReader.RequireInt(obj, "costItem", ctx);
        }
        else if (kind == ShopKind.Point)
        {
            costVariable = JsObjectReader.RequireString(obj, "costVariable", ctx);
        }

        var items = ReadShopItems(obj, kind, ctx);

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
            Items = items,
        });
    }

    private static void RegisterWarp(JsValue raw, INpcRegistry registry)
    {
        var obj = JsObjectReader.RequireObject(raw, "registerWarp");
        var ctx = "registerWarp";

        var from = JsObjectReader.OptionalObject(obj, "from")
            ?? throw new ScriptRegistrationException($"{ctx}: missing required 'from' object.");
        var to = JsObjectReader.OptionalObject(obj, "to")
            ?? throw new ScriptRegistrationException($"{ctx}: missing required 'to' object.");
        var area = JsObjectReader.OptionalObject(obj, "area")
            ?? throw new ScriptRegistrationException($"{ctx}: missing required 'area' object.");

        registry.AddWarp(new WarpRegistration
        {
            FromMap = JsObjectReader.RequireString(from, "map", $"{ctx}.from"),
            FromX = (short)JsObjectReader.RequireInt(from, "x", $"{ctx}.from"),
            FromY = (short)JsObjectReader.RequireInt(from, "y", $"{ctx}.from"),
            AreaXs = (short)JsObjectReader.RequireInt(area, "xs", $"{ctx}.area"),
            AreaYs = (short)JsObjectReader.RequireInt(area, "ys", $"{ctx}.area"),
            ToMap = JsObjectReader.RequireString(to, "map", $"{ctx}.to"),
            ToX = (short)JsObjectReader.RequireInt(to, "x", $"{ctx}.to"),
            ToY = (short)JsObjectReader.RequireInt(to, "y", $"{ctx}.to"),
            Type = JsObjectReader.OptionalString(obj, "type") ?? "warp",
        });
    }

    private static void RegisterSpawn(JsValue raw, INpcRegistry registry)
    {
        var obj = JsObjectReader.RequireObject(raw, "registerSpawn");
        var ctx = "registerSpawn";

        (short, short, short, short)? area = null;
        if (JsObjectReader.OptionalObject(obj, "area") is { } a)
        {
            area = (
                (short)JsObjectReader.RequireInt(a, "x", $"{ctx}.area"),
                (short)JsObjectReader.RequireInt(a, "y", $"{ctx}.area"),
                (short)JsObjectReader.RequireInt(a, "xs", $"{ctx}.area"),
                (short)JsObjectReader.RequireInt(a, "ys", $"{ctx}.area"));
        }

        var respawnBase = 5_000;
        var respawnJitter = 2_000;
        if (JsObjectReader.OptionalObject(obj, "respawn") is { } r)
        {
            respawnBase = JsObjectReader.OptionalInt(r, "baseMs", 5_000);
            respawnJitter = JsObjectReader.OptionalInt(r, "jitterMs", 2_000);
        }

        registry.AddSpawn(new SpawnRegistration
        {
            Map = JsObjectReader.RequireString(obj, "map", ctx),
            Area = area,
            MobId = JsObjectReader.RequireInt(obj, "mobId", ctx),
            Amount = JsObjectReader.RequireInt(obj, "amount", ctx),
            RespawnBaseMs = respawnBase,
            RespawnJitterMs = respawnJitter,
            Boss = JsObjectReader.OptionalBool(obj, "boss", false),
            DisplayName = JsObjectReader.OptionalString(obj, "name"),
            OnDeathEvent = JsObjectReader.OptionalString(obj, "onDeath"),
            Size = JsObjectReader.OptionalInt(obj, "size", 0),
            Ai = JsObjectReader.OptionalInt(obj, "ai", 0),
        });
    }

    // ---- helpers ----

    private static (short Xs, short Ys)? ReadTriggerArea(ObjectInstance obj)
    {
        var area = JsObjectReader.OptionalObject(obj, "triggerArea");
        if (area == null) return null;
        return (
            (short)JsObjectReader.RequireInt(area, "xs", "triggerArea"),
            (short)JsObjectReader.RequireInt(area, "ys", "triggerArea"));
    }

    private static NpcHooks ReadNpcHooks(ObjectInstance obj, string ctx) => new(
        OnClick:    JsObjectReader.OptionalHandle(obj, "onClick",   ctx),
        OnTouch:    JsObjectReader.OptionalHandle(obj, "onTouch",   ctx),
        OnInit:     JsObjectReader.OptionalHandle(obj, "onInit",    ctx),
        OnTimer:    JsObjectReader.OptionalIntKeyedHandles(obj, "onTimer", ctx),
        OnClock:    null,  // onClock is floating-only
        OnPCLogin:  JsObjectReader.OptionalHandle(obj, "onPCLogin", ctx),
        OnPCDeath:  JsObjectReader.OptionalHandle(obj, "onPCDeath", ctx),
        OnPCKill:   JsObjectReader.OptionalHandle(obj, "onPCKill",  ctx),
        OnNPCKill:  JsObjectReader.OptionalHandle(obj, "onNPCKill", ctx));

    private static NpcHooks ReadFloatingHooks(ObjectInstance obj, string ctx) => new(
        OnClick:    null,
        OnTouch:    null,
        OnInit:     JsObjectReader.OptionalHandle(obj, "onInit",    ctx),
        OnTimer:    JsObjectReader.OptionalIntKeyedHandles(obj, "onTimer", ctx),
        OnClock:    JsObjectReader.OptionalStringKeyedHandles(obj, "onClock", ctx),
        OnPCLogin:  JsObjectReader.OptionalHandle(obj, "onPCLogin", ctx),
        OnPCDeath:  JsObjectReader.OptionalHandle(obj, "onPCDeath", ctx),
        OnPCKill:   null,
        OnNPCKill:  null);

    private static IReadOnlyList<ShopItem> ReadShopItems(ObjectInstance obj, ShopKind kind, string ctx)
    {
        var arr = obj.Get("items");
        if (arr is not ObjectInstance items || !items.IsArray())
        {
            throw new ScriptRegistrationException($"{ctx}: 'items' must be an array.");
        }
        var result = new List<ShopItem>();
        var len = (int)items.Get("length").AsNumber();
        for (var i = 0; i < len; i++)
        {
            var entry = items.Get(i.ToString());
            if (entry is not ObjectInstance item)
            {
                throw new ScriptRegistrationException(
                    $"{ctx}.items[{i}] must be an object literal.");
            }
            var itemCtx = $"{ctx}.items[{i}]";
            int? stock = null;
            if (kind == ShopKind.Market)
            {
                stock = JsObjectReader.RequireInt(item, "stock", itemCtx);
            }
            else if (!item.Get("stock").IsUndefined())
            {
                throw new ScriptRegistrationException(
                    $"{itemCtx}: 'stock' is only valid in kind='market' shops.");
            }
            int? discount = null;
            if (kind is ShopKind.Item or ShopKind.Point)
            {
                var d = item.Get("discount");
                if (!d.IsUndefined() && !d.IsNull())
                {
                    discount = (int)d.AsNumber();
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
