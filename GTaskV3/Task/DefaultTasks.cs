using Godot;
using Godot.Collections;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTaskV3.Util;
using GodotServiceFramework.Util;
using Newtonsoft.Json.Linq;
using SigmusV2.Script.service;
using SigmusV3.Script;

namespace GodotServiceFramework.GTaskV3;

/// <summary>
/// task的实现全部以Func的形式提供
/// </summary>
[GTaskFunc]
public static class DefaultTasks
{
    [GTaskFunc("RpcContextTask")] public static Func<GTaskModel, Task<int>> RpcContextTaskFunc = async @this =>
    {
        @this.Context.TaskAction += RpcTaskAction;
        @this.OnCompleted += () => { @this.Context.TaskAction -= RpcTaskAction; };
        return 101;
    };


    private static void RpcTaskAction(TaskEvent @event, GTaskModel? model, GTaskSet? set, string msg)
    {
        if (model == null) return;
        var dictionary = @event switch
        {
            TaskEvent.Progress => new Dictionary
            {
                ["resource"] = "progress",
                ["title"] = model!.Name,
                ["taskId"] = string.Empty,
                ["progress"] = model.Progress
            },
            TaskEvent.Info => new Dictionary
            {
                ["resource"] = "info", ["title"] = model!.Name, ["taskId"] = string.Empty, ["msg"] = msg
            },
            TaskEvent.SuccessMessage => new Dictionary
            {
                ["resource"] = "success", ["title"] = model!.Name, ["taskId"] = string.Empty, ["msg"] = msg,
            },
            TaskEvent.Warning => new Dictionary
            {
                ["resource"] = "warn", ["title"] = model!.Name, ["taskId"] = string.Empty, ["msg"] = msg,
            },
            TaskEvent.Error => new Dictionary
            {
                ["resource"] = "error", ["title"] = model!.Name, ["taskId"] = string.Empty, ["msg"] = msg,
            },
            // TaskEvent.Destroy => new Dictionary
            // {
            //     ["resource"] = "destroy",
            //     ["title"] = task.GetTitle(),
            //     ["taskId"] = task.SingleId,
            //     ["msg"] = msg,
            // },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, null)
        };

        CommonRpc.Instance!.CallDeferred(Node.MethodName.Rpc, "ServerMessage", dictionary);

        switch (@event)
        {
            case TaskEvent.Error:
            {
                // Services.Get<GTaskFlowService>()!.UpdateCurrentContextStatus();
                CommonRpc.Instance!.CallDeferred(Node.MethodName.Rpc, "ServerMessage", new Dictionary()
                {
                    ["resource"] = "status",
                    ["status"] = "任务失败",
                    ["synonyms"] = true,
                    ["users"] = string.Join(",", CommonRpc.OnlineUsers.Values)
                });
                break;
            }
            case TaskEvent.Progress:
            {
                // if (task.Progress < 0)
                // Services.Get<GTaskFlowService>()!.UpdateCurrentContextStatus();
                CommonRpc.Instance!.CallDeferred(Node.MethodName.Rpc, "ServerMessage", new Dictionary()
                {
                    ["resource"] = "status",
                    ["status"] = $"任务正在运行:{model.Name}",
                    ["synonyms"] = false,
                    ["users"] = string.Join(",", CommonRpc.OnlineUsers.Values)
                });
                break;
            }
        }
    }


    /// <summary>
    /// 这个任务永远不会停止, 每次执行生成一个种子, 覆盖Context.  用于其他的任务规则的随机权重.
    ///
    /// 
    /// </summary>
    [GTaskFunc("RandomTask")] public static Func<GTaskModel, Task<int>> RandomTaskFunc = async @this =>
    {
        @this.Context.SeedInt = new Random().Next();
        await Task.Delay(1000 * 10);
        @this.Context.InvokeRuleEngine();
        return 2;
    };

    public static void Test01()
    {
        Console.WriteLine(new Random().Next()%3==0);
    }


    [GTaskFunc("InsertTaskSet")] public static Func<GTaskModel, Task<int>> InsertTaskSetFunc = async @this =>
    {
        if (@this.TryGetParam("set", out var set))
        {
            Log.Info($"支线准备开始了-> {set}");


            await Task.Delay(1000);
            @this.InsertTaskSet(set!.ToString()!, callback: () =>
            {
                @this.Progress = 100;
                Log.Info($"分支任务集完成了-> {set}");
            });
            return 105;
        }
        else if (@this.TryGetParam("sets", out var obj))
        {
            if (obj is JArray array)
            {
                var arrayCount = array.Count;
                @this.Cache["count"] = 0;
                foreach (var token in array)
                {
                    var name = token.Value<string>();
                    Log.Info($"支线准备开始了-> {name}");

                    @this.InsertTaskSet(name!, callback: () =>
                    {
                        @this.Cache["count"] += 1;

                        if (@this.Cache["count"] == arrayCount)
                        {
                            @this.Progress = 100;
                        }

                        Log.Info($"分支任务集完成了-> {name}");
                    });
                }

                return 105;
            }
        }


        return -1;
    };


    [GTaskFunc("TestInsertTaskSet")] public static Func<GTaskModel, Task<int>> InsertTaskSet_Func = async @this =>
    {
        Log.Info("支线准备开始了");
        await Task.Delay(1000);
        @this.InsertTaskSet("红巨星", callback: () =>
        {
            @this.Progress = 100;
            Log.Info("分支任务集完成了");
        });

        return 101;
    };

    [GTaskFunc("TestInsertTask")] public static Func<GTaskModel, Task<int>> InsertTaskFunc = async @this =>
    {
        Log.Info("支线准备开始了!~~~~~~~~~");
        await Task.Delay(1000);
        var model = new GTaskModel
        {
            TaskType = "MessageTask",
            Name = "MessageInsert",
            Parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                ["content"] = "test"
            }
        };
        @this.InsertTask(model, callback: () =>
        {
            @this.Progress = 100;
            // model.OnCompleted = null;
        });

        return 101;
    };

    [GTaskFunc("MessageTask")] public static Func<GTaskModel, Task<int>> MessageTaskFunc = @this =>
    {
        switch (@this.Progress)
        {
            case 1:
            {
                Console.WriteLine($"这是一个message:{@this.GetParamOrDefault("content")}");

                return Task.FromResult(50);
            }
            case 50:
            {
                Console.WriteLine($"message后日谈:{@this.Name}");
                return Task.FromResult(100);
            }
        }


        return Task.FromResult(-1);
    };

    [GTaskFunc("DelayTask")] public static Func<GTaskModel, Task<int>> DelayTaskFunc = async @this =>
    {
        switch (@this.Progress)
        {
            case 1:
            {
                Console.WriteLine("这是一个延时");

                await Task.Delay(3000);
                return 50;
            }
            case 50:
            {
                Console.WriteLine("延时后日谈");
                await Task.Delay(3000);
                return 100;
            }
        }


        return -1;
    };

    [GTaskFunc("TimerTask")] public static Func<GTaskModel, Task<int>> TimerTaskFunc = _ =>
    {
        Console.WriteLine("这是一个定时");

        return Task.FromResult(102);
    };
}