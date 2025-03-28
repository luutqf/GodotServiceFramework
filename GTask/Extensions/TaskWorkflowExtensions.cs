using System.Diagnostics;
using GodotServiceFramework.Context.Session;
using GodotServiceFramework.Extensions;

namespace GodotServiceFramework.GTask.Extensions;

public static class TaskWorkflowExtensions
{
    /// <summary>
    /// 保存一个任务流, 用于恢复进度, 这个暂时不考虑🤔
    /// </summary>
    /// <param name="this"></param>
    /// <returns></returns>
    public static bool SaveWorkflow(this GameTaskWorkflow @this)
    {
        return true;
    }

    /// <summary>
    /// 运行下一组Task
    /// </summary>
    /// <param name="workFlow"></param>
    /// <param name="nextTasks"></param>
    /// <param name="nextIndex"></param>
    public static void RunNextTasks(this GameTaskWorkflow workFlow, GameTask[] nextTasks, int nextIndex)
    {
        if (!workFlow.IsStarted || workFlow.IsDestroyed)
        {
            return;
        }

        foreach (var nextTask in nextTasks)
        {
            //TODO 这里的公共参数需要提取

            workFlow.RunTask(nextTask);
        }
    }


    /// <summary>
    /// 运行一个Task
    /// </summary>
    /// <param name="workFlow"></param>
    /// <param name="nextTask"></param>
    public static void RunTask(this GameTaskWorkflow workFlow, GameTask nextTask)
    {
        var sessionId = workFlow.GetSessionId(out _);

        nextTask.Args.AddRange(workFlow.CommonArgs); //TODO 这里考虑取出
        
        Task.Run(() =>
        {
            using var activity = new Activity(sessionId.ToString()).Start();
            activity.AddTag("session", sessionId);
            _ = nextTask.Start0();
        }, workFlow.Cts.Token);
    }


    /// <summary>
    /// 根据一个坐标, 获取下一组任务
    /// </summary>
    /// <param name="workflow"></param>
    /// <param name="indices"></param>
    /// <param name="finished"></param>
    /// <param name="nextIndex"></param>
    /// <returns></returns>
    public static GameTask[] GetNextTasks(this GameTaskWorkflow workflow, int[] indices, out bool finished,
        out int nextIndex)
    {
        nextIndex = -1;
        GameTask[] nextTasks = [];
        if (indices.Length < 2)
        {
            finished = true;
            return [];
        }

        // var status = workflow.Status;


        var ints = workflow.TaskProgress[indices[0]];
        if (ints.All(i => i == 100) || ints.Any(i => i < 0))
        {
            // Console.WriteLine($"ints.All(i => i == 100): {ints.All(i => i == 100)}");
            // Console.WriteLine($"ints.Any(i => i < 0): {ints.Any(i => i == -1)}");
            if (workflow.Count > indices[0] + 1)
            {
                nextIndex = indices[0] + 1;
                nextTasks = workflow[nextIndex];
            }
        }


        finished = ints.All(i => i == 100);

        return nextTasks;
    }


    /// <summary>
    /// 检查某个任务坐标是否存在,如果存在, 则out一个任务对象
    /// </summary>
    /// <param name="workflow"></param>
    /// <param name="indices"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IndexExists(this GameTaskWorkflow workflow, int[] indices, out GameTask value)
    {
        if (indices.Length < 2)
        {
            value = null!;
            return false;
        }

        try
        {
            var outerIndex = indices[0];
            var innerIndex = indices[1];


            var indexExists = outerIndex < workflow.Count &&
                              innerIndex < workflow[outerIndex].Length;
            value = workflow[outerIndex][innerIndex];
            return indexExists;
        }
        catch (Exception)
        {
            value = null!;
            return false;
        }
    }

    /// <summary>
    /// 随机生成一个任务流ID,这里预计要避免重复
    /// </summary>
    /// <param name="workflow"></param>
    /// <returns></returns>
    public static int RandomFlowId(this GameTaskWorkflow workflow)
    {
        while (true)
        {
            var random = new Random();
            var index = random.Next(0, 999999999);
            return -index;
        }
    }
}