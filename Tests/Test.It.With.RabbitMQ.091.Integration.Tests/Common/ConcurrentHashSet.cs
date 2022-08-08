using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Test.It.With.RabbitMQ091.Integration.Tests.Common;

internal class ConcurrentHashSet<T> : IEnumerable<T>
{
    private readonly ConcurrentDictionary<T, T> _data;

    public ConcurrentHashSet(params T[] data) => 
        _data = new ConcurrentDictionary<T, T>(data.Select(value => new KeyValuePair<T, T>(value, value)));

    internal bool TryRemove(T value)
    {
        return _data.TryRemove(value, out _);
    }

    internal bool TryAdd(T value)
    {
        return _data.TryAdd(value, value);
    }

    internal bool IsEmpty => _data.IsEmpty;

    public IEnumerator<T> GetEnumerator()
    {
        return _data.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}