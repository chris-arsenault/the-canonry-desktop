# Migration Delta Plan: TS → C# Desktop

Comprehensive inventory of features present in the TS web application (`the-canonry`) that are missing or incomplete in the C# desktop application (`the-canonry-desktop`). Organized by dependency order — later tiers depend on earlier ones.

---

## Tier 1: Data & Style Definitions

These are structural definitions that multiple downstream features depend on. Nothing else works correctly without them.

### 1.1 Narrative Style Definitions (42 styles)

**Status:** Not started — no style data files exist in C#

**What exists in TS:**
- 22 story styles (`StoryNarrativeStyle`) in 5 category files
- 20 document styles (`DocumentNarrativeStyle`) in 3 category files
- Full type definitions in `packages/world-schema/src/narrativeStyles.ts` and `documentStyles.ts`

**What exists in C#:**
- Minimal `NarrativeStyle` record in `PerspectiveTypes.cs` (Id, Name, Format, Description, Tags, ProseGuidance, CraftPosture)
- StoryPromptContext accepts NarrativeInstructions, EventInstructions, ProseInstructions, CraftPosture, StyleName, word/scene counts — but nothing populates them

**What's needed:**
- Full `StoryNarrativeStyle` type: narrativeInstructions, proseInstructions, eventInstructions, craftPosture, titleGuidance, roles (RoleDefinition[]), pacing (totalWordCount, sceneCount ranges), eraNarrativeWeight, tags
- Full `DocumentNarrativeStyle` type: documentInstructions, eventInstructions, craftPosture, titleGuidance, roles, pacing (wordCount range only), eraNarrativeWeight, tags
- `RoleDefinition` type: role, count (min/max), description, selectionCriteria
- All 42 style definitions as JSON data files or embedded resources
- `StyleLibrary` container with lookup: `findNarrativeStyle(id) → NarrativeStyle`
- Loading pipeline: JSON → deserialized styles → available to ChronicleTask and prompt builders

**TS source files:**
- Types: `packages/world-schema/src/narrativeStyles.ts`, `documentStyles.ts`, `style.ts`
- Story data: `narrativeStyleDefaultsClassic.ts`, `...Experimental.ts`, `...Genre.ts`, `...Intimate.ts`, `...Climatic.ts`
- Document data: `documentStyleDefaultsOfficial.ts`, `...Professional.ts`, `...Literary.ts`

**C# files to create/modify:**
- Create: `src/TheCanonry.Schema/NarrativeStyles/` (types + data)
- Modify: `PerspectiveTypes.cs` (expand NarrativeStyle or replace)
- Modify: `ChronicleTask.cs` (load style, populate StoryPromptContext/DocumentPromptContext fields)

**Downstream dependents:** Chronicle generation prompts (1.1 → 2.1), role assignment (1.1 → 3.6), era narrative source tiering (1.1 → 3.8), wizard UI (1.1 → 4.1)

---

### 1.2 WorldContext.WorldDynamics — Structured Loading

**Status:** Partial — type exists in PerspectiveTypes but WorldContext stores flat string

**What exists in TS:**
```typescript
worldDynamics: Array<{
  id: string,
  text: string,
  cultures: string[],    // culture relevance filter
  kinds: string[],        // entity-kind relevance filter
  eraOverrides: Record<string, { text: string, replace: boolean }>
}>
```

**What exists in C#:**
- `WorldDynamic` record in PerspectiveTypes.cs: Id, Text, Cultures, Kinds, EraOverrides — correct type
- `WorldContext.WorldDynamics` is `string?` — flat text, not structured
- `PerspectiveSynthesisInput.WorldDynamics` is `IReadOnlyList<WorldDynamic>?` — correct type
- Gap: config loading doesn't parse structured dynamics from JSON into the typed list

**What's needed:**
- Change `WorldContext.WorldDynamics` from `string?` to `IReadOnlyList<WorldDynamic>`
- Update `ConfigStore` deserialization to parse the structured array from illuminatorConfig.json
- Verify PerspectiveSynthesizer filters dynamics by culture/kind and applies era overrides (the TS does this)

**TS source files:**
- `apps/illuminator/webui/src/lib/perspectiveSynthesizer.ts` — dynamics filtering logic

**C# files to modify:**
- `Config/WorldContext.cs`
- `Config/ConfigStore.cs` (if deserialization needs changes)
- `Chronicle/PerspectiveSynthesis/PerspectiveSynthesizer.cs` (verify filtering)

