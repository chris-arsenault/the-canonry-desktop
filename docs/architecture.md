# Architecture

## Layer Overview

```
┌─────────────────────────────────────────────────────────┐
│  TheCanonry.Desktop                                     │
│  Avalonia MVVM — views, viewmodels, navigation, DI      │
├─────────────────────────────────────────────────────────┤
│  TheCanonry.Illuminator                                 │
│  Application logic — enrichment, chronicles, catalog,   │
│  image pipeline, export, bulk operations                │
├──────────────────────┬──────────────────────────────────┤
│  TheCanonry.          │  TheCanonry.     TheCanonry.     │
│  Persistence          │  ApiClients      AwsSync         │
│  EF Core SQLite       │  LLM + Image     S3 sync         │
├──────────────────────┴──────────────────────────────────┤
│  TheCanonry.Schema    TheCanonry.Engine                  │
│  TheCanonry.NameForge TheCanonry.Coherence               │
│  (Core — pure domain logic, no external dependencies)    │
└─────────────────────────────────────────────────────────┘
```

Dependencies flow strictly downward. Core has zero external dependencies beyond the .NET runtime.

## Core Layer

### TheCanonry.Schema

Shared type system for the entire solution.

- **Ids/** — Strongly-typed IDs (`EntityId`, `EraId`, `CultureId`, etc.)
- **World/** — Domain model types (`Entity`, `Relationship`, `Era`, `Culture`, `ExecutionContext`)
- **Primitives/** — Framework primitives (entity kinds, relationship kinds, status values)
- **Config/** — Configuration types for engine and domain setup
- **Domain/** — Domain schema definitions (entity kind configs, relationship kind configs)
- **Json/** — Custom JSON converters for serialization

### TheCanonry.Engine

World simulation engine. Generates procedural world history through alternating growth and simulation phases.

- **Engine/** — `WorldEngine` core loop, `EpochRunner`, `SimulationTickRunner`
- **Templates/** — Template interpreter for entity generation during growth phase
- **Systems/** — System interpreter for relationship formation during simulation phase
- **Pressures/** — Pressure mechanics that drive simulation dynamics
- **Selection/** — Template and target selection algorithms
- **Statistics/** — Population tracking and distribution analysis
- **Coordinates/** — Semantic coordinate system and region mapping
- **Graph/** — Graph utilities and clustering algorithms
- **Rules/** — Declarative rule evaluation
- **Validation/** — Configuration validation
- **Runtime/** — Runtime state management
- **Config/** — Engine configuration types

### TheCanonry.NameForge

Procedural name generation with culture-aware rules.

- **NameForgeService** — Main entry point for name generation
- **NameGenerator** — Core generation algorithm
- **Generation/** — Generation strategies and phoneme handling
- **Types/** — Name component types and culture naming rules
- **Utils/** — String manipulation and phoneme utilities

### TheCanonry.Coherence

Semantic validation of generated worlds. Ensures structural integrity across the entity graph.

- **CoherenceValidator** — Main validator, runs all rule sets
- **Rules/** — Four rule categories:
  - `CrossReferenceRules` — Referential integrity between entities
  - `NumericRangeRules` — Value range validation
  - `OrphanRules` — Disconnected entity detection
  - `PressureRules` — Pressure configuration validation
- **Types/** — Validation result types

## Infrastructure Layer

### TheCanonry.Persistence

EF Core SQLite persistence with repository pattern.

- **CanonryDbContext** — EF Core context with 18 entity sets
- **Entities/** — EF entity models (database-mapped POCOs)
- **Repositories/** — 18 repositories, one per entity type:
  - `EntityRepository`, `RelationshipRepository` — Core graph data
  - `ChronicleRepository`, `EraNarrativeRepository` — Narrative content
  - `ImageRepository`, `StyleLibraryRepository` — Image and style data
  - `SimulationSlotRepository`, `WorldSchemaRepository` — Simulation state
  - `EnrichmentJobRepository`, `HistorianRunRepository` — Job tracking
  - `CostRepository` — API cost tracking
  - `ContentTreeRepository`, `StaticPageRepository`, `PageLayoutRepository` — PrePrint content
  - `DynamicsRunRepository`, `SummaryRevisionRunRepository` — Run history
  - `NarrativeEventRepository`, `TraitPaletteRepository` — Supplementary data

### TheCanonry.ApiClients

HTTP clients for external AI services.

- **Llm/** — LLM API integration
  - `ILlmClient` — Interface for LLM calls (streaming and non-streaming)
  - `ClaudeLlmClient` — Anthropic Claude implementation
  - `LlmRequest`, `LlmResponse`, `LlmChunk` — Request/response types
  - `LlmModel`, `TokenUsage` — Model definitions and usage tracking
- **Images/** — Image generation API integration
  - `IImageClient` — Interface for image generation
  - `FalImageClient` — fal.ai (Flux models)
  - `BflImageClient` — Black Forest Labs
  - `DalleImageClient` — OpenAI DALL-E
  - `WaveSpeedImageClient` — WaveSpeed
  - `ImageProvider` — Provider selection and routing
  - `ImageRequest`, `ImageResult` — Request/response types
- **Shared/** — Common HTTP utilities

### TheCanonry.AwsSync

S3 image synchronization for publishing.

- **S3/** — `IS3Operations` / `S3Operations` — Low-level S3 operations
- **Sync/** — High-level sync services:
  - `ImageSyncService` — Orchestrates sync of images to S3
  - `ManifestManager` — Tracks which images have been synced
  - `ImageVariantGenerator` — Creates size variants for web delivery
  - `CatalogBuilder` — Builds JSON catalog for the web gallery

## Application Layer

### TheCanonry.Illuminator

The main application layer. Contains all business logic for enrichment, chronicle authoring, catalog management, and export.

#### Enrichment Pipeline

Two-part architecture: pure **prompt builders** and **task executors**.

**Prompts/** — 12 static prompt builder classes. Each produces `(systemPrompt, userPrompt)` tuples from domain data. These are pure functions with no side effects. They maintain exact parity with the TS web UI.

| Prompt Builder | Purpose |
|---|---|
| `HistorianPrompts` | Review, edition, chronology, prep — 4 historian task types with tone systems |
| `HistorianContextBuilder` | Context assembly for historian tasks (word budgets, entity snapshots, corpus voice digest) |
| `DescriptionPrompts` | 3-step chain: narrative → visual thesis → visual traits |
| `BackportPrompts` | Chronicle lore backport to entity descriptions |
| `EraNarrativePrompts` | Era narrative thread generation and editing |
| `ToneRankingPrompts` | Tone evaluation and ranking for historian output |
| `FactCoveragePrompts` | Fact coverage analysis and gap identification |
| `DynamicsPrompts` | World dynamics generation |
| `SummaryRevisionPrompts` | Entity summary revision |
| `MotifVariationPrompts` | Motif and theme variation |
| `PaletteExpansionPrompts` | Color palette expansion |
| `EntityTagImageStylesPrompts` | Image style tagging for entities |

**Tasks/** — 19 task executors extending `EnrichmentTaskBase`. Each task:
1. Assembles context from persistence
2. Calls the appropriate prompt builder
3. Sends the prompt to the LLM client
4. Parses the response
5. Persists results

`TaskRegistry` maps task names to executor instances.

#### Chronicle Pipeline

Multi-stage chronicle authoring.

- **V2/** — Second-generation prompt builders (`StoryPromptBuilder`, `DocumentPromptBuilder`, `CopyEditPromptBuilder`) with structured section assembly (`PromptSections`)
- **PerspectiveSynthesis/** — Constellation analysis, fact faceting, narrative voice synthesis, entity directives
- **ChroniclePipelineOrchestrator** — End-to-end orchestration of the multi-step chronicle generation process
- **ChronicleVersionManager** — Version tracking for chronicle iterations
- **ChronicleContextBuilder** — Assembles context for chronicle prompts

#### Catalog

Image catalog management and analysis.

- **CatalogAnalysis** — Statistical analysis of catalog coverage and quality
- **CatalogSimilarity** — Duplicate and near-duplicate detection
- **CatalogDeterministicFill** — Rule-based catalog gap filling
- **CatalogLlmFill** — LLM-assisted catalog enrichment
- **ImageStyleAssignment** — Deterministic style balancing with pair-novelty secondary ranking
- **ForbiddenCombinations** — Style constraint enforcement

#### Image Pipeline

- **ImageGenerationTask** — Image generation via API clients
- **ImagePromptFormatter** — Formats visual thesis + traits into image generation prompts
- **ImageSettings** — Per-entity image generation configuration

#### PrePrint

Content tree assembly and export.

- **ContentTree** — Hierarchical content organization
- **PrePrintStatistics** — Coverage and completeness metrics
- **Export/** — Multi-format export:
  - `MarkdownExporter` + `MarkdownFormatters` — Markdown with ZIP packaging
  - `IcmlExporter` + `IcmlStyles` — Adobe InDesign ICML
  - `IdmlExporter` — Adobe InDesign IDML

#### Bulk Operations

Batch execution of enrichment tasks across multiple entities.

- **BulkOperationRunner** — Generic runner with progress tracking
- Typed operations: `BulkHistorian`, `BulkBackport`, `BulkEraNarrative`, `BulkFactCoverage`, `BulkToneRanking`, `BulkImageStyleTagger`

#### Operations

Corpus-wide operations.

- **EntityRenameService** — Rename entities with cascading updates
- **CorpusFindReplace** — Find and replace across all narrative content
- **EntityCoverageAnalysis** — Analyze enrichment coverage across the entity graph

#### Content

- **StaticPageService** — Static page generation with template rendering
- **WikiLinkService** — Resolve and validate wiki-style `[[entity]]` links in narrative text

## UI Layer

### TheCanonry.Desktop

Avalonia 11.3 MVVM application.

**Shell/** — Main window with sidebar navigation. `ShellViewModel` manages active view, `NavigationService` handles view transitions.

**Feature Views** — Each feature module follows the same pattern:
- `*View.axaml` — XAML layout
- `*View.axaml.cs` — Code-behind (minimal, only Avalonia-specific logic)
- `*ViewModel.cs` — Business logic, commands, state

| Module | Views | Purpose |
|---|---|---|
| Illuminator | `IlluminatorView`, `EntityBrowserView`, `ChronicleView`, `CatalogView`, `ImageCurationView` | Main enrichment workspace |
| Archivist | `ArchivistView` | History and lore browser |
| Forge | `ForgeView` | World generation |
| DomainEditor | `DomainEditorView` | Domain configuration editing |
| AwsSync | `AwsSyncView` | S3 sync management |

**Shared/** — MVVM infrastructure:
- `ViewModelBase` — `INotifyPropertyChanged` base
- `RelayCommand` / `AsyncRelayCommand` — `ICommand` implementations
- `NavigationService` — View routing
- `NavigationItem` — Sidebar item model
- `WindowManager` — Window lifecycle management

## Data Flow

### Enrichment Task Execution

```
ViewModel → EnrichmentQueue → TaskRegistry → EnrichmentTaskBase
    → Prompt Builder (pure function)
    → ILlmClient (API call)
    → LlmResponseParser
    → Repository (persist)
```

### Image Generation

```
ViewModel → ImageGenerationTask
    → ImagePromptFormatter (visual thesis + traits → prompt)
    → IImageClient (API call)
    → ImageRepository (persist blob)
    → AwsSync (optional S3 upload)
```

### Chronicle Pipeline

```
ViewModel → ChroniclePipelineOrchestrator
    → PerspectiveSynthesizer (constellation → directives)
    → V2 Prompt Builders (story/document/copy-edit)
    → ILlmClient (multi-step generation)
    → ChronicleVersionManager (version tracking)
    → ChronicleRepository (persist)
```
