# TODO

## Post-1.0: Improve `DependencyResolver` scalability

**Status:** Planned follow-up

### Problem

`src/SourceMix.Core/DependencyResolver.cs` currently reads and parses all `.cs` files in the solution up front before resolving dependencies from the selected seed files. This is simple and correct, but it can become slow and memory-heavy for large solutions where the user only selected a small set of files.

### Why this matters

- Slower startup for large codebases
- Higher memory usage than necessary
- Unnecessary work when dependency resolution only needs a small fraction of the solution

### Goal

Keep the current dependency-resolution behavior, ordering, and namespace disambiguation, but reduce the amount of eager whole-solution parsing.

### Implementation plan

1. Measure the current behavior on a medium and large solution to confirm where the cost is coming from: file discovery, file reads, syntax parsing, or type-index construction.
2. Split the resolver into clearer phases so file discovery, type indexing, and dependency traversal are separate concerns.
3. Replace the current eager parse of every `.cs` file with a more incremental approach:
   - discover candidate files first
   - build only the minimum metadata needed for type lookup
   - parse full syntax trees only when a file is actually needed during traversal
4. Keep a cache for parsed files so repeated lookups do not re-read or re-parse the same file.
5. Preserve the current namespace-based narrowing behavior and unresolved-type reporting.
6. Add tests that prove the refactor keeps the current behavior:
   - same resolved file set
   - same seed-file ordering behavior
   - same unresolved type names
   - same namespace disambiguation behavior
7. If practical, add a lightweight performance-oriented test or benchmark harness that can be run locally to compare the old and new approaches.

### Constraints

- Do not change the user-facing CLI or TUI behavior as part of this work.
- Keep the result deterministic.
- Avoid a risky rewrite that changes resolution correctness just to optimize speed.

### Likely files involved

- `src/SourceMix.Core/DependencyResolver.cs`
- `test/SourceMix.Tests/DependencyResolverTests.cs`

### Success criteria

- Dependency resolution returns the same logical results as today.
- Large solutions do less unnecessary up-front parsing.
- The code remains maintainable and test-covered.