---

### 1.3 ChronicleRecord — Missing Fields

**Status:** Types partially defined, fields missing from record

**C# ChronicleRecord is missing these fields that TS has:**

| Field | TS Type | Purpose |
|-------|---------|---------|
| `coverImage` | `ChronicleCoverImage` | Scene description, status, generated image ID, visual tags, style assignments |
| `tertiaryCast` | `TertiaryCastEntry[]` | Entities detected in text but not in declared cast |
| `comparisonReport` | `string` | V0/V1 comparison analysis |
| `combineInstructions` | `string` | How to merge V0/V1 |
| `temporalCheckReport` | `string` | Temporal alignment validation |
| `cohesionReport` | `CohesionReport` | Narrative quality validation (6 checks, overall score) |
| `quickCheckReport` | `QuickCheckReport` | Fast unanchored-reference scan |

**TS ChronicleCoverImage type:**
```typescript
{
  sceneDescription: string;
  involvedEntityIds: string[];
  status: "pending" | "generating" | "complete" | "failed";
  generatedImageId?: string;
  visualTags?: string[];
  suggestedArtisticStyleId?: string;
  suggestedCompositionStyleId?: string;
  suggestedColorPaletteId?: string;
  rankedArtisticStyleIds?: string[];
  rankedCompositionStyleIds?: string[];
  rankedColorPaletteIds?: string[];
  secondaryArtisticStyleId?: string;
  secondaryCompositionStyleId?: string;
  secondaryColorPaletteId?: string;
}
```

**TS CohesionReport type:**
```typescript
{
  overallScore: number;  // 0-100
  checks: {
    plotStructure: CheckResult;
    entityConsistency: CheckResult;
    sectionGoals: CheckResult;
    resolution: CheckResult;
    factualAccuracy: CheckResult;
    themeExpression: CheckResult;
  };
  issues: Array<{ severity, description, suggestion }>;
}
```

**TS QuickCheckReport type:**
```typescript
{
  assessment: "clean" | "minor" | "flagged";
  suspiciousPhrases: Array<{ phrase, confidence, reason }>;
}
```

**TS TertiaryCastEntry type:**
```typescript
{
  entityId: string;
  name: string;
  kind: string;
  matchedAs: string;
  matchStart?: number;
  matchEnd?: number;
  accepted: boolean;
}
```

**C# files to modify:**
- `Types/ChronicleTypes.cs` — add ChronicleCoverImage, TertiaryCastEntry, CohesionReport, QuickCheckReport types and fields on ChronicleRecord
- Persistence entity/mapping — add JSON columns for new fields

---

## Tier 2: Chronicle Pipeline

The core generation pipeline. C# currently does single-pass generation; TS has a multi-version flow.

### 2.1 Creative/V1 Prompt Builder

**Status:** Not started — no creative prompt builder exists

**What TS does:**
- `creativePrompt.ts` provides `getCreativeSystemPrompt()` and `buildCreativeStoryPrompt()`
- Same PS outputs, same world data, same cast — different framing
- System prompt emphasizes "find the one detail that makes this story specific"
- Structure presented as "starting shape, not prescription"
- Craft posture still applied but discovery-focused

**What's needed:**
- `CreativeStoryPromptBuilder.cs` — parallel to StoryPromptBuilder with creative framing
- `CreativeDocumentPromptBuilder.cs` if document format has a creative variant (verify in TS)
- Orchestrator method: `RunCreativeGenerationAsync()` with VersionStep.Creative

**TS source files:**
- `apps/illuminator/webui/src/lib/chronicle/v2/creativePrompt.ts`

**C# files to create/modify:**
- Create: `Chronicle/V2/CreativeStoryPromptBuilder.cs`
- Modify: `ChroniclePipelineOrchestrator.cs` — add RunCreativeGenerationAsync

---

### 2.2 Compare & Combine Pipeline Steps

**Status:** Prompts exist (ChroniclePrompts.cs:116-135), not wired into orchestrator

**What TS does:**
- After V0 (structured) and V1 (creative) are generated, a compare step analyzes both
- Compare produces a comparison report stored on the chronicle record
- Combine step takes both versions + comparison and synthesizes a superior V2
- User can also provide combine instructions to guide the merge

