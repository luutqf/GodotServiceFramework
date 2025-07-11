using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GodotServiceFramework.Context.Service;

namespace GodotServiceFramework.GTaskV3;

/// <summary>
/// GTask任务池，管理BaseGTask实例的复用
/// </summary>
[InjectService]
public class GTaskPool
{
    private readonly ConcurrentQueue<BaseGTask> _availableTasks; // 可用任务队列
    private readonly HashSet<BaseGTask> _acquiredTasks; // 已被获取的任务集合
    public int MaxPoolSize { get; set; } = 64;

    private readonly Lock _lockObject = new();

    public event Action<int> OnTaskRelease = delegate { };

    /// <summary>
    /// 构造函数
    /// </summary>
    public GTaskPool()
    {
        _availableTasks = [];
        _acquiredTasks = [];
    }

    /// <summary>
    /// 从池中获取一个可用的BaseGTask实例
    /// </summary>
    /// <returns>可用的BaseGTask实例，如果所有实例都不可用则返回null</returns>
    public BaseGTask GetTask()
    {
        //TODO 这里临时处理一下
        // foreach (var task in _acquiredTasks.Where(task => task.Progress == 100))
        // {
        //     task.Destroy();
        // }

        lock (_lockObject)
        {
            // 优先从可用队列中获取任务
            if (_availableTasks.Count > 0)
            {
                if (_availableTasks.TryDequeue(out var task))
                {
                    _acquiredTasks.Add(task);
                    return task;
                }
                else
                {
                    throw new Exception("无法获取task");
                }
            }

            // 如果可用队列为空，检查是否可以创建新的任务实例
            if (_acquiredTasks.Count < MaxPoolSize)
            {
                var newTask = new BaseGTask();
                _acquiredTasks.Add(newTask);
                return newTask;
            }

            // 如果已达到最大数量，拒绝服务
            throw new Exception("无资源");
        }
    }

    /// <summary>
    /// 从池中获取指定数量的BaseGTask实例
    /// </summary>
    /// <param name="count">要获取的任务数量</param>
    /// <returns>获取到的任务列表</returns>
    /// <exception cref="InvalidOperationException">当请求的数量超过池的总容量限制时抛出</exception>
    public Queue<BaseGTask> GetTasks(int count)
    {
        //TODO 这里临时处理一下
        // foreach (var task in _acquiredTasks.Where(task => task.Progress == 100))
        // {
        // task.Destroy();
        // }

        lock (_lockObject)
        {
            var remainingSlots = GetUsableSize();

            // 检查是否超过总容量限制
            if (count > remainingSlots)
            {
                throw new InvalidOperationException(
                    $"请求获取 {count} 个任务，但池中只能容纳 {remainingSlots} 个任务。池总容量：{MaxPoolSize}，已获取：{_acquiredTasks.Count}");
            }

            var tasks = new Queue<BaseGTask>();

            // 优先从可用队列中获取
            while (tasks.Count < count && _availableTasks.Count > 0)
            {
                if (_availableTasks.TryDequeue(out var task))
                {
                    _acquiredTasks.Add(task);
                    tasks.Enqueue(task);
                }
                else
                {
                    throw new Exception("无法获取task");
                }
            }

            // 如果队列中的任务不够，创建新任务
            while (tasks.Count < count)
            {
                var newTask = new BaseGTask();
                _acquiredTasks.Add(newTask);
                tasks.Enqueue(newTask);
            }

            return tasks;
        }
    }

    public int GetUsableSize()
    {
        return MaxPoolSize - _acquiredTasks.Count;
    }

    /// <summary>
    /// 释放任务回池中，重新加入可用队列
    /// </summary>
    /// <param name="task">要释放的任务</param>
    public void ReleaseTask(BaseGTask task)
    {
        lock (_lockObject)
        {
            if (!_acquiredTasks.Contains(task)) return;

            _acquiredTasks.Remove(task);
            _availableTasks.Enqueue(task);
        }

        OnTaskRelease.Invoke(GetUsableSize());
    }

    /// <summary>
    /// 获取当前池中可用的任务数量
    /// </summary>
    /// <returns>可用任务数量</returns>
    public int GetAvailableTaskCount()
    {
        lock (_lockObject)
        {
            return _availableTasks.Count;
        }
    }

    /// <summary>
    /// 获取当前池中总的任务数量
    /// </summary>
    /// <returns>总任务数量</returns>
    public int GetTotalTaskCount()
    {
        lock (_lockObject)
        {
            return _availableTasks.Count + _acquiredTasks.Count;
        }
    }

    /// <summary>
    /// 获取当前池中已被获取的任务数量
    /// </summary>
    /// <returns>已被获取的任务数量</returns>
    public int GetAcquiredTaskCount()
    {
        lock (_lockObject)
        {
            return _acquiredTasks.Count;
        }
    }

    /// <summary>
    /// 清空任务池
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _availableTasks.Clear();
            _acquiredTasks.Clear();
            var array = new BaseGTask[_acquiredTasks.Count];
            _acquiredTasks.CopyTo(array);
            foreach (var baseGTask in array)
            {
                try
                {
                    baseGTask.Destroy();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    public void Clear(long id)
    {
        HashSet<BaseGTask> hashSet = [];
        foreach (var baseGTask in _acquiredTasks.ToArray())
        {
            if (baseGTask.SetId == id)
            {
                hashSet.Add(baseGTask);
            }
        }

        foreach (var task in hashSet)
        {
            try
            {
                task.Destroy();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    /// <summary>
    /// 预创建指定数量的任务实例
    /// </summary>
    /// <param name="count">要预创建的任务数量</param>
    public void PreCreateTasks(int count)
    {
        lock (_lockObject)
        {
            var actualCount = Math.Min(count, MaxPoolSize - GetTotalTaskCount());
            for (var i = 0; i < actualCount; i++)
            {
                var task = new BaseGTask();
                _availableTasks.Enqueue(task);
            }
        }
    }
}