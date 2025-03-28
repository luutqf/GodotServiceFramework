using System.Collections.Concurrent;

namespace GodotServiceFramework.Util;

/// <summary>
/// 自定义线程池实现
/// </summary>
public class CustomThreadPool : IDisposable
{
    private readonly int _minThreads;
    private readonly int _maxThreads;
    private readonly TimeSpan _idleTimeout;
    private readonly ConcurrentQueue<WorkItem> _workItems = new ConcurrentQueue<WorkItem>();
    private readonly List<ThreadInfo> _threads = new List<ThreadInfo>();
    private readonly object _syncLock = new object();
    private readonly ManualResetEventSlim _workEvent = new ManualResetEventSlim(false);
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private int _busyThreads = 0;
    private bool _isDisposed = false;

    /// <summary>
    /// 创建新的自定义线程池
    /// </summary>
    /// <param name="minThreads">最小线程数</param>
    /// <param name="maxThreads">最大线程数</param>
    /// <param name="idleTimeoutSeconds">空闲线程超时（秒）</param>
    public CustomThreadPool(int minThreads = 1, int maxThreads = 4,
        int idleTimeoutSeconds = 30)
    {
        if (minThreads < 0) throw new ArgumentException("最小线程数不能小于0", nameof(minThreads));
        if (maxThreads <= 0) throw new ArgumentException("最大线程数必须大于0", nameof(maxThreads));
        if (minThreads > maxThreads) throw new ArgumentException("最小线程数不能大于最大线程数");

        _minThreads = minThreads;
        _maxThreads = maxThreads;
        _idleTimeout = TimeSpan.FromSeconds(idleTimeoutSeconds);

        // 初始化最小数量的线程
        for (int i = 0; i < minThreads; i++)
        {
            CreateAndStartThread();
        }

        // 启动管理线程，用于监控和管理线程池
        Task.Run(ManageThreadPool, _cts.Token);
    }

    /// <summary>
    /// 获取当前线程池状态
    /// </summary>
    public ThreadPoolStatus GetStatus()
    {
        lock (_syncLock)
        {
            return new ThreadPoolStatus
            {
                TotalThreads = _threads.Count,
                BusyThreads = _busyThreads,
                IdleThreads = _threads.Count - _busyThreads,
                QueuedItems = _workItems.Count
            };
        }
    }

