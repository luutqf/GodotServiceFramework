using System.Diagnostics;
using Godot;
using GodotServiceFramework.Context.Controller;
using GodotServiceFramework.Context.Thread;
using GodotServiceFramework.GTask;
using GodotServiceFramework.GTask.Entity;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Context.Session;

public static class SessionExtensions
{
    public static Node? GetSessionNode(this Node node)
    {
        var instanceFromId = GodotObject.InstanceFromId(node.GetSessionId(out _));
        if (instanceFromId is Node sessionNode)
        {
            return sessionNode;
        }

        return null;
    }

    public static void SetSessionId(this object obj, ulong sessionId)
    {
        var activity = new Activity(sessionId.ToString()).Start();
        activity.AddTag("session", sessionId);
    }

    /// <summary>
    /// 获取节点的sessionId， 我们以节点树为核心，找到被标记为会话的父节点。
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static ulong GetSessionId(this object obj, out string source)
    {
        if (obj is Node node && SessionManager.SessionIdMap.TryGetValue(node.GetInstanceId(), out var ul))
        {
            source = "node";
            return ul;
        }


        switch (obj)
        {
            case GameTaskFlowEntity gameTaskFlowEntity:
                if (gameTaskFlowEntity.SessionId != 0)
                {
                    source = "entity";
                    return gameTaskFlowEntity.SessionId;
                }

                break;
            case GameTaskWorkflow taskWorkflow:
                if (taskWorkflow.SessionId != 0)
                {
                    source = "flow";
                    return taskWorkflow.SessionId;
                }

                break;
            case GameTask task:
                if (task.GameTaskWorkflow.SessionId != 0)
                {
                    source = "task";
                    return task.GameTaskWorkflow.SessionId;
                }

                break;
        }

        if (Activity.Current != null)
        {
            var session = Activity.Current.GetTagItem("session");
            if (session != null)
            {
                source = "current";
                return (ulong)session;
            }
        }

        source = "default";
        return SessionManager.MainSessionIds.Count == 0 ? 0 : SessionManager.MainSessionIds[0];
    }


    public static GodotResult<TR?> InvokeController<TR>(this Node @this, string controller, string resource,
        string method = "GET", params object?[]? args)
    {
        if (Activity.Current != null)
        {
            Activity.Current.Dispose();
            Activity.Current = null;
        }

        using var activity = new Activity(@this.GetSessionId(out _).ToString()).Start();
        activity.AddTag("session", @this.GetSessionId(out _));
        return Controllers.Invoke<TR?>(controller, resource, method, args);
    }

    public static void InvokeController(this Node @this, string controller, string resource,
        string method = "GET", params object?[]? args)
    {

        if (Activity.Current != null)
        {
            Activity.Current.Dispose();
            Activity.Current = null;
        }

        using var activity = new Activity(@this.GetSessionId(out _).ToString()).Start();
        activity.AddTag("session", @this.GetSessionId(out _));
        Controllers.Invoke(controller, resource, method, args);
    }

    public static void InvokeController(this Node @this, string alias, params object?[]? args)
    {
        if (Activity.Current != null)
        {
            Activity.Current.Dispose();
            Activity.Current = null;
        }

        using var activity = new Activity(@this.GetSessionId(out _).ToString()).Start();
        activity.AddTag("session", @this.GetSessionId(out _));
        Controllers.Invoke(alias, args);
    }

    public static Error EmitSessionSignal(this Node @this, string signal, params Variant[] args)
    {
        var sessionNode = @this.GetSessionNode();
        if (sessionNode != null)
        {
            var combinedArgs = new[] { Variant.From(signal) }.Concat(args).ToArray();
            sessionNode.CallDeferred(GodotObject.MethodName.EmitSignal, combinedArgs);
        }

        return Error.Ok;
    }

    /// <summary>
    /// 简单的封装,直接发送SuperChat信号
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static Error EmitSessionSignal(this object obj, params Variant[] args)
    {
        return obj.EmitSessionSignal("SuperChat", args);
    }

    /// <summary>
    /// 简单的封装,直接发送SuperChat信号
    /// </summary>
    /// <param name="node"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static Error EmitSessionSignal(this Node node, params Variant[] args)
    {
        return node.EmitSessionSignal("SuperChat", args);
    }


    /// <summary>
    /// 向当前会话的父节点发送信号
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="signal"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static Error EmitSessionSignal(this object obj, string signal, params Variant[] args)
    {
        var sessionId = obj.GetSessionId(out _);

        var godotObject = GodotObject.InstanceFromId(sessionId);
        if (godotObject is not Node sessionNode) return Error.FileNotFound;


        var combinedArgs = new[] { Variant.From(signal) }.Concat(args).ToArray();
        sessionNode.CallDeferred(GodotObject.MethodName.EmitSignal, combinedArgs);

        return Error.Ok;
    }
}