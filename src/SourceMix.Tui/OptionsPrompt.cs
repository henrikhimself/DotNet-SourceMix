using System.Globalization;
using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class OptionsPrompt
{
  private const string RecursiveKey = "recursive";
  private const string LimitDepthKey = "limit-depth";
  private const string IncludeCompiledKey = "include-compiled";
  private const string TrimKey = "trim";

  internal static OptionsPromptResult Show(
    SolutionPreferences preferences,
    ITuiConsole? tuiConsole = null,
    IKeyReader? keys = null)
  {
    ArgumentNullException.ThrowIfNull(preferences);

    tuiConsole ??= new SystemTuiConsole();
    keys ??= new ConsoleKeyReader();

    var defaults = preferences.Defaults;
    var toggles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (defaults.Recursive)
    {
      toggles.Add(RecursiveKey);
    }

    if (defaults.LimitDepth)
    {
      toggles.Add(LimitDepthKey);
    }

    if (defaults.IncludeCompiled)
    {
      toggles.Add(IncludeCompiledKey);
    }

    if (defaults.Trim)
    {
      toggles.Add(TrimKey);
    }

    var maxDepth = defaults.MaxDepth > 0 ? defaults.MaxDepth : 3;

    while (true)
    {
      var togglesResult = ShowToggles(toggles, maxDepth, tuiConsole, keys);

      if (togglesResult == StepResult.Back)
      {
        return new OptionsPromptResult(null, StepResult.Back);
      }

      if (togglesResult == StepResult.Quit)
      {
        return new OptionsPromptResult(null, StepResult.Quit);
      }

      if (toggles.Contains(RecursiveKey) && toggles.Contains(LimitDepthKey))
      {
        var (depthText, depthOk, depthQuit) = TextInputPrompt.Read(
          "  Max depth for 'Limit recursion depth':",
          maxDepth.ToString(CultureInfo.InvariantCulture),
          tuiConsole,
          keys,
          static t => int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) && d > 0
            ? null
            : "Depth must be a positive integer.");

        if (depthQuit)
        {
          return new OptionsPromptResult(null, StepResult.Quit);
        }

        if (!depthOk)
        {
          tuiConsole.ClearLines(1);
          continue;
        }

        maxDepth = int.Parse(depthText, NumberStyles.Integer, CultureInfo.InvariantCulture);
      }

      var recursiveOn = toggles.Contains(RecursiveKey);
      var effectiveMaxDepth = recursiveOn && toggles.Contains(LimitDepthKey) ? maxDepth : int.MaxValue;
      var effectiveIncludeCompiled = recursiveOn && toggles.Contains(IncludeCompiledKey);
      var effectiveTrim = recursiveOn && toggles.Contains(TrimKey);

      return new OptionsPromptResult(
        new OptionsToggleValues(recursiveOn, effectiveMaxDepth, effectiveIncludeCompiled, effectiveTrim),
        StepResult.Confirm);
    }
  }

  private static StepResult ShowToggles(HashSet<string> toggles, int maxDepth, ITuiConsole console, IKeyReader keys)
  {
    var limitDepthLabel = $"  Limit recursion depth (uses 'Max depth' = {maxDepth.ToString(CultureInfo.InvariantCulture)})";
    var items = new List<ToggleItem>
    {
      new(RecursiveKey, "Include recursive type dependencies"),
      new(LimitDepthKey, limitDepthLabel),
      new(IncludeCompiledKey, "Decompile interfaces and models from compiled assemblies"),
      new(TrimKey, "Trim method bodies from dependency files (keep signatures only)"),
    };

    var picker = new ListPickerPrompt<ToggleItem>
    {
      Header = "[bold]Configure options[/]  [dim](Space toggle \u00b7 Enter confirm \u00b7 Ctrl+Q back)[/]",
      Items = items,
      KeySelector = static i => i.Key,
      MultiSelect = true,
      AllowPin = false,
      InitialSelected = toggles,
      Renderer = RenderToggle,
    };

    var result = picker.Show(console, keys);

    if (result.Reason == ListPickerExitReason.Skipped)
    {
      return StepResult.Back;
    }

    if (result.Reason == ListPickerExitReason.Quit)
    {
      return StepResult.Quit;
    }

    toggles.Clear();

    if (result.SelectedKeys is { } selectedKeys)
    {
      foreach (var key in selectedKeys)
      {
        toggles.Add(key);
      }
    }

    return StepResult.Confirm;
  }

  private static string RenderToggle(ToggleItem item, ListPickerItemState state)
  {
    var arrow = state.IsCursor ? "[darkorange]>[/]" : " ";
    var checkbox = state.IsSelected ? "[green][[x]][/]" : "[dim][[ ]][/]";
    var label = state.IsCursor
      ? $"[bold]{Markup.Escape(item.Display)}[/]"
      : $"[dim]{Markup.Escape(item.Display)}[/]";

    return $" {arrow} {checkbox} {label}";
  }

  private sealed record ToggleItem(string Key, string Display);
}