**What's needed:**
- `RunCompareAsync()` on orchestrator — takes two version contents, returns comparison report
- `RunCombineAsync()` on orchestrator — takes two versions + comparison + optional instructions, returns combined content
- User prompt builders for compare and combine (currently only system prompts exist)
- ChronicleTask flow: generate → creative → compare → combine → copy-edit

**C# files to modify:**
- `ChroniclePipelineOrchestrator.cs` — add RunCompareAsync, RunCombineAsync
- `ChroniclePrompts.cs` — add user prompt builders for compare/combine
- `Enrichment/Tasks/ChronicleTask.cs` — wire multi-version flow

---

### 2.3 Cover Image Pipeline

**Status:** LlmCallTypes defined (ChronicleCoverImageScene, ChronicleBatchTagCoverImages), no implementation

**What TS does — 4-step pipeline:**
1. **Scene generation**: LLM describes the cover scene from chronicle content → `ChronicleCoverImage.sceneDescription`
2. **Tag**: LLM assigns visual tags and suggests styles for the scene
3. **Assign**: Distribute artistic/composition/color palette styles with balancing across corpus
4. **Generate**: Create image via image service using assigned styles

**TS source files:**
- `workers/tasks/chronicleTask.ts` — `executeCoverImageSceneStep()`, `executeCoverImageStep()`
- `lib/coverImageStyles.ts` — `getCoverImageConfig()`, `getScenePromptTemplate()`

**What's needed:**
- Cover image scene prompt builder
- Cover image scene generation task
- Cover image tag/assign logic (may share with entity image style tagging)
- Cover image generation step in image pipeline
- Bulk operations: BulkCoverImageScene, BulkCoverImageTag, BulkCoverImageAssign, BulkCoverImageGenerate

**C# files to create:**
- `Enrichment/Prompts/CoverImagePrompts.cs`
- `Enrichment/Tasks/CoverImageSceneTask.cs`
- Potentially new bulk ops or extend existing

---

## Tier 3: Validation & Quality

Post-generation validation steps. These run after chronicles are generated and before acceptance.

### 3.1 Quick Check

**Status:** LlmCallType.ChronicleQuickCheck defined with config, no task implementation

**What TS does:**
- Fast scan for unanchored references (entities/events mentioned but not in declared cast)
- Returns assessment ("clean"/"minor"/"flagged") with suspicious phrases and confidence levels
- Lightweight — runs on Haiku

**What's needed:**
- Quick check prompt builder
- Quick check task
- Store result on ChronicleRecord.QuickCheckReport (after adding field per 1.3)

---

### 3.2 Cohesion Report

**Status:** Not implemented

**What TS does:**
- Full narrative quality validation across 6 dimensions: plotStructure, entityConsistency, sectionGoals, resolution, factualAccuracy, themeExpression
- Each check returns pass/warning/fail with description
- Overall score 0-100
- Issues list with severity and suggestions

**What's needed:**
- Cohesion report prompt builder
- Cohesion report task
- LlmCallType entry
- Store result on ChronicleRecord

---

### 3.3 Temporal Check

**Status:** Report field exists in HistorianContextBuilder (line 64), LLM call not implemented

**What TS does:**
- Validates focal era and temporal narrative alignment
- Ensures chronicle correctly reflects timespan and world state of target era
- Report is used by historian annotation prompts (already referenced in HistorianPrompts.cs:950)

**What's needed:**
- Temporal check prompt builder
- Temporal check task
- Store result on ChronicleRecord.TemporalCheckReport

---

### 3.4 Tertiary Cast Detection

**Status:** Not implemented

**What TS does:**
- Scans chronicle text for entity name mentions not in declared primary/secondary cast
- Returns TertiaryCastEntry[] with matched text, positions, entity IDs
- User can accept/reject each detection
- Accepted entries feed into backport progress calculation

**What's needed:**
- Text scanning logic (entity name → text search with position tracking)
- Storage on ChronicleRecord.TertiaryCast
- Backport progress calculation: `computeBackportProgress()` includes accepted tertiary

---

### 3.5 Motif Weaver (Weave Mode)

**Status:** MotifVariationTask exists (vary mode only)

**What TS does:**
- Inverse of variation: incorporates a thematic motif INTO text where it's absent
- Input: full description + target sentence + phrase to weave in
- Output: full description with phrase naturally incorporated
- Uses same full-description approach as variation (per motif-variation-findings.md)

