# Happy Campfire Friends VR — Complete Build System Reference

**Document type:** Technical history, recovered design decisions, Unity component inventory, placement-rule specification, troubleshooting record, and integration roadmap  
**Compiled:** October 28, 2026  
**Coverage:** March 2026 construction prototype; March 26–27 alignment diagrams and solver notes; October 27, 2026 reconstruction blueprints v0.1–v0.3; implications of the latest abandoned-camp security direction.  
**Project names used in the records:** *Happy Campfire Friends*, *Campfire Friends VR*, *Camp Ashwood*; *The Long Road to Camp Ashwood* is a related narrative property, not the name of the build-system codebase.  
**Important:** This is a **reconstruction and design history**, **not a verified dump of the latest Unity project**. The original complete `.cs` files are not available in the supplied materials. The report distinguishes features built/tested in conversations from code patches suggested, design choices proposed, and functionality that remains unverified.

---

## Contents

1. [Scope, sources, and evidence labels](#1-scope-sources-and-evidence-labels)
2. [What the construction system is for](#2-what-the-construction-system-is-for)
3. [The current technical contract](#3-the-current-technical-contract)
4. [Evolution of the placement mechanic](#4-evolution-of-the-placement-mechanic)
5. [Unity files, data types, and responsibilities](#5-unity-files-data-types-and-responsibilities)
6. [Prefabs, Blender authoring, bounds, and pivots](#6-prefabs-blender-authoring-bounds-and-pivots)
7. [Grid and coordinate model](#7-grid-and-coordinate-model)
8. [Ray targeting and surface classification](#8-ray-targeting-and-surface-classification)
9. [Complete face, edge, corner, and tangent solver](#9-complete-face-edge-corner-and-tangent-solver)
10. [Vertical stacking and roof construction](#10-vertical-stacking-and-roof-construction)
11. [Rotation and occupied geometry](#11-rotation-and-occupied-geometry)
12. [Placement validation, support, and collisions](#12-placement-validation-support-and-collisions)
13. [Preview, palette, controls, and delete mode](#13-preview-palette-controls-and-delete-mode)
14. [Inventory, building costs, health, repair, and removal](#14-inventory-building-costs-health-repair-and-removal)
15. [World state, persistence, multiplayer, and story hooks](#15-world-state-persistence-multiplayer-and-story-hooks)
16. [Observed defects and debugging history](#16-observed-defects-and-debugging-history)
17. [Proposed maintainable architecture](#17-proposed-maintainable-architecture)
18. [Build-system use across the three gameplay directions](#18-build-system-use-across-the-three-gameplay-directions)
19. [Configuration and acceptance test matrix](#19-configuration-and-acceptance-test-matrix)
20. [What is confirmed, proposed, or unresolved](#20-what-is-confirmed-proposed-or-unresolved)
21. [Suggested rebuild and verification sequence](#21-suggested-rebuild-and-verification-sequence)
22. [Technical appendices](#22-technical-appendices)
23. [Source inventory](#23-source-inventory)

---

## 1. Scope, sources, and evidence labels

### 1.1 What is included

This document covers the **entire discussed construction subsystem**: selecting modular prefabs; resource/recipe metadata; surface and physics targeting; modular lattice; face attachment; rotated bounds; edge and corner alignment; top/middle/bottom vertical intent; wall stacking; roof support and roof-to-roof extension; preview and invalid feedback; collision and support checks; ground placement; input mappings; place/delete; refunds, durability and repairs; layer setup; known compiler and regression issues; suggested services and save schemas; and how construction will function in the October 2026 security-guard game premise.

The gathering, clock, night, scare, radio, and multiplayer systems are covered **where they interface with construction**. They are not represented as complete implemented gameplay systems.

### 1.2 Evidence labels used throughout

| Label | Meaning |
|---|---|
| **[TESTED / REPORTED]** | The user or the earlier exchange reported that the behavior worked in a particular test, not that all cases were regression-tested. |
| **[HISTORICAL IMPLEMENTATION]** | Scripts, public members, and operations were described or appeared in earlier code discussions; their latest exact code is unavailable. |
| **[DECIDED DESIGN]** | An explicit rule or intended UX was settled in discussion. This does not by itself prove a working implementation. |
| **[PATCH PROPOSED]** | A concrete code or inspector fix was recommended; there is no reliable evidence it was merged and retained. |
| **[LATER BLUEPRINT]** | An October 2026 architectural reconstruction or game-design proposal, not a recovered March source file. |
| **[OPEN / VERIFY]** | Conflicting information, missing source, or an undetermined rule. |

**Do not confuse an assistant-generated full-script response with proof that the script compiled, was copied into the Unity project, or survived subsequent revisions.** The known build system experienced regressions as different placement solutions were tested.

### 1.3 Key primary materials

- `Build Alignment Update.txt` — explicit face-contact, tangent-alignment, and 0.5 m lattice model.
- `align.png`, `surface.png`, `snap.png`, `increment surface alignment.png` — user-drawn placement-intent diagrams, reproduced as linked reference assets in the companion ZIP.
- March 23–28, 2026 build-system discussions: `BuildManager`, `BuildPieceData`, `BuildFootprint`, `PlacedBuildPiece`, `PlayerInventory`, layer assignments, invalid previews, collision, vertical stacking, roof support, and corner-placement revisions.
- October 27, 2026 `Campfire_Friends_VR_Systems_Blueprint_v1.md` — technical reconstruction baseline and separation into services.
- October 27, 2026 `Campfire_Friends_VR_Build_Resource_Night_Systems_v02.md` — single-player ranger-driven design pass.
- October 27, 2026 `Campfire_Friends_VR_Security_Camp_Systems_v03.pdf` — **latest story/application direction**, involving 1–4 guards securing a pre-existing abandoned camp.
- The provided `camper.txt` describes *The Long Road to Camp Ashwood* story framework. It does **not** specify the build-system implementation.

### 1.4 Absolute reconstruction boundary

The historical discussion identifies certain class names, enum names, values, snippets, algorithms, and bug reports. **It does not provide a complete recoverable latest `BuildManager.cs` or the other referenced `.cs` files.** This document therefore does not invent a complete original code listing or claim the new blueprint services already exist.

---

## 2. What the construction system is for

### 2.1 Core interaction

The player selects an authored modular construction piece, points at valid terrain or an existing structure, sees a ghost preview, rotates the part as needed, and places it once validation succeeds. The same system must support removing placed pieces, costs and refunds, health/damage, and contextual repair.

**Player experience goal:** *The player expresses intent, not centimeter-perfect accuracy.* Aiming at a face, approximate wall band, or structural edge should let the placement solver choose an exact modular result. This emerged from repeated cases where pieces visually almost touched but could not form flush corners or roof seams.

### 2.2 Intended pieces across discussions

Original sandbox / survival kit:

- Floor, foundation, basic block.
- Wall; straight sections placed adjacently, turned into corners, and stacked vertically.
- Roof sections / roof panels, including multiple panels making a continuous room covering.
- Doors / doorway sections.
- Fencing and perimeter pieces.
- Potential utility and decorative pieces; not a locked first-pass catalog.

Latest security-camp first kit, **proposed** in v0.3:

- Fence panel closing a breached perimeter.
- Gate / gate-latch repair at an approved gate anchor.
- Boarding plank at an approved window or doorway anchor.
- Door brace at an approved existing door anchor.
- Portable/work light placed on ground, wall, or approved connector.
- Small modular watch platform **later**.

### 2.3 A reusable core, not three separate build engines

The same geometric/transactional foundation should be able to support an open building sandbox, a supervised ranger-driven campsite, and targeted repairs to an authored security site. Game modes change **piece catalogs, allowed anchors, economic rules, objectives, and permissions**, not the meanings of alignment, occupancy, durability, or resource transactions.

---

## 3. The current technical contract

The strongest settled geometry rule, stated explicitly in `Build Alignment Update.txt`, is:

> **Face adjacency + bounds alignment + 0.5 m lattice quantization.**

It is **neither** unrestricted freeform snapping **nor** a simplistic snap of the object's pivot to the nearest grid point.

| Aspect | Intended behavior | Status |
|---|---|---|
| Shared modular unit | **0.5 meters** | **[DECIDED DESIGN]** |
| Basic rotation | Around **world/local Y only**, quarter turns **0°, 90°, 180°, 270°** | **[DECIDED DESIGN]** |
| Surface targeting | Raycast to terrain, authored build surfaces, or placed pieces | **[HISTORICAL IMPLEMENTATION]** |
| Primary attachment | Flush **face-to-face** using source/candidate logical bounds | **[DECIDED DESIGN]** |
| Secondary alignment | Nearest meaningful **min / center / max** bounds planes on tangent axis | **[DECIDED DESIGN]** |
| Vertical placement | Top / middle / bottom intent for side hits; top-face support for stacking | **[DECIDED DESIGN]** |
| Final normalization | Quantize **resolved** placement to compatible 0.5 m planes without breaking contact | **[DECIDED DESIGN]** |
| Preview | Visible valid/invalid ghost before spending resources | **[HISTORICAL IMPLEMENTATION]** |
| Validation | Reject blocked, unsupported, incompatible, or unaffordable placements | **[HISTORICAL IMPLEMENTATION]** and blueprint refinement |
| Placement confirmation | Confirm once; revalidate; then spend/create/register once | **[LATER BLUEPRINT]**; historical input present |
| Deletion | Target existing build, highlight it, remove it, handle refunds | **[HISTORICAL IMPLEMENTATION]**; transaction details not fully verified |
| Co-op authority | Shared world, atomic stock/cell claims, local previews for each player | **[LATER BLUEPRINT]** |

**Crucial invariant:** Exact contact must survive lattice adjustment. A raw hit point is noisy and is **not** the coordinate to quantize directly.

---

## 4. Evolution of the placement mechanic

This chronology matters because some older advice was superseded and should not be accidentally revived during rebuilding.

### 4.1 March 23–24: First grid-based placing and roof/stacking limitations

Early `BuildManager` discussions used a **0.5 m grid**, modular prefabs, placement/deletion modes, inventory checks, and raycast targeting. Desktop test controls were introduced. `BuildPlacementMode` variants in an early design pass included `GroundOnly`, `Stackable`, and `TopSurfaceOnly`.

**Reported problem:** Roof pieces behaved like ground-placed items. Trying to place pieces on top of an existing wall/block failed or did not resolve to the correct top height. Early proposals included recognizing the top hit face, using the collider's top `bounds.max.y` for the candidate bottom instead of rounding the hit-point Y, and accepting `PlacedBuild` hits in the raycast mask.

### 4.2 March 24–25: Grid placement alone causes seams

Rotated wall pieces were moving in coarse increments; edges would not meet. A player aimed at a wall's side or corner and the resulting candidate was too far inward or too far outward. The 0.5 m grid was not inherently the problem: **the alignment reference was wrong**.

An alternative was explored: move objects within a grid without snapping on X/Z and reserve snapping for vertical stacking. That was a **design exploration**, not the final placement contract. It raised unresolved questions about roof panels and future modular consistency.

The rule subsequently shifted toward **bounds face contact first**: use the existing piece's actual face and the new piece's opposite face, especially after 90° rotation.

### 4.3 March 25: Prefab footprint, overlap and support corrections

A dedicated `BuildBounds` or `SnapBounds` child with `BuildFootprint` and a `BoxCollider` was discussed to avoid using arbitrary render-mesh bounds. Collider contact and support were treated as different checks.

When adjacent faces merely touched, strict `OverlapBox` checks sometimes marked placement invalid. A small overlap test inset/skin and support ray inset were proposed (recorded in Section 12). **Those specific tuning values are proposals, not an independently verified final configuration.**

At different points the preview was reported restored, roof placement at an upper wall worked, and ground/corner placement improved; subsequent screenshots still revealed X/Z and edge/corner defects.

### 4.4 March 26: Multi-stage solver and cleanup

The alignment model became a staged solver:

1. Determine dominant ray-hit face.
2. Read approximate vertical intent (top/middle/bottom on a side face).
3. Solve exact touching-axis contact using rotated bounds.
4. Choose tangent-axis structural alignment (min/center/max).
5. Quantize the **resolved** result on compatible 0.5 m planes.
6. Validate and show preview.

A cleanup identified apparently obsolete helpers after moving to `buildUnit`-based placement: `GetGridSize`, `GetSupportYFromHit`, `SnapValue`, and `topFaceDotThreshold`. The user reported making a first cleanup pass. **Recheck actual references before removing any member in a current source snapshot.**

### 4.5 March 26–27: Lattice contact preservation and corner intent

A generic round-the-position-to-grid helper was not enough; it could undo face contact, particularly when pivots, rotation, or half-cell thicknesses differed. The discussion introduced **contact-preserving X/Y/Z quantization** as a proposed refinement. `tangentAutoAlignMaxDistance` moved through candidate values, including `0.51` and `0.76` meters, reflecting tuning rather than a single permanently verified constant.

The final known wall-corner issue was more specific: a simple nearest-center candidate sometimes won when the player clearly intended a min/max corner. The subsequent suggestion was to compute **tangent intent** (`Min`, `Center`, `Max`) from aim position and apply a selection penalty to incompatible structural candidates. This **was proposed after the user reported working placement with lingering corner quirks**; do not mark it as a confirmed final fix.

### 4.6 October 27: Architectural and gameplay reframing

Three design blueprints followed the prototype:

- **v0.1:** modular build/harvest/day-night survival reconstruction; originally light, age-appropriate forest threats.
- **v0.2:** single-player woodland builder with a remote ranger, gradual psychological horror, and authored night incidents rather than required wave combat.
- **v0.3 (latest story premise):** **1–4 security guards** watching a notorious abandoned summer camp that will be demolished, with salvage/reinforcement rather than mandatory cabin construction; 0.5 m anchored modular building retained.

**The gameplay premise changed, but the settled geometric placement rules did not.** The later service-oriented architecture is **proposed** and does not prove the March single-script `BuildManager` was refactored.

---

## 5. Unity files, data types, and responsibilities

### 5.1 Files and classes named in historical discussions

| Script / type | Known role or reference | Status / caveat |
|---|---|---|
| `BuildManager.cs` | High-level controller for selection, raycast, preview, solving, validation, place, delete, and input during prototype | **[HISTORICAL IMPLEMENTATION]**; full final contents missing. |
| `BuildPieceData.cs` | `ScriptableObject` per-piece asset, prefab, costs, surface, rotation, health, support settings | Partial exact source was provided in a March chat. |
| `BuildFootprint.cs` | Dedicated footprint / logical bounds provider, generally associated with a child `BoxCollider` | **[HISTORICAL IMPLEMENTATION]**; exact latest code unavailable. |
| `PlacedBuildPiece.cs` | Marker/state on instantiated pieces; associates placed instance with its piece definition, enabling support, delete, durability | **[HISTORICAL IMPLEMENTATION]**; `Data` vs `PieceData` mismatch was reported. |
| `BuildMode` | Mode control: `None`, `Place`, `Delete` in one patch | **[HISTORICAL IMPLEMENTATION]**. |
| `PlayerInventory` | Stores inventory and checks/consumes construction costs; harvesting adds resources | **[HISTORICAL IMPLEMENTATION]**; exact API beyond discussed references unverified. |
| `InventoryCost` | Array item describing resources required to construct a piece | **[HISTORICAL IMPLEMENTATION]**; exact field names not recovered. |
| `BuildPlacementMode` | Early `GroundOnly`, `Stackable`, `TopSurfaceOnly` classifications | **[HISTORICAL DESIGN]**; not proven retained after introduction of `SupportMode`. |
| `BuildSurfaceType` | Actual discussed enum: `Ground`, `GridOnly` | **[VISIBLE SOURCE FRAGMENT]**. |
| `SupportMode` | Actual discussed enum: `Default`, `Roof`, `Wall`, `Floor`, `FreePlace` | **[VISIBLE SOURCE FRAGMENT]**. |

### 5.2 `BuildPieceData` — visible source fragment

The following members were visible in the supplied conversation excerpt. **This is a partial class, not a complete replacement file.**

```csharp
public enum BuildSurfaceType
{
    Ground,
    GridOnly
}

public enum SupportMode
{
    Default,
    Roof,
    Wall,
    Floor,
    FreePlace
}

[CreateAssetMenu(fileName = "BuildPiece_", menuName = "Campfire/Build/Build Piece Data")]
public class BuildPieceData : ScriptableObject
{
    public SupportMode supportMode = SupportMode.Default;

    [Header("Identity")]
    public string pieceId;
    public string displayName;

    [Header("Prefab")]
    public GameObject prefab;
    public Sprite icon;

    [Header("Build")]
    public InventoryCost[] buildCosts;
    public bool snapToGrid = true;
    public float gridSize = 1f;
    public BuildSurfaceType surfaceType = BuildSurfaceType.Ground;

    [Header("Rotation")]
    public bool yRotationOnly = true;
    public float rotationStep = 90f;

    [Header("Durability")]
    [Min(1)] public int maxHealth = 100;
    public bool refundOnDelete = true;
    // The visible excerpt stops here.
}
```

**Subtle but important:** `gridSize = 1f` is a **per-piece default in the visible data fragment**, while the final alignment model specifies a **shared `buildUnit = 0.5f`**. Those two values must not be silently treated as equivalent. When rebuilding, determine whether `gridSize` is intentionally used for something other than the authoritative global lattice or is an obsolete legacy setting. Likewise, `snapToGrid` and `surfaceType` should be audited against the chosen solver; no blanket assumption that they are dead fields is justified without current code.

`BuildSurfaceType.GridOnly` and `SupportMode.FreePlace` have **names**, but their precise validation semantics were not fully established in the supplied fragment. The existence of the enum does not prove every mode was fully functional.

### 5.3 `PlacedBuildPiece` and the `Data` compiler problem

A reported compile error was:

```text
'PlacedBuildPiece' does not contain a definition for 'Data'
```

An earlier patch or call site referred to `placed.Data`, while another implementation used a `PieceData` reference/property and an `Initialize(...)` method. The resolution is a **consistent public contract**, not arbitrary aliases without auditing usages. The user-provided class source is not available to prove which property became final.

Information a placed instance needs, whether on its component or in a registry:

- Reference to definition (`BuildPieceData` / stable `pieceId`).
- Unique instance ID (for future persistence and network authority).
- World transform / grid anchor and yaw quarter-turn.
- Durability (`maxHealth`, current health).
- Support/attachment provider IDs when tracked.
- Ownership/permissions where co-op security repair requires them.

The last four items represent **later architecture expectations**, not all proven historical fields.

### 5.4 `BuildFootprint` and bounds providers

`BuildFootprint` is intended to describe the **logical construction volume** separately from decorative meshes. Earlier setup discussions used `Reset()` / `OnValidate()` to bind or inspect a `BoxCollider`; whether those methods remain in the real file is unverified.

A robust logical-bounds API should answer: local center/size; rotated world bounds; face planes; occupancy cells; optionally connection surfaces. **It should not rely on a renderer's decorative overhang to decide whether a roof can fit.**

### 5.5 `BuildManager` historical areas of responsibility

Earlier full-script drafts combined:

- Input mapping / build and delete modes.
- Selected `BuildPieceData` and palette slots.
- `Camera` / controller ray origin and max placement distance.
- Layer masks (`Ground`, `BuildSurface`, and placed-build layer).
- Pointer `LineRenderer`.
- Preview instance, preview renderer material swaps (valid green/invalid red), and delete highlight.
- Rotating the current candidate.
- `CalculatePlacementPosition()` and later `SolvePlacement()`-style logic.
- Support classification and collision checks.
- `PlayerInventory` affordability and cost deduction.
- Instantiate/mark placed pieces, assign placed layer recursively.
- Deletion and optional resource refunds.

A later recommendation was to **stop continually patching one giant `CalculatePlacementPosition()`** and make a small solver pipeline or separate services. That is an architectural recommendation, not evidence that the refactor happened.

---

## 6. Prefabs, Blender authoring, bounds, and pivots

### 6.1 Unit convention

- Model at **real-world Unity meters**; keep mesh import scale and transforms predictable.
- The universal modular subdivision is **0.5 m**. Dimensions intended to participate in the simple lattice should be multiples of 0.5.
- A piece can occupy one or many cells per axis; **0.5 m does not mean every piece is a 0.5 m cube**.
- The visual piece can slope, protrude, or decorate outside the logical construction footprint if that is deliberately supported by its authoring data.

### 6.2 Pivot convention and tested dimensions

The working modeling convention was **bottom-center prefab pivot** for simple blocks, walls, and roof test pieces. A dedicated bounds child should have its center offset above that pivot.

Historical references, **not universal asset specifications**:

| Piece | Logical bounds size (X,Y,Z) | Bounds local center | Purpose |
|---|---|---|---|
| Test wall | `(1, 2, 0.5)` m | `(0, 1, 0)` m | Two-meter wall standing on Y=0. |
| Test roof panel | `(1, 0.5, 1)` m | `(0, 0.25, 0)` m | Low-height logical roof piece with possible sloped visual mesh. |
| Earlier test wall/block note | About `1 × 1 × 0.5` mesh/collider in one earlier patch | Bottom pivot assumed | This was part of a separate earlier test/configuration and must not be mistaken for the 2 m tall wall standard. |
| Diagram example | `(1.5, 5.5, 1)` m | Unspecified | Demonstrates `3 × 11 × 2` half-meter logical cells. |

Wall footprint example:

```text
WallRoot  (position is the intended bottom-center anchor)
├── VisualMesh                 // Blender model / material
├── BuildBounds                // child with BoxCollider / BuildFootprint
│   └── BoxCollider.center = (0, 1, 0)
│       BoxCollider.size   = (1, 2, 0.5)
└── [optional placement connectors / indicators]
```

Do not assume the collider's physical blocking shape, the renderer's bounds, and the **logical occupancy footprint** must be identical. For debug they may initially match; for overhanging/sloped roofs they often should differ.

### 6.3 Blender and Unity checklist

1. Model to target dimensions and **apply mesh/object transforms** before exporting.
2. Fix pivot at bottom center as chosen for the simple modular kit.
3. Ensure Unity instance/root scale is intentional, ideally `(1,1,1)`.
4. Add the child `BuildBounds` / `SnapBounds`; size it to the desired logical cells, not incidental bevels or foliage.
5. Ensure the `BoxCollider` is usable for face tests where intended and is not accidentally included in preview self-hits.
6. Verify prefab rotation at **0°, 90°, 180°, 270°**; reassess the world-axis dimensions after rotation.
7. Confirm that the top of a wall physically/logically lands at the elevation at which a roof is supposed to rest.
8. Confirm roof visuals tile end-to-end: a correct logical footprint cannot fix a mesh exported with unexpected padding or eaves.
9. Validate that logical dimensions are integer multiples of 0.5 and show warnings for exceptions.
10. Establish explicit connectors for special cases: sloped roof seams, windows, gate repairs, and wall-mounted fixtures.

### 6.4 Why bounds correctness matters so much

Several apparent “grid errors” are actually mistakes in prefab dimensions, bounds centers, or rotated extents. For example, a wall 1 m wide and 0.5 m thick has **different contact offsets after 90° yaw**. Any solver using the unrotated extents or prefab center alone will push a rotated wall by the wrong amount.

---

## 7. Grid and coordinate model

### 7.1 Shared lattice

**Settled rule:** `buildUnit = 0.5f` meters, shared across a build region.

```csharp
// Illustrative helper matching the alignment note.
private const float BuildUnit = 0.5f;
private float SnapToBuildUnit(float value)
{
    return Mathf.Round(value / BuildUnit) * BuildUnit;
}
```

The alignment note also suggested a serialized value:

```csharp
[SerializeField] private float buildUnit = 0.5f;
```

This is a **helper**, not by itself the entire placement algorithm. It must operate in a coordinate frame / phase compatible with the structural planes.

### 7.2 Lattice cells and size example

For `(1.5, 5.5, 1.0)` meters:

```text
X: 1.5 / 0.5 =  3 cells
Y: 5.5 / 0.5 = 11 cells
Z: 1.0 / 0.5 =  2 cells
```

Integer-cell occupancy allows deterministic collision and save/restore. The later blueprints propose a fixed world origin and `GridSpace` conversion between world positions and integer cell coordinates.

### 7.3 Four related but different concepts

1. **Raw ray hit** — an approximate intent location, noisy and continuously variable.
2. **Face-contact pose** — an exact relative contact solution between the target and candidate logical bounds.
3. **Structural alignment plane** — one of the target's min/center/max planes or a designated connector plane.
4. **Cell/lattice placement** — the final modular pose and occupied cells, reconciled to 0.5 m planes.

Conflating these is the central cause of the historic placement drift.

### 7.4 Contact preservation versus blind snapping

A solved touching relationship should maintain `candidate.min == target.max` or `candidate.max == target.min` on the relevant axis within tolerance. Quantize relative to the **valid contact plane**; if snapping a candidate's transform/pivot would introduce penetration or a gap, do **not** apply that blind snap.

**Engineering implication (not a recovered code statement):** With center-pivot pieces of odd half-meter thickness, a valid pivot can fall between the global half-meter planes while both physical faces align correctly. This makes it safer to quantize **logical bounds/anchor planes** and derive the transform than to require all pivots to be exact lattice multiples. A fixed bottom-center convention reduces but does not remove all such cases.

### 7.5 Why the requested grid was not reduced

A smaller generic step can make movement appear smoother while concealing structural inconsistencies. The earlier discussion explicitly retained the 0.5 m modular unit; **0.25 m** was discussed as only necessary for a genuinely quarter-meter asset dimension, not as the cure for the wall corner bug.

---

## 8. Ray targeting and surface classification

### 8.1 Targeting pipeline

The build pointer uses a VR ray or desktop camera/mouse ray and selects a relevant collision target:

- Terrain / ground.
- An approved authored `BuildSurface` (later security structures may provide dedicated repair anchors).
- A registered **placed build piece**.
- Special connector/repair anchor, when the piece category requires it.

The preview object must **not raycast against itself**, or the candidate will chase its own geometry and oscillate. Ignore colliders from the preview rig or place it on a layer excluded from targeting; use a coherent policy.

### 8.2 Build layers

Earlier names appearing in the conversations:

- `Ground`
- `BuildSurface`
- `PlacedBuild`
- `PlaceBuild` (a conflicting name in older patches)

**Known trap:** `PlaceBuild` versus `PlacedBuild` is not cosmetic. Unity layer names and `LayerMask` selectors must match exactly. A candidate can snap to terrain but fail to stack if the raycast never hits the placed layer. Some earlier implementations recursively assigned layers to all instantiated child objects; retaining that behavior requires checking collision, target, and visual children separately.

### 8.3 Face from hit normal

A later solver classified the dominant absolute component of the collision normal:

- X-dominant: `+X` / `−X` side face.
- Y-dominant: `+Y` top / `−Y` underside.
- Z-dominant: `+Z` / `−Z` side face.

For simple axis-aligned 90° geometry, a dominant-axis normal is an effective initial rule. Sloped authored connectors should have their own specific attachment constraints rather than assuming an arbitrary sloped normal is a conventional box face.

### 8.4 Vertical intent from side-face aiming

On an existing wall side face, aim position indicates **lower, middle, or upper** attachment intent. A normalized vertical hit fraction can be computed along the target logical bounds:

```text
t = (hitPoint.y - targetBounds.min.y) / targetBounds.size.y

Bottom: t < 0.33
Middle: 0.33 <= t < 0.66
Top:    t >= 0.66
```

Values **0.33 and 0.66** were documented in the historical solver. They are logical bands, not permission to attach a wall at an arbitrary Y coordinate. They influence which **valid aligned vertical candidate** is selected. On a direct top-face hit, the solver should normally set the candidate's bottom to the target top.

### 8.5 Ground targeting special case

A regression caused valid initial ground placements to show red/invalid. The discussed correction included recognizing upward-facing ground/foundation targets as suitable support and avoiding tests that incorrectly rejected a prefab simply because its absolute pivot did not sit on an arbitrary world lattice origin. Terrain can be uneven; the selected ground-policy (flat anchored build regions vs following terrain) must be explicit rather than inferred from a general wall-solver rule.

---

## 9. Complete face, edge, corner, and tangent solver

### 9.1 The stages, in order

**Settled pipeline from the alignment note:**

1. **GetDominantAxis** — classify the struck face.
2. **GetVerticalIntent** — on side hits, inspect top/middle/bottom band.
3. **SolveTouchAxis** — solve exact min/max adjacency using the **rotated candidate bounds**.
4. **SolveVerticalAxis** — align the appropriate vertical band or contact elevation.
5. **SolveTangentAxis** — choose nearest structurally meaningful min/center/max alignment or connector.
6. **QuantizeToBuildUnit** — reconcile the candidate to the 0.5 m lattice **without losing face contact**.
7. **Validate / Preview / Confirm** — separate from the geometric solver itself.

The authoring note gave this order as an antidote to an overly patched `CalculatePlacementPosition()`.

### 9.2 Touching-axis math

Use rotated candidate bounds as if the candidate's **root position were zero** (call these `candidateAtOrigin`). To contact the positive-X face of an existing target:

```csharp
worldX = targetBounds.max.x - candidateAtOrigin.min.x;
```

For the negative-X face:

```csharp
worldX = targetBounds.min.x - candidateAtOrigin.max.x;
```

Analogous equations apply for Z:

```text
Hit +Z: candidate.min.z = target.max.z
Hit −Z: candidate.max.z = target.min.z
```

And Y:

```text
Hit top (+Y): candidate.min.y = target.max.y
Hit bottom (−Y): candidate.max.y = target.min.y   // only if underside builds are allowed
```

**Touching faces must meet, not overlap and not leave a seam.** A placement may still be rejected for unsupported/invalid type, but it must not be geometrically wrong.

### 9.3 Tangent-axis candidate generation

When touching on X, the tangent horizontal dimension is normally Z; when touching on Z, tangent is X. With an X-contact and Z-tangent, the alignment note supplied the following strategy:

```csharp
float rawZ = hit.point.z - candidateBoundsAtOrigin.center.z;

float[] candidateZs = new float[]
{
    targetBounds.min.z    - candidateBoundsAtOrigin.min.z,
    targetBounds.center.z - candidateBoundsAtOrigin.center.z,
    targetBounds.max.z    - candidateBoundsAtOrigin.max.z
};

// Reconcile valid candidate planes with the build lattice,
// then choose the candidate nearest the player's apparent intent.
```

These three candidates mean:

- **Min-to-min:** line up the near/low-end boundaries.
- **Center-to-center:** line up centers.
- **Max-to-max:** line up the far/high-end boundaries.

This is not a general “snap to whatever point the laser hits.” It prefers structural modular alignment so placing a small piece on the side of a long wall does not gradually drift.

### 9.4 Missing corner-intent refinement

**Remaining issue in March 27 screenshots:** A candidate could align by its center even when the ray pointed at a target's near/far edge. The proposed refinement introduced a `TangentIntent` classification (`Min`, `Center`, `Max`) from the struck face's local tangent coordinate, followed by a weighted scoring rule:

```text
Score(candidate) = distanceFromUserIntent
                 + penaltyIfCandidateAlignmentDisagreesWithMinCenterMaxIntent
                 + penaltyForInvalidOrNonModularResult
```

This is a **conceptual description** of the proposal, not a recovered method signature or exact constants. The purpose is to make **corner-oriented input prefer edge-to-edge candidates** rather than center-aligned positions that produce inset corners.

The user diagram titled `align.png` explicitly rejects a misaligned overlap/gap and marks an automatically flush edge alignment as correct.

### 9.5 Range and magnetism

An historical setup used `tangentAutoAlignMaxDistance = 0.76` m; an earlier iteration used around `0.51` m. The threshold is a **snap acceptance/magnetism distance**, not the core grid unit. It determines whether a structurally aligned candidate is close enough to the player's rough aim to be selected instead of another placement interpretation. This is still a tuning decision.

### 9.6 The two kinds of snapping

**Contact snap:**

```text
candidate.min = target.max
candidate.max = target.min
```

**Lattice snap:**

```text
Final logical face/edge/anchor planes fall on the shared 0.5 m modular system.
```

**Both are required.** Pure center-grid snapping can leave gaps; pure bounds freeform contact can yield a chain of numerically touching pieces that fail to line up on a consistent modular grid later.

### 9.7 Do not over-quantize all three axes

The alignment note explicitly states that quantization is **controlled**, not blind on every axis:

- **Contact axis:** preserve the chosen face relationship and reconcile only if the contact planes can stay equal.
- **Tangent axis:** choose and normalize nearest structural min/center/max/connector plane.
- **Vertical axis:** respect face/top/bottom/middle intent and normalize to modular levels where applicable.

The exact world anchor and allowed off-grid origin behavior need to be defined in the new `GridSpace` design before implementation.

### 9.8 Placement diagram references

See the included source figures:

- ![Bounds edge/corner alignment expectations](assets/align.png) — explicit reject/accept examples for edge alignment.
- ![Surface alignments at different height bands](assets/surface.png) — desired side attachment near top, middle, and bottom, plus multi-piece continuation.
- ![Side adjacency with meeting corners](assets/snap.png) — side-face and horizontal end-to-end flush placement.
- ![Half-meter surface increments](assets/increment_surface_alignment.png) — 3D example of half-meter cells, where `(1.5, 5.5, 1)` corresponds to `(3,11,2)` units.

---

## 10. Vertical stacking and roof construction

### 10.1 Why vertical stacking was difficult

A wall on top of another wall requires three distinct conditions:

1. The ray must hit the actual built piece / compatible support surface.
2. The candidate **bottom face** must be positioned exactly at the target's **top face**, regardless of the visual pivot location.
3. The support checker must recognize contact with the item immediately below while the overlap checker must **not** treat simple touching as penetration.

Earlier failures were linked to missing placed-layer ray hits, snapping Y to the wrong plane, and invalidation from overlap/support logic.

### 10.2 Direct top-face placement

For a top hit, the baseline equation is:

```text
candidateWorldY = targetWorldBounds.max.y - candidateBoundsAtOrigin.min.y
```

This avoids a prior suggested error of taking `Floor(hit.point.y / gridSize) * gridSize` for Y and landing inside or below the support's top surface.

**[TESTED / REPORTED]:** Later discussions reported vertical wall stacking working in at least a test configuration. This should not be read as proof that the full roof support and corner cases were solved.

### 10.3 Wall side upper/middle/lower intent

A side hit can request a horizontal attachment at one of several vertical levels. The final result should still land on a supported modular Y placement: align bottoms, align centers, align tops, or contact a top plane as appropriate to the piece's category and support mode. A wall/roof combination can use different rules than a wall/wall combination.

### 10.4 Roof panels as multi-piece room coverings

The user explicitly wanted to place **multiple roof tiles/panels** to form the top of a room, not merely put a single roof prefab on ground or balance one panel on one wall.

Required cases:

- First roof panel on top of a wall or an authorized ledge.
- Roof panel aligned beside another roof panel to extend the covering.
- Interior roof panel that no longer directly rests on a wall but can be supported by approved neighboring roof structure if the design allows it.
- Rotated roof panel with valid top elevation and correct edges.
- Sloped roof visuals that may meet with special connectors; do not use their entire decorative mesh as occupied space.
- A panel should be invalid if it is genuinely floating or contacts only a disallowed point/corner.

### 10.5 Roof edge adjacency versus corner adjacency

A later **proposed roof-specific** check required:

- Neighbor top/bottom plane match within about **0.08 m**.
- Shared edge span of at least about **0.45 m** (`0.9 × 0.5 m`).
- Simple diagonal **point/corner contact does not count as supporting a roof**.

These were suggested to distinguish a real roof seam from a roof touching a single corner. The **0.45 m overlap value was a prototype heuristic**, not a universal rule for all roof lengths and connector types. With varied sizes, use a minimum meaningful edge span or authored connector requirement.

### 10.6 Why roof edge placement still shifted in 0.5 m jumps

One screenshot showed that a roof could be placed either slightly inside a wall or visibly away from it, with no allowed midpoint. The inference was not “make everything freeform”; it was that the candidate reference bounds/pivot or the placement quantization phase was wrong. The corrected face/contact calculation must use both source and **rotated destination bounds** before normalizing. Structural candidate enumeration should include supported roof edge alignments rather than only generic center-grid positions.

### 10.7 `SupportMode` and later policy

The provided `SupportMode` enum names are `Default`, `Roof`, `Wall`, `Floor`, and `FreePlace`. Historical fixes used **roof-specific** validation distinct from walls. The later v0.1/v0.2 blueprints proposed per-category anchor rules:

| Category | Intended support / attachment |
|---|---|
| Floor | Terrain or approved supporting surface. |
| Wall | Ground/floor, face-to-face wall continuation, and top stacking where permitted. |
| Roof | Suitable wall/top provider **or** a valid roof-to-roof edge/connector chain; support rules decide max span if any. |
| Fence | Ground and compatible fence connectors/side faces. |
| Door / doorway | Authored doorway opening or dedicated doorway piece. |
| Light / fixture | Ground, wall mount, or dedicated anchor, depending on definition. |
| Security brace/board | Authored legacy-building window/door/gate anchor and associated repair policy. |

The exact `Default` and `FreePlace` behavior must be verified or specified; the table is the intended grammar, not proven existing switch statements.

---

## 11. Rotation and occupied geometry

### 11.1 Rotation design

- For the initial modular kit, rotate about Y only.
- `BuildPieceData.rotationStep` was shown as `90f`; allowed quarter-turns `0`, `90`, `180`, `270`.
- `yRotationOnly = true` appeared in the data definition.
- Rotation changes candidate bounds and occupancy; always **re-solve placement** after rotating rather than spinning the preview mesh around a previously solved center.

### 11.2 Rotated bounds are indispensable

A 1 × 2 × 0.5 m wall has a different footprint when its yaw is 90°:

```text
Yaw 0°:  X 1.0m  ×  Z 0.5m
Yaw 90°: X 0.5m  ×  Z 1.0m
```

A collision, contact-offset, and structural-alignment check using the previous orientation will fail. The solver must convert the logical local footprint into the **world-oriented candidate bounds** for that yaw before computing contact and proposed occupied cells.

### 11.3 Occupancy representation

The later blueprints specify integer-cell claims from a rotated **logical footprint**, not renderer bounds. Examples:

- `GridSpace` converts world to grid indices relative to fixed origin.
- `BuildWorldRegistry` tracks occupied cells and placed-instance IDs.
- Preview computes claims but **does not reserve or claim cells**.
- Successful commit atomically claims cells and instantiates/registers the piece.
- Deletion or destruction releases the same exact claims.

Special shapes may need explicit cell masks or connector rules. A sloped roof mesh may hang over its logical cells and should not globally block other legitimate panels merely because of its decorative eave.

---

## 12. Placement validation, support, and collisions

### 12.1 Validation is separate from solving

The **solver** answers: “Where should the piece be if we honor the player's intent?” The **validator** answers: “Is that exact placement allowed?” A placement that is flush but unsupported should be **correctly positioned and invalid**, not silently moved to a distant arbitrary “safe” coordinate.

Suggested reasons from the blueprints:

```text
Blocked / Overlap
Unsupported / NoSupport
InvalidSurface / WrongSurface
InsufficientResources / NeedWood
OutOfBuildArea             (proposed extension)
OccupiedAnchor             (proposed for security fixtures)
LockedByObjective          (proposed for security fixtures)
```

The varying names reflect multiple design passes, not a single canonical enum recovered from code.

### 12.2 Overlap validation

Historical implementations used `Physics.OverlapBox` or related collider/bounds checking to reject intersecting placed pieces. The key fix was to avoid rejecting **shared contact faces** as if they were nonzero penetration.

Suggested historical values/approaches:

- Overlap test **skin/shrink** of about **0.02 m** on test dimensions in one proposal.
- Another draft shrank dimensions by a factor around **0.98**.
- Exclude the candidate's ghost colliders from the overlap test.
- Recognize the support provider and disregard contact-only intersections when evaluating a candidate on top of it; do **not** ignore actual interpenetration.
- Use logical occupancy or a narrow-phase penetration test for authoritative results, rather than letting a decorative collider alone dictate validity.

**Status:** These are tuning variants in the debugging history, **not** a verified final physics setup. Using both `0.02` and `0.98` simultaneously without understanding the geometry can hide real penetrations.

### 12.3 Support and collision must not be conflated

A piece can be in empty space and still be invalid due to missing support. Another piece can have valid support but overlap an occupied cell. Separate checks are mandatory.

Support may be provided by:

- Ground / terrain.
- Explicit authored build surface.
- A placed floor or wall top.
- A compatible wall/roof/fence side connector.
- A legacy structure anchor (window, gate, door opening).

**Not all neighbors are valid providers.** A floating roof should not gain support because its corner is near an unrelated decorative collider.

### 12.4 Support raycast values discussed

One March proposal for false-invalid corner/support cases used:

```text
Support inset:         ~0.03 m
Ray origin above base: ~0.06 m
Ray length:            ~0.14 m
Contact tolerance:     ~0.16 m
```

There was also a helper named `HasSupportBelow` / `HasSupport` in prior discussions. **These values were troubleshooting suggestions**. The later architecture favors logical support providers and explicit anchor constraints so the correctness of a roof chain is not dependent on a fragile short raycast alone.

### 12.5 Face and roof tolerances

Other documented tuning values:

| Parameter | Value seen | Use / caveat |
|---|---:|---|
| `faceAttachTolerance` | `0.08` m | Contact-plane epsilon in March solver drafts. |
| `lowerBandThreshold` | `0.33` | Normalized side-hit vertical intent. |
| `upperBandThreshold` | `0.66` | Normalized side-hit vertical intent. |
| `tangentAutoAlignMaxDistance` | `0.51` → `0.76` m | Separate historical tuning iterations; latest **proposed** setup often `0.76`. |
| Maximum pointer/placement distance | `8` m | Value used in a March proposed complete setup. |
| Roof matching elevation tolerance | `0.08` m | Proposed roof support compatibility. |
| Minimum shared roof edge | `0.45` m | Proposed for 0.5 m module, not a universal edge grammar. |
| Overlap skin | `0.02` m | Suggested to avoid contact-only false positives. |
| Overlap box scale | `0.98` | Alternate earlier heuristic. |

### 12.6 Structural integrity and dependent deletion

The later design documents proposed tracking which pieces support others, but **the final removal rule was not locked across versions**:

- v0.1 considered blocking deletion, marking dependents unsupported, or deterministic dependent removal; physics collapse was specifically not a first-pass requirement.
- v0.2 recommended **prevent deleting a support while dependent parts exist**, with “Remove attached pieces first.”
- v0.3 seeks **safe removal/refund and protection of critical repair objectives**; unrestricted demolition of historic camp buildings is excluded.

This is a meaningful version difference. Use the v0.2/v0.3 conservative blocking behavior as the **recommended initial implementation**, but label it a proposal until accepted. Never silently delete a support and leave stale registry claims or dangling dependency IDs.

---

## 13. Preview, palette, controls, and delete mode

### 13.1 Build mode states

A complete March `BuildManager` draft used:

```csharp
public enum BuildMode
{
    None,
    Place,
    Delete
}
```

This separates a nonbuilding mode from a chosen-placement state and a destructive/editing mode.

### 13.2 Preview/ghost behavior

While placing:

1. Select a `BuildPieceData` asset / palette item.
2. Find the target and solve candidate pose.
3. Draw the preview at that **exact pose**.
4. Validate placement and costs.
5. Show a green/valid or red/invalid ghost with optional reason text.
6. Rotate/re-target/recompute every time the intention changes.
7. On confirmation, run the same authoritative validation and commit once.

The ghost should use separate preview materials/renderer handling and ideally should not itself block physics or be targeted by the placing ray. A `LineRenderer` pointer and delete highlight were present in earlier scripts.

**Known bug class:** A red ghost does not necessarily mean overlap. Layer mismatch, ground misclassification, bad support, missing resource, or out-of-phase quantization can all present identically if the validator collapses results to a boolean. The newer blueprints explicitly want distinct messages.

### 13.3 Desktop test controls discussed

| Input | Historical intent |
|---|---|
| `1`, `2`, `3` | Select initial build-palette pieces / slots. |
| `R` | Rotate current candidate by 90°. |
| Left mouse button | Place / confirm in placement mode. |
| `X` | Delete / remove an aimed placed piece in early desktop setup. |
| Right mouse button | A debug cancel/mode action appeared in an early mapping; exact final behavior unverified. |
| `Esc` | Exit/cancel building state in some early mapping. |

There were multiple iterations. **Do not assume every mapping above survived in the exact latest script**; use them as the known regression/test baseline.

### 13.4 Quest controller mapping discussed

- **Right trigger:** place or delete, depending on active mode.
- **B / secondary button:** toggle build/delete mode.
- **A / primary button:** rotate.
- Some early adapters used Unity XR `triggerButton`, `secondaryButton`, `primaryButton` names.

The later v0.1 blueprint deliberately kept platform/vendor binding at the **input adapter** level; the exact final XR framework was not locked by the build blueprint. The wider app has separately used Meta interaction technology; this does not prove the build manager's input code currently uses one specific SDK.

### 13.5 Press-edge requirement

Use **pressed-this-frame** / edge-trigger semantics for place, rotate, mode toggle, and delete. Holding a trigger should not build three duplicate walls, subtract the cost repeatedly, or delete a row of adjacent objects. Continuous ray/preview updates are fine; **commits are discrete transactions**.

### 13.6 World-space feedback and VR comfort

The later plans recommend:

- Quick-access build palette with icon, name, cost and available materials.
- A visible invalid reason instead of only red color.
- Optional target face/anchor highlight.
- Deliberate rotate/cancel/delete controls.
- Light haptics and audio on valid placement; clear but nonpunitive invalid cue.
- Legible world-space labels, color-independent cues, and no reliance on the user keeping a menu open constantly.
- Desktop test input calling **the same placement API**, not a separate desktop implementation with different snap rules.

---

## 14. Inventory, building costs, health, repair, and removal

### 14.1 Existing inventory concepts

The historical system discussed `PlayerInventory`, `InventoryCost[]`, and `BuildPieceData.buildCosts`. Construction should check availability **before** displaying a final valid result, but inventory must be deducted **only** when a valid placement successfully commits.

Gathering/resource discussions included tree chopping with tool/collider impact, a minimum movement/velocity threshold, per-target hit cooldown, node depletion, and auto-crediting common wood. Later blueprints move the first security-camp resource loop to **authorized salvage** (boards from a maintenance shed, stock box, or lumber pile) rather than chopping standing forest trees to construct a cabin.

### 14.2 Resource semantics by game design version

| Version | Resource flow | Status |
|---|---|---|
| Original forest survival | Axe strikes trees, receive wood, pay for construction | Discussed/prototyped. |
| v0.1 reconstruction | Wood-only initial resource, `InventoryService` as authority; tree harvesting separate from construction | Later blueprint. |
| v0.2 ranger woodland | Tree yields wood; simple shelter recipe; repair after a scripted incident | Later blueprint. |
| v0.3 security guards | Inspect authorized stock → take boards → shared depot decreases → build/repair from **shared session stock** | **Latest gameplay proposal**, requires co-op authority. |

### 14.3 Costs and recipes

Visible data has `InventoryCost[] buildCosts`. The **only exact example economy numbers** in the later plans were explicitly illustrative:

```text
Tree yield: 10 wood
Floor:       2 wood
Wall:        3 wood
Roof:        4 wood
Fence:       2 wood
Repair:      roughly 1–2 wood per damaged piece
```

**These are NOT approved final costs.** Security-specific boards, scrap/fasteners, and optional batteries also have no fixed final recipes. The v0.3 first resource milestone proposes **one collectible resource (boards), one shared depot, and one practical repair recipe**.

### 14.4 Atomic place transaction

Authoritative flow recommended across the later blueprints:

```text
User confirms candidate
  → validate current surface / anchor / support / occupied cells
  → check cost and construction permissions
  → reserve or atomically spend required stock
  → register claim and instantiate persistent piece
  → emit BuildPlaced / InventoryChanged
  → notify objectives, UI, save and (later) network peers
```

If validation fails, **spend nothing**. If a placement fails after stock reservation, it must roll back or complete the transaction consistently; never leave resources spent without the corresponding structure.

### 14.5 Durability

The visible class included:

```csharp
[Min(1)] public int maxHealth = 100;
```

This is an initial **asset default**, not proof every object should have 100 HP. A placed piece needs its own mutable current health. Later design suggests bands such as `Intact`, `Marked`, `Damaged`, and optionally `Destroyed`.

The preferred near-term night behavior in v0.2/v0.3 is **damaged but standing**, such as a scratched wall, dented fence, compromised gate, or disabled light, rather than immediately obliterating the player's work and triggering physics collapse.

### 14.6 Repair

Suggested authoritative repair lifecycle:

```text
Interact with a registered damaged build or legacy fixture
  → calculate missing condition and recipe
  → check access, target validity and available stock
  → spend resources exactly once
  → restore condition / visual band
  → emit BuildRepaired / FixtureRepaired
  → notify objective and persistent state
```

Night incidents request damage through a `DamageService`; scare scripts and UI code must not independently edit `currentHealth` or inventory.

### 14.7 Delete and refund

The visible `BuildPieceData` fragment included:

```csharp
public bool refundOnDelete = true;
```

Earlier drafts discussed returning recipe items when voluntarily deleting a placed piece. **The exact percentage/quantity returned was not reliably locked**; the `refundOnDelete` flag alone does not define whether it refunds 100%, a percentage, or a damaged-value amount. The v0.2 plan suggests a modest refund for voluntary removal, and v0.3 requires protecting mandatory security repairs.

Safe deletion should:

- Verify the target is a placed, deletable item rather than original camp infrastructure.
- Check support dependents and objective locks.
- Remove it from the registry and release occupied cells/anchor claims.
- Remove/update support references and health registry.
- Process refund only once according to explicit policy.
- Despawn/destroy visuals, notify UI/objectives/save, and replicate for co-op.

A destroyed part and a voluntary delete should use the same structural cleanup operation, **but may have different refund rules**.

---

## 15. World state, persistence, multiplayer, and story hooks

### 15.1 Persistent instance record

The October reconstruction proposed a stable data transfer object rather than relying on Unity object references or serializing `MonoBehaviour` instances.

Suggested fields:

```text
PlacedInstance
  instanceId
  pieceId
  gridAnchor / worldLogicalPose
  yawQuarterTurns
  currentHealth
  associatedAnchorId / supportProviderIds
  optional repair/owner/objective flags
```

The **exact schema and serializer are proposed**, not recovered. On load, reconstruct the occupancy registry and support relationships **before** enabling player placement.

### 15.2 Construction world registry

Proposed `BuildWorldRegistry` would own:

- Unique placed-instance IDs.
- Maps from `pieceId` to catalog definition.
- Cell occupancy and special connector/anchor claims.
- Support-provider and dependent relationships.
- Lookup by world location, target collider, or anchor.
- Lifecycle events: `BuildPlaced`, `BuildRemoved`, `BuildDamaged`, `BuildRepaired`.

This is essential for save/load, concurrent co-op building, and scripted incidents to find a relevant installed wall, boarded window, fence, or light.

### 15.3 Co-op security requirements (latest v0.3)

The latest design specifies **1–4 players total**, with shared authoritative world state. The host/server is expected to own:

- One placed-object registry and occupation/anchor table.
- One pool of shared repair/construction stock; carried tools may be personal.
- Atomic collection and build/repair transactions.
- One common shift clock and shared objective state.
- Damage and major incident results; persistence across rejoining.

Each player gets a **local preview ghost**. Two players aiming at the same empty spot can both *preview*, but **only one can commit** when the authoritative state resolves the collision. The other receives a fresh `PlaceRejected`/blocked result with no duplicate stock charge.

The exact network transport, authoritative host model, latency handling, and prediction rules were not chosen. Do not represent multiplayer transactions as already implemented.

### 15.4 Story-aware placement and dynamic incidents

Later narrative blueprints propose using built structures as authored event targets:

- Scratching/knocking on a wall the player actually placed.
- A rattle at a repaired perimeter fence.
- A repaired light flickering during night watch.
- A boarded window or braced door becoming the focal point of a disturbance.

An incident queries eligible registered pieces/anchors. If an expected piece doesn't exist, use an **authored fallback location** or select another incident. Story code should not assume every player built a cabin in the same spot or that every security team installed the same brace.

### 15.5 Keep game systems authoritative

The later documentation divides ownership:

| System | Owns |
|---|---|
| Input adapter | User intent and controller bindings. |
| Placement solver | Candidate pose and structural alignment. |
| Placement validator | Candidate eligibility, collision, support and cost checks. |
| Construction service / world registry | Permanent placed-world changes and removal. |
| Inventory / shared stock | Resource quantities, costs, reservations and spending. |
| Damage / repair | Current condition and repair result. |
| Save service | Durable data, migration and reconstruction. |
| Clock / objective / incident / radio | Request events and react to construction facts; do not bypass build authority. |

---

## 16. Observed defects and debugging history

| Symptom / date window | What was observed | Cause discussed | Fix or outcome status |
|---|---|---|---|
| March 23–24: roof always seeks ground | Roof top placement unavailable | Ground-only raycast / placement classification; no top-surface rules | **[PATCH PROPOSED]** `TopSurfaceOnly`, hit `PlacedBuild`, use top bounds. |
| March 24: stacking does not work | Walls/blocks cannot sit on existing pieces | Bad layer, collider hit, Y snap, overlap or support detection | **[TESTED / REPORTED]** stacking later worked in at least one revision. |
| March 24–25: rotated walls leave gaps | Adjacent rotated pieces fail to meet edges | Center snap, wrong rotated extents, large apparent increments | **[DECIDED DESIGN]** bounds face-solve rather than naïve pivot snapping. |
| March 25: valid corner shows invalid | Ghost red despite visually nonpenetrating adjacency | OverlapBox detects mere contact or bad support check | **[PATCH PROPOSED]** shrink/skin and separate support; exact final tuning unverified. |
| March 25: top roof Y works but horizontal overlap persists | Correct elevation, wrong X/Z | Failing to honor side normals and rotated bounds | **[PATCH PROPOSED]** dominant-axis face contact, then grid reconciliation. |
| March 25: prefab dimensions/pivots inconsistent | Placement offsets unexpected | Renderer vs footprint confusion, mesh transform/pivot | **[DECIDED DESIGN]** bottom-center root with dedicated logical bounds child. |
| March 26: X fixed then Z broken / vice versa | One-axis patch damages other axis | Axis-specific ad hoc conditionals | **[DECIDED DESIGN]** systematic solver stages. |
| March 26: new ground piece invalid | Can't place first foundation on ground | Ground-up orientation and absolute-world-grid tests too strict | **[PATCH PROPOSED]** allow valid upward support, validate footprint modularity not arbitrary pivot world-origin condition. |
| March 26: `GetGridSize` has no references | Legacy code apparently unused | Migrated from old `gridSize` logic to `buildUnit` solver | First cleanup pass reported; exact removed code unknown. |
| March 26: `GetSupportYFromHit`, `SnapValue`, `topFaceDotThreshold` | Likely dead helpers/field after rewrite | Old top-face/grid resolution still present | Audit/removal proposed; no source diff available. |
| March 26–27: 0.5 snap destroys face flushness | Piece can be inset or leave seam | Rounding solved transform without preserving contact | **[PATCH PROPOSED]** contact-preserving quantization per axis. |
| March 27: roof tile cannot sit exactly against wall corner | Nearest allowed position too deep or too far | Rotated footprint or incorrect lattice phase/contact | **[OPEN / VERIFY]** corner-aware roof candidates/connector matching proposed. |
| March 27: wall corners still inset | Center-aligned candidate selected near intended end cap | No tangent min/center/max intent preference | **[PATCH PROPOSED]** tangent-intent scoring, not confirmed merged. |
| March 26: `PlacedBuildPiece.Data` compile error | Class has no `Data` property | Calling code and component API diverged | **[OPEN / VERIFY]** reconcile `Data` versus `PieceData`/`Initialize`; don't assume resolved. |
| Various: deleted area stays invalid | Occupancy/collider ghost persists | Registry or preview not cleared, layer/collider stale | **[LATER BLUEPRINT]** atomic `RemovePiece` and occupancy cleanup, explicit tests. |
| Various: wrong placement layer name | Stacking/surface targeting fails silently | `PlaceBuild` vs `PlacedBuild` naming conflict | **[OPEN / VERIFY]** align Unity inspector and code constants. |

### 16.1 Lessons from debugging

1. Check **logical geometry** before changing grid size.
2. A candidate that visually touches but is invalid needs **reason-level diagnostics**.
3. Ground, side face, top face, roof edge, and wall opening are **different attachment grammars**.
4. Rotating is not a visual-only change; it changes contacts, support, and occupation.
5. Never trust a patch until it passes **X and Z**, **all four quarter-turns**, and **corner and upper/lower face** cases.
6. An `OverlapBox` heuristic is not a complete support or occupancy system.
7. Input and preview are not allowed to mutate the authoritative world until commit.
8. Keep a tested reference scene with simple box prefabs while building art and special shapes separately.

---

## 17. Proposed maintainable architecture

### 17.1 Migration from monolithic `BuildManager`

This is the **October v0.1–v0.3 recommended architecture**, not necessarily an implemented refactor:

```text
Building/
  Data/
    BuildPieceData (or BuildPieceDefinition)
    BuildRules
    AllowedAnchors / ConnectorDefinitions
  Grid/
    GridSpace
    BuildWorldRegistry / OccupancyRegistry
  Geometry/
    BuildFootprint
    RotatedLogicalBounds
    FaceAndConnectorQueries
  Placement/
    BuildTargeter
    PlacementSolver
    PlacementValidator
    PlacementProposal
    BuildPreview
  World/
    ConstructionService / BuildService
    PlacedBuildPiece
    SupportGraph
    BuildRemovalService
  Integration/
    BuildInputAdapter
    BuildPalette
    BuildEvents

Resources/
  ResourceDefinitions
  InventoryService / SharedStockService
  HarvestableNode / SupplyNode

Damage/
  DamageService
  RepairService

Persistence/
  SaveService
  PlacedPieceDTO
  LoadRebuilder

Tests/
  EditMode: geometry, rotation, occupancy, support
  PlayMode: preview, input, costs, deletion, save/rejoin
```

Names differ among the blueprints (`B01/B02`, `S03/S04/S05`, etc.); **do not simultaneously create duplicate overlapping systems under all names**. Choose one concrete naming convention after auditing the surviving code.

### 17.2 Candidate proposal contract

Suggested immutable/read-only structure:

```text
PlacementProposal
  pieceId
  desiredRotationQuarterTurns
  finalWorldPose / gridPose
  hitTargetKind / hitFace / anchorId
  occupiedCells
  chosenSupportProviderIds
  attachmentCandidateScore (optional)
  allowed / validationReasons
```

The proposal should be recalculated when the ray moves, piece changes, rotation changes, inventory changes, target support changes, or a competing player commits nearby.

### 17.3 Deterministic conceptual pipeline

```text
InputAdapter points and selects piece
  ↓
BuildTargeter raycasts terrain, placed pieces, authored anchors
  ↓
PlacementSolver
    A. classify target and dominant face
    B. compute rotated footprint
    C. infer vertical / tangent intent
    D. enumerate compatible face/connector candidates
    E. align exact contact
    F. reconcile resolved bounds/planes to global 0.5 m lattice
    G. score candidates and choose nearest valid-intent one
  ↓
PlacementValidator
    surface/anchor category
    occupancy / intersection
    structural support / roof edge
    build-region permissions
    resource affordability
  ↓
BuildPreview (local ghost and message)
  ↓ press edge
ConstructionService revalidates against current authoritative world
  ↓
InventoryService atomic spend (or shared stock reservation)
  ↓
ConstructionService places/registers persistent instance
  ↓
UI / story / save / replication listeners
```

### 17.4 Single-source-of-truth rule

The validator should use the **same data/geometry** as the solver and construction service. Neither UI nor a late collision callback should independently move a piece away from the previewed pose. Otherwise, a green preview may lead to a differently positioned real wall.

---

## 18. Build-system use across the three gameplay directions

### 18.1 Original family-friendly campground / survival builder

Players construct walls, floors, roofs, fences and useful campsite objects; collect wood via chopping; improve protection at night; and repair after encounters. The earliest reconstruction included nighttime creature attacks and possible defensive weapons, but enemy AI and exact combat were **not fully locked or shown implemented**. The build system must support broad open assembly and damageable structures.

### 18.2 v0.2 ranger-driven woodland mystery

The player builds/repairs a small shelter during the day, guided by a remote ranger by radio. At night, an authored incident can target a registered wall and leave damage. This direction suggested a single-player first playable, **“One Cabin, One Night”**: arrival; chop trees; build floor/two walls/roof/fence; dusk; a knock on the new wall; morning repair. Enemy waves and weapons were **deferred**, not required by the building code.

### 18.3 v0.3 abandoned-camp security shift — latest premise

**Current story/design handoff:** A wealthy buyer acquired an infamous abandoned summer camp and intends to demolish it. People are trespassing, taking souvenirs, damaging fences, and exploring sites linked to killings some years earlier. The player team (1–4 total) is hired to **secure and watch the existing property**, not create a wilderness cabin from scratch.

The system should therefore support **site-specific repairs and fortifications**:

```text
Radio/shift assignment
  → inspect damaged fence, gate or building
  → recover authorized boards from supply site
  → target authored repair anchor or damaged existing piece
  → preview modular patch / brace / board / light
  → validate anchor, occupied space, support and shared cost
  → commit/record repair once
  → secure the access point / objective
  → night incident reacts to that location
  → report damage and repair next shift
```

**Proposed first playable: “The First Watch.”** Contract briefing; walkthrough to breached fence and supply box; retrieve boards and place patch/brace; nightfall and radio check; gate rattle/distant flashlight; unsettling evidence at an off-limits building with controlled scare; morning damage report and repair. The building portion must work **solo and in four-player shared sessions** without duplicate pickup or placement.

The proposed first content is a bounded camp section (entrance, maintenance shed, cabin or infirmary facade), one breached fence, one supply source, two repairable piece types, and one or two validated fixture anchors. This is a more constrained **catalog/anchor policy**, not a different lattice/face solver.

### 18.4 Future modes and old promises

The October security premise does **not** retroactively erase the March free-building prototype. The best architectural interpretation is a reusable construction foundation with selectable rulesets:

- **Sandbox/Adventure catalog:** floors, walls, roofs, fences, cabins and optional free building where approved.
- **Security mission catalog:** fence patch, gate repair, board, brace, light and later platform, with limited anchors, permissions and costs.

This is a **recommendation**, not a confirmed feature toggle currently present in Unity.

---

## 19. Configuration and acceptance test matrix

### 19.1 Minimum scene/configuration checklist

**Inspector / project:**

- `Ground`, `BuildSurface`, and a **single consistently named placed-build layer** configured.
- Raycast masks include terrain, valid existing builds, and authored repair anchors.
- Preview visuals/ghost colliders are excluded from targeting and authoritative collision claims.
- A BuildManager/input coordinator references camera/XR pointer, catalog, inventory/shared stock service, palette, and materials.
- Test definitions have nonempty `pieceId`, prefab, expected `BuildFootprint`, rotation, costs and explicit support settings.
- Prefab bounds child is assigned and offset correctly relative to bottom-center root.
- Actual placed prefabs register correctly and expose targetable colliders.
- Fixed 0.5 m build-unit reference and fixed scene origin/anchor policy.
- Distinct visual feedback for invalid surface/support/overlap/cost; pointer draw distance configured.
- A clear test resource balance plus optional no-cost sandbox configuration.

**Before co-op testing:**

- A shared authoritative placed-world registry and stock service.
- Unique IDs for placed pieces and fixture anchors.
- Duplicate request protection and transaction IDs.
- Load/restore reconstructs occupation and valid repair state before accepting new requests.

### 19.2 Geometric tests

| Test ID | Scenario | Expected behavior |
|---|---|---|
| GEO-01 | First wall/floor placed on level terrain | Green preview, no false missing support, correct modular pose. |
| GEO-02 | Place a wall beside another at **+X**, yaw 0° | Candidate min X equals source max X; tangent edges line up without a gap. |
| GEO-03 | Same at **−X** | Candidate max X equals source min X. |
| GEO-04 | Repeat on **+Z**, **−Z** | No axis-asymmetry or residual Z gap. |
| GEO-05 | Rotate wall 90°, repeat four faces | Correct rotated width/depth and face contact. |
| GEO-06 | Four yaw options at corner | No inset corner, no false collision; user intent selects proper edge alignment. |
| GEO-07 | Side hit at bottom 10% | Bottom intent chooses permitted modular vertical candidate. |
| GEO-08 | Side hit at middle 50% | Middle intent chooses permitted structural alignment. |
| GEO-09 | Side hit at top 90% | Upper intent chooses permitted structural alignment. |
| GEO-10 | Place on wall top face | Candidate bottom Y equals wall top Y exactly. |
| GEO-11 | Double-stack two 2 m walls | Upper wall starts at lower wall's top; no floating seam or penetration. |
| GEO-12 | Roof on supported wall | Valid with correct height, no false penetration. |
| GEO-13 | Roof panel next to roof panel | Shared seam and valid support; no unintended half-step. |
| GEO-14 | Roof touches neighbor at **corner point only** | Unsupported when roof rule requires real edge span. |
| GEO-15 | Unequal modular dimensions `(1.5,5.5,1)` vs shorter piece | Contact/structural alignment respects `(3,11,2)` lattice cells. |
| GEO-16 | Decorative roof eave exceeds logical bounds | Legal occupancy remains based on footprint; graphic overhang does not block all neighbors. |
| GEO-17 | `BuildBounds` center slightly incorrect | Diagnostic warns/locates source of drift rather than changing grid globally. |
| GEO-18 | Rotating preview while aimed at edge | New valid candidate recomputed; geometry does not jump unpredictably. |
| GEO-19 | Aim slightly away from corner (within magnet range) | Correct edge/corner candidate selected if one is eligible. |
| GEO-20 | Aim beyond allowed snap range | No unexpected long-range teleport to an unrelated structural edge. |

### 19.3 Validation and lifecycle tests

| Test ID | Scenario | Expected behavior |
|---|---|---|
| VAL-01 | Candidate genuinely intersects occupied cells | Invalid `Blocked`; no cost. |
| VAL-02 | Candidate only shares an intended face | Not rejected as overlap. |
| VAL-03 | Candidate in empty air with no approved anchor | Invalid `Unsupported`; pose remains stable. |
| VAL-04 | Roof placed on disallowed wall/ground category | Invalid `WrongSurface` / `Unsupported`. |
| VAL-05 | Missing wood/boards | Preview explains insufficient stock, commit deducts zero. |
| VAL-06 | Held placement input | Exactly one structure per press edge. |
| VAL-07 | Delete one free-standing part | Cell claims and collision target removed; optional refund follows policy once. |
| VAL-08 | Delete a supporting wall with attached roof | Conservative first policy blocks removal and explains dependent piece. |
| VAL-09 | Rotate/replace a piece | Old occupancy claims do not leak; new claims are rechecked. |
| VAL-10 | Save/load the structure | Exact grid pose, yaw, health and occupancy restored. |
| VAL-11 | Unrecognized piece ID during load | Safe logged failure or explicit migration; no silently broken registry. |
| VAL-12 | Two players claim same slot at once | Exactly one authoritative commit, loser charged zero. |
| VAL-13 | Two players collect same salvage node | Supplies awarded once; shared node quantity never negative. |
| VAL-14 | Two players repair same fixture | No duplicate repair cost and no state divergence. |
| VAL-15 | Delete/repair a mission-critical fence | Objective lock/repair rule enforced, synchronized to all. |
| VAL-16 | Incident targets a nonexistent player wall | Uses compatible authored fallback; no null reference or stuck story. |

### 19.4 Diagnostics worth adding

**Recommended (not documented as already implemented):** a debug placement overlay printing `pieceId`, rotated logical bounds, source/candidate face planes, raw hit, selected vertical/tangent intent, chosen candidate score, pre-/post-lattice pose, occupied cells, support IDs, invalid reason flags, and current stock. Draw gizmos for `BuildBounds`, contact faces, support rays, and occupancy cell coordinates. This would directly expose the March problems that were otherwise visible only as red ghosts and tiny seams.

---

## 20. What is confirmed, proposed, or unresolved

### 20.1 Firmest recovered facts

- Unity VR modular construction was actively being developed/tested in March 2026.
- The key historical code names included `BuildManager`, `BuildPieceData`, `BuildFootprint`, `PlacedBuildPiece`, `PlayerInventory`, and `InventoryCost`.
- The working modular unit was **0.5 m** and the intended rotation was **Y only, 90° steps**.
- Preview, deletion, ray targeting, surface attachment, stacking, and roofs were all worked on.
- Dedicated footprint/bounds data was introduced to solve prefab geometry problems.
- Placement suffered X/Z/rotated wall corner and roof seam errors; some revisions fixed specific cases without resolving all cases.
- The intended final geometry model is **face adjacency → bounds structural alignment → contact-preserving half-meter lattice quantization**.
- The visible `BuildPieceData` fragment contains the exact enums and fields transcribed in Section 5.
- A `PlacedBuildPiece.Data` mismatch was reported.
- The latest game-story blueprint positions the mechanic as **repair/fortification of an abandoned camp in 1–4 player VR**, with shared materials and authoritative placement still to be implemented.

### 20.2 Not demonstrated as working in the available material

- A fully compiled **latest** `BuildManager.cs` that includes all corner fixes.
- Exact final `BuildFootprint.cs`, `PlacedBuildPiece.cs`, `PlayerInventory.cs` and `InventoryCost` field layouts.
- Successful final tangent-intent corner patch in both horizontal axes.
- Complete all-roof support adjacency and multi-panel ceiling acceptance testing.
- Atomic networked occupancy/stock transactions in a real 4-player Unity session.
- Persisted placed-world save/load and rejoin lifecycle in the actual Unity project.
- A finished refund policy; structural collapse policy; repair economy; final game catalog.
- Network stack, authority/transport details, or a shipped combat/damage/night system.

### 20.3 Important design choices to settle in code

1. Whether the **global 0.5 m lattice** is the only authoritative snap interval and how old per-piece `gridSize=1f` is handled.
2. Exact meaning of `BuildSurfaceType.GridOnly`, `SupportMode.Default`, `SupportMode.FreePlace`, and which legacy `BuildPlacementMode` rules survive.
3. Bottom-center pivot convention for *every* buildable piece versus special authored anchors for fixtures.
4. Whether footprint logical volume is a single box, multi-cell mask, or custom connector geometry for slope/door/roof.
5. Exact tolerance and scoring parameters after all-axis tests (`0.51` vs `0.76`, overlap skin, roof shared-edge minimum).
6. Support dependency response on voluntary deletion and scripted destruction.
7. Refund value/percentage and whether damaged pieces refund less or nothing.
8. Which pieces are freeform sandbox placements versus restricted security repair anchors.
9. How terrain slope/elevation and existing legacy-building fixtures reconcile with the 0.5 m lattice origin.
10. How authority, client prediction, replication and save/rejoin are implemented for 1–4-player sessions.

---

## 21. Suggested rebuild and verification sequence

**This is a reconstruction path, not a statement that these tasks have already been completed.**

### Stage A — Preserve what remains

1. Back up the surviving Unity project and collect the real four core `.cs` files, related inventory classes, scene and prefabs.
2. Search all `BuildManager` calls, especially `Data`, `PieceData`, `GetGridSize`, `CalculatePlacementPosition`, and `SolvePlacement`.
3. Record current build-layer names and masks before renaming anything.
4. Make a minimal test scene with simple wall, floor, and roof logical bounds and visible gizmos.
5. Run baseline compilation; repair interface mismatches **before** changing solver behavior.

### Stage B — Lock one source of geometric truth

1. Define the 0.5 m coordinate frame/global origin and bounds/pivot convention.
2. Audit `BuildFootprint`; validate box dimensions and center in every prefab.
3. Make rotated bounds calculations deterministic at all four yaw angles.
4. Implement/verify distinct face-contact, tangent-alignment, vertical-intent, and lattice reconciliation stages.
5. Add intent-aware corner candidate scoring and contact-preserving quantization.
6. Exhaustively test X/Z and all four yaw orientations before adding roof exceptions.

### Stage C — Support, collision, and roof panels

1. Establish logical occupied cells or equivalent precise collision model.
2. Separate overlap from support checks; reject real penetration, accept merely flush contact.
3. Validate wall stacking and first roof-on-wall panel.
4. Add roof-to-roof edge rules, negative corner-point-only tests, and multi-panel room coverage.
5. Assign clear invalid reason codes.

### Stage D — Lifecycle and economy

1. Restore consistent `PlacedBuildPiece` definition linkage and initialization.
2. Move placement cost deduction to a single successful commit path.
3. Implement/verify delete, refund and occupancy release once.
4. Introduce durable instance IDs and exact save/load reconstruction.
5. Add damage, repair and support-dependency lifecycle.

### Stage E — Security-camp integration

1. Implement a small legacy-building anchor catalog (fence gap, gate, doorway/window, work light).
2. Use a shared supplies depot with one boards recipe.
3. Hook the construction service to objective flags and radio reactions.
4. Add one authored night incident targeting a built or repaired anchor with fallback.
5. Extend to 1–4 players with authoritative cell/anchor and stock transactions.
6. Test both ordinary and deliberately awkward placements across solo/rejoin/4-player sessions.

---

## 22. Technical appendices

### Appendix A — Snapshot of explicit historical settings and terms

```ini
; Not a copy-paste Unity config. Values are historical or proposed as labeled.
GlobalBuildUnit=0.5       ; decided
YawRotationStep=90        ; decided
WallBoundsSize=1,2,0.5    ; historical test asset
WallBoundsCenter=0,1,0   ; historical test asset
RoofBoundsSize=1,0.5,1   ; historical test asset
RoofBoundsCenter=0,0.25,0; historical test asset
FaceAttachTolerance=0.08 ; solver draft
VerticalBottomBand=0.33  ; solver draft
VerticalTopBand=0.66     ; solver draft
TangentAutoAlignMax=0.76 ; later proposed tuning (previously ~0.51)
PlacementMaxDistance=8   ; proposed setup
OverlapBoxSkin=0.02      ; proposed alternative
OverlapBoxScale=0.98     ; separate proposed alternative
SupportInset=0.03        ; proposed support ray setting
SupportRayOffset=0.06    ; proposed support ray setting
SupportRayLength=0.14    ; proposed support ray setting
SupportTolerance=0.16    ; proposed support check
RoofElevationTolerance=0.08 ; proposed roof check
RoofMinSharedEdge=0.45  ; proposed 0.5-module roof check
```

### Appendix B — Equations and invariant checks

For target bounds `T`, rotated candidate bounds at root `(0,0,0)` `C₀`, and candidate root translation `P`:

```text
Contact +X : P.x = T.max.x - C₀.min.x
Contact -X : P.x = T.min.x - C₀.max.x
Contact +Y : P.y = T.max.y - C₀.min.y
Contact -Y : P.y = T.min.y - C₀.max.y
Contact +Z : P.z = T.max.z - C₀.min.z
Contact -Z : P.z = T.min.z - C₀.max.z

Tangent min    : P.t = T.min.t    - C₀.min.t
Tangent center : P.t = T.center.t - C₀.center.t
Tangent max    : P.t = T.max.t    - C₀.max.t

Cell length = 0.5m
Cell count = logical dimension / 0.5m
```

After solving, check the intended equality again, e.g. `abs(candidateWorld.min.x - T.max.x) <= faceAttachTolerance`, and ensure allowed occupied cells and support. **Do not use these equations alone as full production collision or roof connector code.**

### Appendix C — Example input/action interface (proposal)

```text
SelectPiece(pieceId)
EnterBuildMode(mode)
SetRotation(quarterTurns)
UpdateAim(ray)
GetPlacementProposal(pieceId, quarterTurns, ray, context)
ConfirmPlacement(proposalId or validation token)
TryRemove(targetInstanceId)
TryRepair(targetInstanceId)
```

These APIs are a recommended separation between input and construction; not recovered function signatures.

### Appendix D — Suggested events (proposed)

```text
PlacementPreviewUpdated
BuildPlaced / PiecePlaced
PlaceRejected
BuildRemoved / PieceRemoved
BuildDamaged / PieceDamaged
BuildRepaired
BuildDestroyed / PieceDestroyed
InventoryChanged / StockChanged
SupplyCollected / ResourceGained
SaveComplete / RestoreComplete
ObjectiveChanged / StoryFlagSet
```

Each event should report a **fact after an authoritative change**. Event listeners may trigger UI, radio, haptics, and logs, but should not secretly retry spending or move the piece.

### Appendix E — Design-gameplay tradeoffs retained from the October blueprints

- **Pokémon Pokopia:** low-friction collection, visible repair/rejuvenation, rewarding incremental improvements; not a requirement for a broad life-sim catalog.
- **Minecraft:** readable modular construction, meaningful resource costs, and night changing the perception of shelter; not unbounded voxel destruction or a procedural infinite world.
- **Firewatch:** human radio contact and gradual mystery; not copy of characters or a fully linear single-player framework.
- **Five Nights at Freddy's:** an assigned watch, limited information, credible site-security routines and dread; not a requirement for stationary camera-room gameplay.

These are **design inspirations**, not features implemented by the build system.

### Appendix F — What must be captured from the Unity project to upgrade this from a reconstruction to an audited specification

```text
Assets/**/BuildManager.cs
Assets/**/BuildPieceData.cs
Assets/**/BuildFootprint.cs
Assets/**/PlacedBuildPiece.cs
Assets/**/PlayerInventory.cs
Assets/**/InventoryCost*.cs
Any BuildMode / input adapter script
Any BuildPalette / preview component
Relevant scene .unity and prefab .prefab assets
BuildPieceData .asset definitions
Physics layers and collision matrix screenshots
Inspector settings for test wall / roof / floor prefabs
Current compile log and playmode video/test screenshots
```

Once available, each field and method could be marked **verified in source**, deprecated, or to-be-implemented. Without those files, exact call graphs, compile behavior, and the latest resolved/unresolved defects cannot be claimed.

---

## 23. Source inventory

| Reference | What it supports | Provenance / limitation |
|---|---|---|
| `Build Alignment Update.txt` | Final explicit formula and order: face contact, structural min/center/max alignment, 0.5 m lattice and helper names. | User-supplied design note, **not** complete compiled source. |
| `align.png` | Reject gap/inset, prefer structural bounds edge alignment. | Source image. |
| `surface.png` | Side attachment at top/middle/bottom and chained adjacent pieces. | Source image. |
| `snap.png` | Flush horizontal and face adjacency. | Source image. |
| `increment surface alignment.png` | 0.5 m increment, modular face planes, `(1.5,5.5,1)` example. | Source image. |
| March 23–28, 2026 construction discussions | Historical code names, field fragments, issue progression, control mapping, tentative constants, success reports and regressions. | Conversation history; **full latest code not supplied**. |
| `Campfire_Friends_VR_Systems_Blueprint_v1.md` (Oct 27, 2026) | Technical system boundaries, logical bounds separate from visuals, placement/validation/commit order, inventory, support, persistence, acceptance tests. | Explicit **reconstruction baseline**, not proof of implemented services. |
| `Campfire_Friends_VR_Build_Resource_Night_Systems_v02.md` (Oct 27, 2026) | Ranger-driven story integration, wood/recipes, night damage/repair, candidate/commit and support-removal policy. | **Working design proposal**; v0.2 single-player premise superseded by v0.3 for current gameplay. |
| `Campfire_Friends_VR_Security_Camp_Systems_v03.pdf` (Oct 27, 2026) | Latest 1–4 guard security premise, limited repair kit, authored anchors, shared stock and authoritative co-op transaction requirements. | **Working design proposal**; multiplayer implementation not recovered. |
| `camper.txt` | Related Camp Ashwood narrative/lore context, not technical construction rules. | Story document; no support for source-code claims. |

**Final engineering interpretation:** Keep **0.5 m face/edge-aware modular construction** as the stable core. Preserve the March hard-won lessons about rotated bounds, support, roofing, and contact-preserving alignment. Treat the October security game as a **new ruleset and mission wrapper** around this core—not as evidence the old code or difficult geometry bugs have disappeared.
