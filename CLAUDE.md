# CLAUDE.md

Instructions for Claude Code when working in this repository.

## Build & Test

```bash
# Build the solution
dotnet build

# Run all tests (~460 tests)
dotnet test

# Run a specific test project
dotnet test tests/TheCanonry.Illuminator.Tests/

# Run the desktop app
dotnet run --project src/TheCanonry.Desktop/
```

Building and testing the C# project is always safe and encouraged. Use `dotnet build` to verify compilation and `dotnet test` to verify correctness after changes.

**Note:** The companion TS project (`the-canonry`) has different rules — never build the TS app without explicit instruction.

## CRITICAL: Forbidden Git Commands

**NEVER run `git reset` in any form.** Not `git reset HEAD`, not `git reset --soft`, not `git reset --hard`, not `git reset` with any arguments. This command destroys work. Use `git restore --staged <file>` to unstage.

**NEVER run `git checkout` to discard changes.** Not `git checkout -- <file>`, not `git checkout .`. Uncommitted changes may belong to another agent working concurrently. Reverse unwanted edits with a targeted `Edit` tool call.

## Project Structure

```
src/
├── Core/                          # Domain logic — no UI, no I/O
│   ├── TheCanonry.Schema/         # Types, IDs, primitives, JSON
│   ├── TheCanonry.Engine/         # World simulation engine
│   ├── TheCanonry.NameForge/      # Procedural name generation
│   └── TheCanonry.Coherence/      # Semantic validation rules
├── Infrastructure/                # External integrations
│   ├── TheCanonry.ApiClients/     # LLM + image API clients
│   ├── TheCanonry.Persistence/    # EF Core SQLite (18 repositories)
│   └── TheCanonry.AwsSync/        # S3 image sync
├── TheCanonry.Illuminator/        # Application logic
│   ├── Enrichment/Prompts/        # 12 prompt builders (TS-parity)
│   ├── Enrichment/Tasks/          # 19 enrichment tasks
│   ├── Chronicle/                 # Chronicle pipeline + V2 prompts
│   ├── Catalog/                   # Analysis, similarity, style assignment
│   ├── ImagePipeline/             # Image generation
│   ├── PrePrint/                  # Export (Markdown, ICML, IDML)
│   ├── BulkOps/                   # Batch operation runners
│   ├── Operations/                # Rename, find/replace, coverage
│   └── Content/                   # Static pages, wiki links
└── TheCanonry.Desktop/            # Avalonia MVVM UI
    ├── Shell/                     # Main window, navigation
    ├── Illuminator/               # 5 feature views
    ├── Archivist/                 # History browser
    ├── Forge/                     # World generation
    ├── DomainEditor/              # Config editor
    └── AwsSync/                   # S3 sync management
```

## Build Configuration

Defined in `Directory.Build.props`:

- **C# 13**, nullable enabled, implicit usings
- **TreatWarningsAsErrors** — no warnings tolerated
- **AnalysisLevel latest-All** — maximum analyzer coverage
- **EnforceCodeStyleInBuild** — style violations are build errors
- **GenerateDocumentationFile** — XML docs generated (CS1591 suppressed for missing doc comments)

The Desktop project additionally suppresses CA1812 (internal class never instantiated) because Avalonia views and ViewModels are instantiated via XAML, DI, and navigation.

## Dependency Direction

```
Desktop → Illuminator → Infrastructure → Core
```

- Core depends on nothing external
- Infrastructure depends on Core only
- Illuminator depends on Core + Infrastructure
- Desktop depends on everything

Never add upward dependencies (e.g., Core depending on Infrastructure).

## TS Prompt Parity — CRITICAL

The 12 prompt builders in `src/TheCanonry.Illuminator/Enrichment/Prompts/` must produce **identical LLM prompts** to their TypeScript counterparts in the web UI. The TS source files live in `the-canonry/apps/illuminator/webui/src/workers/tasks/`.

When modifying any prompt builder:
1. Read the corresponding TS file first
2. Ensure the C# output matches exactly — same wording, same formatting, same sections
3. There are no intentional divergences. Any difference is a bug.

Prompt files and their TS counterparts:

| C# Prompt Builder | TS Source |
|---|---|
| `HistorianPrompts.cs` | `historianReviewTask.ts`, `historianEditionTask.ts`, `historianPrepTask.ts`, `historianChronologyTask.ts` |
| `DescriptionPrompts.cs` | `descriptionTask.ts` |
| `BackportPrompts.cs` | `chronicleLoreBackportTask.ts`, `chronicleLoreBackportSystemPrompt.ts` |
| `EraNarrativePrompts.cs` | `eraNarrativeTask.ts` |
| `ToneRankingPrompts.cs` | `toneRankingTask.ts` |
| `FactCoveragePrompts.cs` | `factCoverageTask.ts` |
| `DynamicsPrompts.cs` | `dynamicsTask.ts` |
| `SummaryRevisionPrompts.cs` | `summaryRevisionTask.ts` |
| `MotifVariationPrompts.cs` | `motifVariationTask.ts` |
| `PaletteExpansionPrompts.cs` | `paletteExpansionTask.ts` |
| `EntityTagImageStylesPrompts.cs` | `entityTagImageStylesTask.ts` |
| `HistorianContextBuilder.cs` | `historianContextBuilders.ts` |

## Code Style

Enforced via `.editorconfig`:

- **Naming:** PascalCase for public members, `_camelCase` for private fields, `I` prefix for interfaces
- **Formatting:** 4-space indent, LF line endings, UTF-8
- **File-scoped namespaces** preferred
- **Expression bodies** preferred for single-line members
- **`var`** preferred when type is apparent

## Patterns

- **MVVM** — `ViewModelBase`, `RelayCommand`, `AsyncRelayCommand` for Desktop views
- **Repository pattern** — 18 repositories behind `CanonryDbContext`, each with CRUD for one entity type
- **Prompt/Task separation** — prompt builders are pure static functions; tasks handle orchestration, API calls, and persistence
- **Task registry** — `TaskRegistry` maps enrichment task names to executors
- **Bulk operations** — `BulkOperationRunner` dispatches typed operations with progress tracking

## API Discipline

Same rules as the TS monorepo:

- **No escape hatches** — don't expose internal objects via getter methods
- **No fallback defaults for required config** — throw on missing required values
- **No deprecated code left for compatibility** — delete old APIs when adding new ones
- **One canonical way** to do each thing — no parallel paths

## Lint Fix Discipline

A lint violation is a signal about code quality, not a warning to silence.

Before fixing any violation, state what the code is doing wrong — not what the linter reports, but the underlying quality problem. The fix must solve the stated problem. A fix that makes the violation disappear without solving the problem is not a fix.

Prohibited shortcuts:
- Prefixing unused variables with `_` instead of removing them
- Splitting functions at arbitrary lines to hit a line count
- Widening types (`| null`, `any`) to accommodate wrong callers
- Adding `#pragma warning disable` as a first response