**What's needed:**
- Add weave mode to MotifVariationTask (or create MotifWeaveTask)
- Weave-specific prompt (emphasize organic incorporation, not insertion)
- Weave-specific payload type

**TS source files:**
- `workers/tasks/motifVariationTask.ts` — weave mode handler

---

### 3.6 Interleaved Annotation

**Status:** Not implemented

**What TS does:**
- Annotates chronicles + entities in chronological order, interleaving them
- Prevents concentrated back-references and accumulates corpus voice naturally
- Work queue: chronicles in temporal order, after each chronicle its referenced (unannotated) entities
- Orphan entities cycle through tones: witty, weary, forensic, elegiac, cantankerous
- Two-phase UI: confirmation (shows grouped work list) → execution (progress/results)

**What's needed:**
- Interleaved annotation work-item ordering logic
- Integration with BulkHistorian (or new bulk op) that follows the interleaved order
- UI for confirmation and progress

**TS source files:**
- `lib/db/interleavedAnnotationStore.ts` — `InterleavedAnnotationProgress`, `prepareInterleaved()`

---

## Tier 4: Export & Data

### 4.1 Chronicle JSON Export

**Status:** C# has Markdown and IDML export only, no structured JSON export

**What TS does — export format v1.3:**
```
{
  exportVersion: "1.3",
  chronicle: { id, title, format, focusType, narrativeStyleId, craftPosture, lens, narrativeDirection, model, ... },
  content: string,
  wordCount: number,
  versions: ChronicleVersion[],           // Full generation history
  perspectiveSynthesis: {
    input: { coreTone, narrativeStyleId, constellation, facts, entities },
    output: { brief, facets, narrativeVoice, entityDirectives, temporalNarrative },
    model, generatedAt, tokens, cost
  },
  generationLLMCall: { systemPrompt, userPrompt, model },
  imageRefs: { generatedAt, model, refs[] },
  coverImage: { sceneDescription, involvedEntityIds, status, generatedImageId },
  historianNotes: HistorianNote[],
  historianPrep: string,
  comparisonReport: string,
  combineInstructions: string,
  temporalCheckReport: string
}
```

**What's needed:**
- ChronicleExport type
- `BuildChronicleExport()` function
- JSON serialization to file
- Bulk export (multiple chronicles)

**TS source files:**
- `lib/chronicleExport.ts` — full export type and `buildChronicleExport()`

---

### 4.2 Annotation Export

**Status:** Not implemented

**What TS does:**
- `downloadBulkAnnotationReviewExport(simulationRunId)` — exports all annotations for review
- `downloadBulkToneReviewExport(simulationRunId)` — exports tone assignments for review
- JSON format with entity metadata + annotation content

**TS source files:**
- `lib/chronicleExport.ts` (lines 471, 517)

---

### 4.3 Era Narrative Image Refs

**Status:** EraNarrative has CoverSceneDescription/CoverImageId, but no inline image refs

**What TS does — two ref types:**
1. `ChronicleImageRef` (`type: "chronicle_ref"`) — references an existing chronicle image
2. `EraNarrativePromptRequestRef` (`type: "prompt_request"`) — new scene generated for era narrative
   - Has sceneDescription, status (pending/generating/complete/failed), generatedImageId

**Container:** `EraNarrativeImageRefs` — array of refs + generation metadata

**What's needed:**
- `EraNarrativeImageRef` type (discriminated union of chronicle_ref and prompt_request)
- `EraNarrativeImageRefs` container on EraNarrative
- Image ref generation task for era narratives
- Image generation from era narrative prompt requests

**TS source files:**
- `lib/eraNarrativeTypes.ts` — `EraNarrativeImageRef`, `EraNarrativeImageRefs`

---

## Tier 5: Desktop UI

Avalonia UI features that the TS web app has.

### 5.1 Chronicle Wizard

**Status:** ChronicleView exists (list + detail view), no creation wizard

**What TS has — 5-step wizard:**
1. **StyleStep**: Select narrative style (story or document) + generation sampling
2. **EntryPointStep**: Select entry-point entity with StoryPotentialRadar (5-axis: connections, temporal span, role diversity, event involvement, prominence) + MiniConstellation
3. **RoleAssignmentStep**: Assign entities to narrative roles with EnsembleConstellation visualization, EnsembleHealthBar (diversity metric), FilterChips
4. **DirectionStep**: Free-text narrative direction
5. **ReviewStep**: Confirm all selections

