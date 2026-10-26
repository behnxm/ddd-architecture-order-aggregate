# 0012. Banned-API Analyzer to Guarantee Domain Determinism

## Context
Aggregates in Domain-Driven Design must remain completely pure and deterministic. When domain entities or value objects call static environmental APIs (such as `DateTime.UtcNow`, `DateTime.Now`, `DateTimeOffset.UtcNow`, or `Guid.NewGuid()`), unit tests become flaky, time cannot be controlled or simulated deterministically, and domain logic becomes coupled to system clocks.

## Decision
I enforced deterministic time and identity generation at compile time using `Microsoft.CodeAnalysis.BannedApiAnalyzers`:
1. **Domain Layer**:
   - `BannedSymbols.txt` strictly forbids:
     - `System.DateTime.Now`
     - `System.DateTime.UtcNow`
     - `System.DateTimeOffset.Now`
     - `System.DateTimeOffset.UtcNow`
     - `System.Guid.NewGuid()`
   - All domain methods that record timestamps must accept `DateTimeOffset now` as an explicit parameter passed in by the caller.
   - All entity and event identity generation must use the strongly-typed ID factories backed by `Guid.CreateVersion7()`.
2. **Application Layer**:
   - Banned static system clock calls (`DateTime.Now`, `DateTime.UtcNow`, etc.).
   - All application handlers receive time strictly through `System.TimeProvider` injected via dependency injection.
   - Unit tests inject `Microsoft.Extensions.Time.Testing.FakeTimeProvider` for 100% deterministic time control.

## Consequences
- **Positive**: Compile-time errors immediately prevent inadvertent clock coupling or non-deterministic ID generation.
- **Positive**: Domain logic and time-dependent invariants (such as stale draft cutoff thresholds) are 100% reproducible in automated tests.
- **Positive**: Zero runtime overhead: the analyzer executes entirely at compile time as a private asset.
