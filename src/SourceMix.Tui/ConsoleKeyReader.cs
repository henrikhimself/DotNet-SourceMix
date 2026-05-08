namespace Hj.SourceMix.Tui;

internal sealed class ConsoleKeyReader : IKeyReader
{
  public bool KeyAvailable => Console.KeyAvailable;

  public ConsoleKeyInfo ReadKey()
  {
    return Console.ReadKey(intercept: true);
  }

  public bool TryReadKey(TimeSpan timeout, out ConsoleKeyInfo key)
  {
    var deadline = DateTime.UtcNow + timeout;

    while (DateTime.UtcNow < deadline)
    {
      if (Console.KeyAvailable)
      {
        key = Console.ReadKey(intercept: true);

        return true;
      }

      Thread.Sleep(20);
    }

    key = default;

    return false;
  }
}