**Key interactions:**
- Role slots come from narrative style's `roles` definition (depends on 1.1)
- Focus type (single/ensemble) derived from role assignments
- Entity selector with search/filter/sort
- Ensemble health metric for cast diversity

**TS source files:**
- `components/ChronicleWizard/WizardContext.tsx` — state management
- `components/ChronicleWizard/steps/StyleStep.tsx`
- `components/ChronicleWizard/steps/EntryPointStep.tsx`
- `components/ChronicleWizard/steps/RoleAssignmentStep.tsx`

---

### 5.2 Historian Edition Comparison

**Status:** Not implemented

**What TS does:**
- Side-by-side diff viewer for entity description versions
- Shows: pre-historian baseline → prior editions → active edition
- Per-version: word count, generation date
- Word-by-word diff highlighting (diffWords)
- Export when 3+ versions exist

---

### 5.3 Catalog Review UI

**Status:** Not implemented (C# has image generation but no review/management UI)

**What TS does:**
- Paginated table of generated images (50 per page)
- Shows: thumbnail, title, tags, imageType, style IDs
- Inline editing of title, tags, imageType, style selections
- Filters: all, has-llm-title, missing-title, missing-tags, missing-style, missing-any
- Sort by newest first

---

### 5.4 Coverage Report UI

**Status:** Not implemented

**What TS does:**
- Per-field completeness analysis: title, tags, artisticStyleId, compositionStyleId, colorPaletteId
- For each field: present, missing, derivable counts
- Operations: deterministic fill, LLM classify fill (with vision mode), title fill, similarity analysis

---

## Summary Matrix

| # | Feature | C# Types | C# Logic | C# UI | Priority |
|---|---------|----------|----------|-------|----------|
| 1.1 | Narrative Style Definitions (42) | Minimal | None | None | **Critical** |
| 1.2 | WorldDynamics Structured Loading | Complete | Partial | N/A | **Critical** |
| 1.3 | ChronicleRecord Missing Fields | Partial | None | None | **Critical** |
| 2.1 | Creative/V1 Prompt Builder | None | None | N/A | **High** |
| 2.2 | Compare & Combine Pipeline | Prompts only | None | None | **High** |
| 2.3 | Cover Image Pipeline | LlmCallTypes | None | None | **High** |
| 3.1 | Quick Check | LlmCallType | None | None | Medium |
| 3.2 | Cohesion Report | None | None | None | Medium |
| 3.3 | Temporal Check | Field ref | None | None | Medium |
| 3.4 | Tertiary Cast Detection | None | None | None | Medium |
| 3.5 | Motif Weaver (Weave Mode) | None | None | N/A | Medium |
| 3.6 | Interleaved Annotation | None | None | None | Medium |
| 4.1 | Chronicle JSON Export | None | None | None | Medium |
| 4.2 | Annotation Export | None | None | None | Low |
| 4.3 | Era Narrative Image Refs | Partial | None | None | Low |
| 5.1 | Chronicle Wizard UI | None | None | None | **High** |
| 5.2 | Historian Edition Comparison | None | None | None | Low |
| 5.3 | Catalog Review UI | None | None | None | Low |
| 5.4 | Coverage Report UI | None | None | None | Low |

---

## Implementation Order Recommendation

**Phase 1 — Foundation (enables everything else):**
1. 1.1 Narrative Style Definitions
2. 1.2 WorldDynamics Structured Loading
3. 1.3 ChronicleRecord field additions

**Phase 2 — Full Chronicle Pipeline:**
4. 2.1 Creative Prompt Builder
5. 2.2 Compare & Combine wiring
6. 5.1 Chronicle Wizard UI (parallel with 4-5)

**Phase 3 — Quality & Validation:**
7. 3.1 Quick Check
8. 3.3 Temporal Check
9. 3.2 Cohesion Report
10. 3.4 Tertiary Cast Detection

**Phase 4 — Cover Images:**
11. 2.3 Cover Image Pipeline (scene → tag → assign → generate)

**Phase 5 — Enrichment & Export:**
12. 3.5 Motif Weaver (weave mode)
13. 3.6 Interleaved Annotation
14. 4.1 Chronicle JSON Export
15. 4.2 Annotation Export

**Phase 6 — UI Polish:**
16. 5.2 Historian Edition Comparison
17. 5.3 Catalog Review
18. 5.4 Coverage Report
19. 4.3 Era Narrative Image Refs
