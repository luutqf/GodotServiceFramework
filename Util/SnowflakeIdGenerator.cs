namespace SigmusV2.GodotServiceFramework.Util;

/// <summary>
/// 雪花算法的现代实现
/// </summary>
public sealed class SnowflakeIdGenerator
{
    private static SnowflakeIdGenerator? _generator;
    private static readonly Lock GenLock = new();

    public static ulong NextUId()
    {
        return (ulong)NextId();
    }

    public static long NextId()
    {
        if (_generator == null)
        {
            lock (GenLock)
            {
                _generator ??= new SnowflakeIdGenerator(0, 0);
            }
        }

        return _generator._NextId();
    }

    private readonly struct IdStructure
    {
        // 各部分占用的位数
        public const int TimestampBits = 41; // 时间戳占用位数
        public const int WorkerIdBits = 5; // 工作机器ID占用位数
        public const int DatacenterIdBits = 5; // 数据中心ID占用位数
        public const int SequenceBits = 12; // 序列号占用位数

        // 每部分的最大值
        public const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        public const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);
        public const long MaxSequence = -1L ^ (-1L << SequenceBits);

        // 每部分向左的偏移量
        public const int WorkerIdShift = SequenceBits;
        public const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        public const int TimestampShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
    }

    // 开始时间戳 (2020-01-01)
    private const long Epoch = 1577808000000L;

    private readonly long _datacenterId;
    private readonly long _workerId;
    private long _sequence;
    private long _lastTimestamp = -1L;

    private readonly Lock _lock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="datacenterId">数据中心ID</param>
    /// <param name="workerId">工作机器ID</param>
    public SnowflakeIdGenerator(long datacenterId, long workerId)
    {
        // 检查datacenterId是否合法
        if (datacenterId > IdStructure.MaxDatacenterId || datacenterId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(datacenterId),
                $"Datacenter ID must be between 0 and {IdStructure.MaxDatacenterId}");
        }

        // 检查workerId是否合法
        if (workerId is > IdStructure.MaxWorkerId or < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workerId),
                $"Worker ID must be between 0 and {IdStructure.MaxWorkerId}");
        }

        _workerId = workerId;
        _datacenterId = datacenterId;
    }

    /// <summary>
    /// 生成下一个ID
    /// </summary>
    /// <returns>生成的唯一ID</returns>
    public long _NextId()
    {
        lock (_lock)
        {
            var timestamp = GetTimestamp();

            // 检查时钟回拨
            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException(
                    $"Clock moved backwards. Refusing to generate id for {_lastTimestamp - timestamp} milliseconds");
            }

            // 如果是同一毫秒生成的，则递增序列号
            if (_lastTimestamp == timestamp)
            {
                _sequence = (_sequence + 1) & IdStructure.MaxSequence;
                // 同一毫秒的序列数已经达到最大
                if (_sequence == 0)
                {
                    // 阻塞到下一个毫秒，获得新的时间戳
                    timestamp = WaitNextMillis(_lastTimestamp);
                }
            }
            else
            {
                // 不同毫秒，序列从0开始
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            // 组装ID (时间戳部分 | 数据中心部分 | 机器ID部分 | 序列号部分)
            return ((timestamp - Epoch) << IdStructure.TimestampShift)
                   | (_datacenterId << IdStructure.DatacenterIdShift)
                   | (_workerId << IdStructure.WorkerIdShift)
                   | _sequence;
        }
    }

    /// <summary>
    /// 获取当前时间戳
    /// </summary>
    private static long GetTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 阻塞到下一个毫秒
    /// </summary>
    /// <param name="lastTimestamp">上次的时间戳</param>
    /// <returns>新的时间戳</returns>
    private static long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = GetTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = GetTimestamp();
        }

        return timestamp;
    }
}