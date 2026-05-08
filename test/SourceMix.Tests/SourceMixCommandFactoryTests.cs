namespace Hj.SourceMix.Tests;

public sealed class SourceMixCommandFactoryTests
{
  [Fact]
  public async Task InvokeAsync_NoArguments_UsesCliHandlerAsync()
  {
    CliRequest? cliRequest = null;

    var command = SourceMixCommandFactory.Create(
      (request, _) =>
      {
        cliRequest = request;
        return Task.FromResult(11);
      },
      _ => Task.FromResult(22));

    var exitCode = await command.Parse([]).InvokeAsync();

    Assert.Equal(11, exitCode);
    Assert.NotNull(cliRequest);
    Assert.Empty(cliRequest.Files);
  }

  [Fact]
  public async Task InvokeAsync_CliArguments_BindsExpectedValuesAsync()
  {
    CliRequest? cliRequest = null;

    var command = SourceMixCommandFactory.Create(
      (request, _) =>
      {
        cliRequest = request;
        return Task.FromResult(33);
      },
      _ => Task.FromResult(44));

    var exitCode = await command
      .Parse(["Foo.cs", "-o", "context.md", "-r", "-d", "3", "-c", "-t", "-e", "-p", "code-review", "-s", "arch", "tests"])
      .InvokeAsync();

    Assert.Equal(33, exitCode);
    Assert.NotNull(cliRequest);
    Assert.Equal(["Foo.cs"], cliRequest.Files);
    Assert.Equal("context.md", cliRequest.Output?.Name);
    Assert.True(cliRequest.Recursive);
    Assert.Equal(3, cliRequest.Depth);
    Assert.True(cliRequest.IncludeCompiled);
    Assert.True(cliRequest.Trim);
    Assert.True(cliRequest.ExpandTypes);
    Assert.Equal("code-review", cliRequest.Prompt);
    Assert.Equal(["arch", "tests"], cliRequest.SkillKeys);
  }

  [Fact]
  public async Task InvokeAsync_TuiSubcommand_UsesTuiHandlerAsync()
  {
    var cliInvoked = false;
    var tuiInvoked = false;

    var command = SourceMixCommandFactory.Create(
      (_, _) =>
      {
        cliInvoked = true;
        return Task.FromResult(55);
      },
      _ =>
      {
        tuiInvoked = true;
        return Task.FromResult(66);
      });

    var exitCode = await command.Parse(["tui"]).InvokeAsync();

    Assert.Equal(66, exitCode);
    Assert.False(cliInvoked);
    Assert.True(tuiInvoked);
  }
}
