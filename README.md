# The Canonry Desktop

Native desktop companion for [The Canonry](https://github.com/tsonu/the-canonry) world-generation framework. Built with Avalonia UI, .NET 10, and C# 13.

The web-based Illuminator UI drives creative authoring; this desktop application provides the same enrichment, chronicle, and image pipelines as a native app with local SQLite persistence and direct OS integration.

## Tech Stack

| Component | Version |
|-----------|---------|
| .NET SDK | 10.0 |
| C# | 13 |
| Avalonia UI | 11.3 |
| EF Core (SQLite) | 10.0 |
| DI | Microsoft.Extensions.DependencyInjection 10.0 |

## Project Structure

```
the-canonry-desktop/
├── src/
│   ├── Core/                              # Domain logic — no UI, no I/O
│   │   ├── TheCanonry.Schema/             # Shared types, IDs, primitives, JSON converters
│   │   ├── TheCanonry.Engine/             # World simulation engine, templates, systems
│   │   ├── TheCanonry.NameForge/          # Procedural name generation
│   │   └── TheCanonry.Coherence/          # Semantic validation rules
│   │
│   ├── Infrastructure/                    # External integrations
│   │   ├── TheCanonry.ApiClients/         # LLM (Claude) and image API clients (fal, BFL, DALL-E, WaveSpeed)
│   │   ├── TheCanonry.Persistence/        # EF Core SQLite — DbContext + 18 repositories
│   │   └── TheCanonry.AwsSync/            # S3 image sync with manifest and variant generation
│   │
│   ├── TheCanonry.Illuminator/            # Application layer — enrichment, chronicles, catalog
│   │   ├── Enrichment/                    # LLM enrichment pipeline
│   │   │   ├── Prompts/                   # 12 prompt builders (exact parity with TS web UI)
│   │   │   └── Tasks/                     # 19 enrichment task executors
│   │   ├── Chronicle/                     # Chronicle pipeline — V2 prompts, perspective synthesis
│   │   ├── Catalog/                       # Catalog analysis, similarity, image style assignment
│   │   ├── ImagePipeline/                 # Image generation tasks and prompt formatting
│   │   ├── PrePrint/                      # Content tree, export (Markdown, ICML, IDML)
│   │   ├── BulkOps/                       # Bulk operation runners for batch enrichment
│   │   ├── Operations/                    # Rename, find/replace, coverage analysis
│   │   ├── Content/                       # Static pages, wiki link resolution
│   │   ├── Config/                        # Image settings, LLM call config
│   │   └── Types/                         # Shared types for all Illuminator subsystems
│   │
│   └── TheCanonry.Desktop/               # Avalonia UI layer — MVVM views and navigation
│       ├── Shell/                         # Main window, navigation frame
│       ├── Illuminator/                   # Entity browser, chronicle, catalog, image curation views
│       ├── Archivist/                     # History and lore browser view
│       ├── Forge/                         # World generation view
│       ├── DomainEditor/                  # Domain configuration editor view
│       ├── AwsSync/                       # S3 sync management view
│       └── Shared/                        # ViewModelBase, RelayCommand, NavigationService
│
├── tests/                                 # 8 test projects, ~460 tests
│   ├── TheCanonry.Schema.Tests/
│   ├── TheCanonry.Engine.Tests/
│   ├── TheCanonry.NameForge.Tests/
│   ├── TheCanonry.Coherence.Tests/
│   ├── TheCanonry.ApiClients.Tests/
│   ├── TheCanonry.Persistence.Tests/
│   ├── TheCanonry.AwsSync.Tests/
│   └── TheCanonry.Illuminator.Tests/
│
├── domain/default-project/                # JSON domain configuration (entity kinds, eras, etc.)
├── web/                                   # Embedded web views (pics gallery, viewer)
├── Directory.Build.props                  # Shared build config (C# 13, strict analysis)
├── .editorconfig                          # Code style — naming, formatting, analyzer severity
└── TheCanonry.slnx                        # Solution file
```

## Build & Test

```bash
# Restore and build
dotnet build

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/TheCanonry.Illuminator.Tests/

# Run the desktop app
dotnet run --project src/TheCanonry.Desktop/
```

## Architecture

Four-layer architecture with strict dependency direction:

```
Desktop (Avalonia MVVM)
    ↓
Illuminator (Application logic)
    ↓
Infrastructure (Persistence, API clients, AWS)
    ↓
Core (Schema, Engine, NameForge, Coherence)
```

- **Core** has zero external dependencies (except the .NET runtime). Domain types, simulation engine, name generation, and validation live here.
- **Infrastructure** depends on Core. Provides EF Core persistence, HTTP-based LLM and image API clients, and S3 sync.
- **Illuminator** depends on Core + Infrastructure. Contains all enrichment prompt builders, task executors, chronicle pipeline, catalog analysis, and export functionality. This is the main application layer.
- **Desktop** depends on everything. Thin Avalonia MVVM shell that wires views to Illuminator services via DI.

### TS Parity

The Illuminator prompt builders (`src/TheCanonry.Illuminator/Enrichment/Prompts/`) produce **identical LLM prompts** to their TypeScript counterparts in the web UI (`the-canonry/apps/illuminator/webui/src/workers/tasks/`). This is tested and enforced — divergences are bugs.

### Key Patterns

- **MVVM** with `ViewModelBase`, `RelayCommand`, `AsyncRelayCommand` in Desktop layer
- **Repository pattern** — 18 repositories behind `CanonryDbContext` (SQLite)
- **Task-based enrichment** — `EnrichmentTaskBase` → concrete tasks, dispatched via `TaskRegistry`
- **Prompt/Task separation** — prompt builders are pure functions (static classes), tasks handle orchestration
- **Bulk operations** — `BulkOperationRunner` with typed operation definitions

## Domain Configuration

The `domain/default-project/` directory contains JSON files that define the world's domain:

- Entity kinds, relationship kinds, cultures, status values
- Era definitions with template weights
- Pressures, generators, systems, actions
- Culture-specific naming rules

These files are loaded at runtime and interpreted by the Engine.
