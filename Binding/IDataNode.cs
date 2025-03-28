using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Data;

public interface IDataNode
{
    static readonly ConcurrentDictionary<ulong, (Type T, int I)> Dict = [];

    static readonly ConcurrentDictionary<ulong, (FieldInfo info, string name)[]> FieldInfosMap = [];


    //标记是否是单例场景🤔,暂时放在这里
    public bool IsSingleScene => true;


    public static void Binding(IBinding data, DataModifyType type, string? propertyKey = null,
        object? propertyValue = null)
    {
        DataStore.Set(data);
        //TODO 这里暂时不考虑数据源的问题,id不一样就视为不一样.
        foreach (var (instanceId, (t, i)) in Dict)
        {
            //TODO 这里应该改成异步执行,再说
            if (data.GetType() != t) continue;

            if (GodotObject.InstanceFromId(instanceId) is not Node n || !n.IsInsideTree())
            {
                Dict.Remove(instanceId, out _);
                continue;
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (n is not IDataNode node) continue;

            switch (type)
            {
                case DataModifyType.Insert:
                    node.OnDataInsert0(n, data);
                    break;
                //对于数据源侧数据绑定, 只有在数据更新时才会更新node; 插入不管, 插入需要node自行实现; 删除也需要node自行解决
                case DataModifyType.Update:
                    //当数据ID一致的时候,才能触发更新, 其他两个情形无法限定, 可以由node自行解决.
                    if (data.Id == i)
                        node.OnDataUpdate0(n, data);
                    break;
                case DataModifyType.Delete:
                    //那就要求删除的时候, 让节点自己解决吧
                    if (data.Id == i)
                        node.OnDataDelete0(n, data);
                    break;
                case DataModifyType.Property:
                    if (data.Id == i)
                    {
                        node.OnDataProperty(data, propertyKey!, propertyValue);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    private void OnDataInsert0(Node node, IBinding data)
    {
        OnDataInsert(data);
    }

    /// <summary>
    /// 插入一般不会影响其他同级节点, 而是父级
    /// </summary>
    /// <param name="data"></param>
    public void OnDataInsert(IBinding data)
    {
    }


    private void OnDataUpdate0(Node node, IBinding data)
    {
        foreach (var (info, name) in FieldInfosMap[node.GetInstanceId()])
        {
            if (info.GetValue(node) is not Node field) continue;

            var propertyInfo = data.GetType().GetProperty(name);
            if (propertyInfo != null)
            {
                field.SetBindValue(propertyInfo.GetValue(data));
            }
        }

        OnDataUpdate(data);
    }

    public void OnDataUpdate(IBinding data)
    {
    }

    private void OnDataDelete0(Node node, IBinding data)
    {
        OnDataDelete(data);
    }

    public void OnDataDelete(IBinding _)
    {
    }

    /// <summary>
    /// 这里也视为更新, 但区分实体持久化的更新
    /// </summary>
    public void OnDataProperty(IBinding binding, string key, object? value)
    {
    }

    public void InitNone(Type type)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (this is not Node node) return;

        Dict[node.GetInstanceId()] = (type, 0);
    }

    public IBinding? _Parse(Variant args)
    {
        return null;
    }

    /// <summary>
    /// 
    /// 
    /// 初始化时, 还得把所有的标记绑定的子节点全部connect对应的信号, 用来双向数据绑定
    /// 
    /// 绑定的子节点必须是Field,因为这个比较好用.
    /// </summary>
    /// <param name="binding"></param>
    public void InitBindData(IBinding binding)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (this is not Node node) return;

        if (!node.IsNodeReady())
        {
            throw new Exception("节点实例未就绪,不允许初始化数据节点");
        }

        Dict[node.GetInstanceId()] = (binding.GetType(), binding.Id);


        //TODO 这里单向绑定吧还是
        var fieldInfos = GetType().GetRuntimeFields()
            .Where(info => info.GetCustomAttributes().Any(attr => attr is BindingAttribute))
            .Select(info => (info, info.GetCustomAttribute<BindingAttribute>()!.Name))
            .ToArray();
        FieldInfosMap[node.GetInstanceId()] = fieldInfos;


        DataStore.Set(binding);

        OnInitData(binding);
    }


    protected void OnInitData(IBinding binding)
    {
    }


    public void DestroyBind()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (this is not Node node) return;
        Dict.Remove(node.GetInstanceId(), out var value);
        Logger.Debug($"remove dataNode {node.GetInstanceId()}-> {value.T} id: {value.I}");
    }

    public int GetBindId()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (this is not Node node) return -1;

        if (Dict.TryGetValue(node.GetInstanceId(), out var value))
        {
            return value.I;
        }

        return -1;
    }

    public TR? GetBind<TR>() where TR : IBinding
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (this is not Node node) return default;

        return Dict.TryGetValue(node.GetInstanceId(), out var value) ? DataStore.Get<TR>(value.I) : default;
    }
}