    /// <summary>
    /// 将工作项提交到线程池执行
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <returns>表示异步操作的任务</returns>
    public Task QueueWorkItem(Action action)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(CustomThreadPool));

        var tcs = new TaskCompletionSource<object?>();
        var workItem = new WorkItem(action, tcs);

        _workItems.Enqueue(workItem);
        _workEvent.Set(); // 通知有新工作

        EnsureThreadsCapacity();

        return tcs.Task;
    }

    /// <summary>
    /// 将带参数的工作项提交到线程池执行
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="state">传递给操作的状态对象</param>
    /// <returns>表示异步操作的任务</returns>
    public Task QueueWorkItem(Action<object> action, object state)
    {
        return QueueWorkItem(() => action(state));
    }

    /// <summary>
    /// 将带返回值的工作项提交到线程池执行
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <returns>表示异步操作的任务，包含返回值</returns>
    public Task<TResult> QueueWorkItem<TResult>(Func<TResult> func)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(CustomThreadPool));

        var tcs = new TaskCompletionSource<TResult>();

        QueueWorkItem(() =>
        {
            try
            {
                var result = func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    /// 确保线程池有足够容量处理工作项
    /// </summary>
    private void EnsureThreadsCapacity()
    {
        lock (_syncLock)
        {
            // 如果所有线程都忙，且未达到最大线程数，则创建新线程
            if (_busyThreads >= _threads.Count && _threads.Count < _maxThreads)
            {
                CreateAndStartThread();
            }
        }
    }

    /// <summary>
    /// 创建并启动新的工作线程
    /// </summary>
    private void CreateAndStartThread()
    {
        var thread = new Thread(WorkerThreadFunc!)
        {
            IsBackground = true,
            Name = $"CustomThreadPool-Worker-{Guid.NewGuid()}"
        };

        var threadInfo = new ThreadInfo
        {
            Thread = thread,
            LastActivityTime = DateTime.UtcNow
        };

        lock (_syncLock)
        {
            _threads.Add(threadInfo);
        }

        thread.Start(threadInfo);
    }

    /// <summary>
    /// 工作线程的主函数
    /// </summary>
    private void WorkerThreadFunc(object state)
    {
        var threadInfo = (ThreadInfo)state;

        while (!_cts.Token.IsCancellationRequested)
        {
            if (_workItems.TryDequeue(out var workItem))
            {
                try
                {
                    Interlocked.Increment(ref _busyThreads);
                    threadInfo.LastActivityTime = DateTime.UtcNow;
                    threadInfo.IsIdle = false;

                    // 执行工作项
                    workItem.Execute();
                }
                catch (Exception ex)
                {
                    // 记录异常但不让它终止线程
                    Console.WriteLine($"Thread pool worker exception: {ex}");
                }
                finally
                {
                    Interlocked.Decrement(ref _busyThreads);
                    threadInfo.IsIdle = true;
                }
            }
            else
            {
                _workEvent.Reset();

                // 等待新工作或取消信号
                _workEvent.Wait(TimeSpan.FromSeconds(1), _cts.Token);
            }
        }
    }

    /// <summary>
    /// 管理线程池的线程函数
    /// </summary>
    private async Task ManageThreadPool()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                // 每5秒检查一次
                await Task.Delay(5000, _cts.Token);

                ThreadInfo[] threadsToRemove = [];
                lock (_syncLock)
                {
                    // 如果超过最小线程数，并且有空闲线程，检查超时
                    if (_threads.Count > _minThreads)
                    {
                        var idleThreads = _threads
                            .Where(t => t.IsIdle && DateTime.UtcNow - t.LastActivityTime > _idleTimeout)
                            .ToArray();

                        // 计算可以安全移除的线程数量
                        int excessThreads = Math.Min(idleThreads.Length, _threads.Count - _minThreads);
                        if (excessThreads > 0)
                        {
                            threadsToRemove = idleThreads.Take(excessThreads).ToArray();
                            foreach (var threadInfo in threadsToRemove)
                            {
                                _threads.Remove(threadInfo);
                            }
                        }
                    }
                }

                // 安全地终止超时线程
                if (threadsToRemove.Length > 0)
                {
                    foreach (var threadInfo in threadsToRemove)
                    {
                        threadInfo.ShouldExit = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Thread pool manager exception: {ex}");
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _cts.Cancel();
        _workEvent.Set(); // 唤醒所有等待的线程

        // 等待所有线程完成
        foreach (var threadInfo in _threads.ToArray())
        {
            threadInfo.ShouldExit = true;
            threadInfo.Thread!.Join(100); // 等待一小段时间
        }

        _workEvent.Dispose();
        _cts.Dispose();
    }

    /// <summary>
    /// 工作项，表示提交到线程池的任务
    /// </summary>
    private class WorkItem(Action action, TaskCompletionSource<object?> taskSource)
    {
        public void Execute()
        {
            try
            {
                action();
                taskSource.SetResult(null);
            }
            catch (Exception ex)
            {
                taskSource.SetException(ex);
            }
        }
    }

    /// <summary>
    /// 线程信息，用于跟踪线程状态
    /// </summary>
    private class ThreadInfo
    {
        public Thread? Thread { get; set; }
        public DateTime LastActivityTime { get; set; }
        public bool IsIdle { get; set; } = true;
        public bool ShouldExit { get; set; } = false;
    }
}

/// <summary>
/// 线程池状态信息
/// </summary>
public class ThreadPoolStatus
{
    public int TotalThreads { get; set; }
    public int BusyThreads { get; set; }
    public int IdleThreads { get; set; }
    public int QueuedItems { get; set; }
}