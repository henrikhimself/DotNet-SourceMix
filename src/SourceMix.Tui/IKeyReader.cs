namespace Hj.SourceMix.Tui;

internal interface IKeyReader
{
  bool KeyAvailable { get; }

  ConsoleKeyInfo ReadKey();

  bool TryReadKey(TimeSpan timeout, out ConsoleKeyInfo key);
}
