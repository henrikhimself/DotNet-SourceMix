namespace Hj.SourceMix.Core;

public static class BuiltInPrompts
{
  private const string NUnitTestText = "Write comprehensive NUnit unit tests for every public type and member in the code above. Start with happy-path scenarios, then add tests that break the happy path: null, empty, and boundary-value inputs; cases that should throw expected exceptions; and other edge cases. Use Assert.That for assertions wherever possible. Organize tests with one test class per production class, and follow the Arrange / Act / Assert pattern, beginning each phase with a code comment.";
  private const string XUnitTestText = "Write comprehensive xUnit unit tests for all public types and members in the code above. Cover: happy-path scenarios, null/empty/boundary-value inputs, expected exceptions, and edge cases. Prefer `[Theory]` with `[InlineData]` for parameterised inputs. Use `FluentAssertions` for assertions where possible. Organise tests into one test class per production class, following the Arrange/Act/Assert pattern.";
  private const string CodeReviewText = "Review the code above for: correctness (bugs, off-by-one errors, null-reference risks), security (input validation, injection risks, secret handling), performance (unnecessary allocations, N+1 queries, blocking async calls), maintainability (naming clarity, method length, SOLID violations), and C# best practices. For each finding, state the location, severity (critical/major/minor), and a suggested fix.";
  private const string TechDocsText = "Write technical documentation for all public types and members in the code above. Provide: (1) an overview paragraph for each class describing its purpose and responsibilities, (2) XML documentation comments (`<summary>`, `<param>`, `<returns>`, `<exception>`) for all public methods and properties, (3) a concise Markdown API reference table per class listing member name, type, and description.";
  private const string ExplainText = "Explain what the code above does to a developer who is unfamiliar with this codebase. Cover: the overall purpose of each class, how the classes interact with each other, the key algorithms or data-flow patterns used, any non-obvious design decisions, and what a new contributor should understand before modifying this code.";
  private const string DebugText = "Analyse the code above and identify potential runtime errors and logic bugs. Look specifically for: NullReferenceExceptions, index-out-of-bounds errors, race conditions, unhandled exceptions, incorrect null checks, off-by-one errors, infinite loops or missing exit conditions, and incorrect use of async/await. For each issue, state the location, the root cause, and a suggested fix.";
  private const string RefactorText = "Suggest concrete refactoring improvements for the code above. Focus on: extracting long methods, reducing parameter counts, eliminating duplication, improving naming, applying appropriate design patterns, reducing coupling, and improving testability. For each suggestion, show the before and after code.";
  private const string ArchitectureText = "Analyse the architecture of the code above. Describe: the design patterns in use, the coupling and cohesion between components, adherence to SOLID principles, potential scalability or maintainability concerns, and recommended structural improvements with rationale.";

  public static IReadOnlyDictionary<string, BuiltInPrompt> All { get; } =
    new Dictionary<string, BuiltInPrompt>(StringComparer.OrdinalIgnoreCase)
    {
      ["nunit-test"] = new("Write nUnit tests", NUnitTestText),
      ["xunit-test"] = new("Write xUnit tests", XUnitTestText),
      ["code-review"] = new("Review code", CodeReviewText),
      ["tech-docs"] = new("Write technical documentation", TechDocsText),
      ["explain"] = new("Explain code", ExplainText),
      ["debug"] = new("Find bugs", DebugText),
      ["refactor"] = new("Suggest refactoring", RefactorText),
      ["architecture"] = new("Analyse architecture", ArchitectureText),
    };
}
