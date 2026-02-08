using System.Collections.Concurrent;
using DevManager.Core.Models;
using DevManager.Core.Services.Interfaces;

namespace DevManager.Core.Services;

public class LogService : ILogService
{
    private readonly ConcurrentDictionary<Guid, CircularBuffer<LogEntry>> _buffers = new();
    private readonly int _maxLines;

    public event EventHandler<LogEntry>? LogReceived;

    public LogService(int maxLines = 5000)
    {
        _maxLines = maxLines;
    }

    public void AppendLog(Guid processDefinitionId, string text, LogEntryType type)
    {
        var entry = new LogEntry(processDefinitionId, DateTime.Now, text, type);
        var buffer = _buffers.GetOrAdd(processDefinitionId, _ => new CircularBuffer<LogEntry>(_maxLines));
        buffer.Add(entry);
        LogReceived?.Invoke(this, entry);
    }

    public IReadOnlyList<LogEntry> GetLogs(Guid processDefinitionId)
    {
        if (_buffers.TryGetValue(processDefinitionId, out var buffer))
            return buffer.ToList();
        return [];
    }

    public void ClearLogs(Guid processDefinitionId)
    {
        if (_buffers.TryGetValue(processDefinitionId, out var buffer))
            buffer.Clear();
    }
}

internal class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private readonly object _lock = new();
    private int _start;
    private int _count;

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            var index = (_start + _count) % _buffer.Length;
            _buffer[index] = item;
            if (_count == _buffer.Length)
                _start = (_start + 1) % _buffer.Length;
            else
                _count++;
        }
    }

    public List<T> ToList()
    {
        lock (_lock)
        {
            var list = new List<T>(_count);
            for (int i = 0; i < _count; i++)
                list.Add(_buffer[(_start + i) % _buffer.Length]);
            return list;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _start = 0;
            _count = 0;
        }
    }
}
