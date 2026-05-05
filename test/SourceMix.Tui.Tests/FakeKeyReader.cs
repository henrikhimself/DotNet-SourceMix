namespace Hj.SourceMix.Tui.Tests;

internal sealed class FakeKeyReader : IKeyReader
{
  private readonly Queue<object?> _queue;

  public FakeKeyReader(IEnumerable<ConsoleKeyInfo> keys)
  {
    _queue = new Queue<object?>(keys.Cast<object?>());
  }

  public bool KeyAvailable => _queue.Count > 0 && _queue.Peek() is ConsoleKeyInfo;

  public ConsoleKeyInfo ReadKey()
  {
    while (_queue.Count > 0)
    {
      var next = _queue.Dequeue();

      switch (next)
      {
        case ConsoleKeyInfo cki:
          return cki;
        case Action action:
          action();
          break;
      }
    }

    throw new InvalidOperationException("No more keys queued.");
  }

  public bool TryReadKey(TimeSpan timeout, out ConsoleKeyInfo key)
  {
    if (_queue.Count == 0)
    {
      key = default;

      return false;
    }

    var next = _queue.Dequeue();

    switch (next)
    {
      case ConsoleKeyInfo cki:
        key = cki;
        return true;
      case Action action:
        action();
        key = default;
        return false;
      default:
        key = default;
        return false;
    }
  }

  public void Enqueue(ConsoleKeyInfo key) => _queue.Enqueue(key);

  public void EnqueueIdle() => _queue.Enqueue(null);

  public void EnqueueAction(Action action) => _queue.Enqueue(action);
}
