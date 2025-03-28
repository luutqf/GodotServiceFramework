using Godot;
using Godot.Collections;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Data;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Extensions;

public static class DataNodeExtensions
{
    public static void InitBindData(this Node @this, IBinding binding)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (@this is not IDataNode node) return;
        node.InitBindData(binding);
    }

    public static void InitBindData(this Node @this, Variant args)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (@this is not IDataNode node) return;

        if (args.Obj is IBinding ib)
        {
            node.InitBindData(ib);
            return;
        }

        try
        {
            var binding = node._Parse(args);
            Logger.Info(binding);
            if (binding is not null)
            {
                node.InitBindData(binding);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }

    public static void InitNone(this Node @this, Type type)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (@this is not IDataNode node) return;
        node.InitNone(type);
    }


    public static void DestroyBindData(this Node @this)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (@this is not IDataNode node) return;

        node.DestroyBind();
    }

    public static int GetBindId(this Node @this)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (@this is not IDataNode node) return -1;

        return node.GetBindId();
    }

    public static TR GetBind<TR>(this Node @this) where TR : IBinding
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        return @this is not IDataNode node ? throw new Exception("cannot getBind") : node.GetBind<TR>()!;
    }
}