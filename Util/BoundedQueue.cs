namespace SigmusV2.Script.utils;

using System;
using System.Collections.Generic;
using System.Collections;

public class BoundedQueue<T> : IEnumerable<T>
{
    private readonly Queue<T> _queue;
    private readonly int _maxCapacity;

    public BoundedQueue(int maxCapacity)
    {
        if (maxCapacity <= 0)
            throw new ArgumentException("最大容量必须大于0", nameof(maxCapacity));

        _maxCapacity = maxCapacity;
        _queue = new Queue<T>();
    }

    public BoundedQueue(int maxCapacity, IEnumerable<T> collection)
    {
        if (maxCapacity <= 0)
            throw new ArgumentException("最大容量必须大于0", nameof(maxCapacity));

        _maxCapacity = maxCapacity;
        _queue = new Queue<T>(collection);

        // 如果初始集合超过了最大容量，移除多余的元素
        while (_queue.Count > _maxCapacity)
        {
            _queue.Dequeue();
        }
    }

    public void Enqueue(T item)
    {
        // 如果已达到最大容量，先出队一个元素
        if (_queue.Count >= _maxCapacity)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(item);
    }

    public T Dequeue()
    {
        return _queue.Dequeue();
    }

    public T Peek()
    {
        return _queue.Peek();
    }

    public void Clear()
    {
        _queue.Clear();
    }

    public bool Contains(T item)
    {
        return _queue.Contains(item);
    }

    public int Count => _queue.Count;

    public int MaxCapacity => _maxCapacity;

    // 实现 IEnumerable<T> 接口，让类可以被迭代
    public IEnumerator<T> GetEnumerator()
    {
        return _queue.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}