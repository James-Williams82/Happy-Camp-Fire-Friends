# CAMPFIRE FRIENDS VR
## Abandoned Camp Security — Core Systems & Narrative Design

**Version:** 0.3  |  **Date:** October 27, 2026  |  **Status:** Working concept and system definitions; decisions marked as proposals

**Scope:** Focus on **1 — Construction**, **2 — Resource Gathering / Inventory**, and **5 — Time / Night Incidents / Repair**. Radio narration and **1–4 player cooperative sessions** are required supporting systems. Combat, weapons, and advanced enemy AI are deliberately deferred.

> **Core proposition:** *You're paid to keep people out of a condemned summer camp. By the second night, you're not sure who's trying to get in — or who might already be there.*

## 1. The corrected game premise

An abandoned summer camp has been purchased by a wealthy buyer. The property is scheduled for demolition, but it has become an attraction for trespassers, souvenir hunters, and vandals. Several people were killed there a few years ago; the case has made the grounds infamous. People sneak in to steal fixtures and personal effects, photograph buildings, or claim a piece of the camp's history.

The buyer hires **one to four temporary security guards** to watch the property until the demolition crew arrives. The players are those guards. They're not campers rebuilding a retreat. They're employees trying to get through a short contract: protect remaining property, make necessary repairs, document disturbances, and report anything suspicious.

A remote **camp/base ranger or operations contact** speaks over the radio. Like the human connection in *Firewatch*, this voice grounds the player in an isolated place: helping with tasks, making jokes, recalling camp history, and occasionally going quiet at the wrong moment. That person has information the guards do not, but their knowledge and reliability should unfold rather than be announced.

The experience starts plausibly: people are trespassing at a notorious abandoned camp. Over successive shifts, events stop fitting the explanation. **Whether the true threat is human, supernatural, or deliberately ambiguous remains an open story decision.**

**Format:** First-person VR; **solo or 2–4 player co-op, four total maximum**; a bounded abandoned-camp map; successive day-to-night security shifts; authored suspense punctuated by selected jump scares.

**Working title caution:** “Campfire Friends VR” is retained as the project label. It now sounds considerably warmer than the actual premise. We can revisit the commercial title after tone is established.

## 2. Experience pillars and reference games

| Influence | What to extract | How it translates here |
|---|---|---|
| **Five Nights at Freddy's** | An ordinary security job with a fixed shift, procedures, limited information, escalating dread, and a reason to stay at a dangerous site. | Players have an assigned overnight watch, must protect specific locations, respond to disturbances, and endure the shift. We are **not** copying a stationary camera-room format. |
| **Firewatch** | A believable remote relationship, an isolated environment, and a mystery carried by what a radio partner says or avoids saying. | A ranger/operations voice provides guidance, humor, partial local history, and concern; conversation responds to discoveries. |
| **Minecraft** | Consistent grid-based placement and a satisfying relationship between usable resources and built objects. | Repair and fortification use a modular, legible kit. The existing camp is authored terrain/architecture, not a player-built voxel world. |
| **Pokémon Pokopia** | Low-friction collecting and visible improvement of a neglected place. | Tasks feel rewarding: restore lights, patch fences, organize a watch point, make a location feel temporarily secure. |

**Four design promises:** (1) The camp feels like a real place that existed before the player; (2) every repair has a reason in the security job; (3) fear comes from uncertainty rather than constant attack; (4) co-op changes how players *experience* incidents without making the story incoherent for a solo guard.

## 3. Core player loop and nightly structure

**Arrival / briefing → survey and collect supplies → repair and fortify assigned locations → take the night watch → investigate or respond to disturbances → report, assess damage, and prepare for the next shift.**

| Stage | Player activity | Systems exercised | Narrative function |
|---|---|---|---|
| **Briefing** | Hear job terms; collect radio, flashlight, and first assignment. | Radio, objective state, session spawn | Establish believable reasons to be on-site. |
| **Preparation** | Inspect gates, sheds, buildings; collect sanctioned supplies; fix a fence or secure an entry point. | Resource nodes, inventory, grid placement, repair | Turn the location into a place players understand. |
| **Dusk** | Check lights, choose patrol routes, receive first radio check-in. | Time state, equipment state, story trigger | Establish routine and the first feeling of vulnerability. |
| **Night watch** | Patrol, hear a disturbance, check access points, possibly split up, decide what to report. | Incident director, location triggers, radio, co-op | Suspense and occasional jump scares. |
| **Shift end** | Compare what was seen, document missing/damaged property, repair critical sections. | Damage, objectives, persistent world | Leave evidence and consequences, not a total reset. |

**Night progression proposal:** A short multi-night contract, perhaps **three to five shifts**, with different objectives and story beats. The number of shifts is a pacing decision, **not** a technical requirement for the prototype. One fully authored shift is the first target.

**Security-fantasy constraint:** A hired guard should not be harvesting a forest simply to make a cabin on someone else's condemned property. Construction exists to **board, reinforce, illuminate, fence, and control access**. Gathering means recovering permitted supplies, salvage, and usable repair materials. More expansive camp construction can remain a sandbox option later.

## 4. System 1 — Construction and site fortification

### Responsibility

Provide a modular placement system that allows guards to improve **specific security-relevant parts of the existing camp**. The authored camp buildings are permanent landmarks; player-authored pieces are temporary repairs or fortifications placed against valid surfaces or on the ground.

### First building kit

| Piece | Use | Where it can go |
|---|---|---|
| Fence panel | Close a gap in the perimeter | Ground or compatible fence connector |
| Gate / gate latch repair | Restore a controllable entry point | Approved gate anchor |
| Boarding plank | Cover a window or weak doorway | Approved wall/opening anchor |
| Door brace | Secure an existing door | Approved door anchor |
| Portable light / work light | Illuminate a route or watch position | Ground, wall mount, or approved connector |
| Small watch platform **(later)** | Player-designed lookout | Ground-supported modular assembly |

**Placement rules proposed for first prototype:** 0.5 m logical grid; 90° yaw turns; face/connector snap; explicit anchor types on legacy buildings; footprint separate from visual mesh; occupancy and support checks; valid placement preview; material cost validated again on commit; safe deletion/refund policy. These preserve what worked in the earlier grid-building prototype while fitting the new setting.

**Construction flow:** `target → classify terrain / camp fixture / existing piece → generate snap candidate → grid/anchor alignment → validate occupancy + surface + support + cost → preview → revalidate → spend materials once → create persistent instance`.

**Multiplayer requirement:** A single authoritative world state accepts a construction transaction. Two players cannot spend the same materials or claim the same cells at once. Both see the same preview result after placement; preview ghosts remain local to the player using the tool.

**Initial exclusions:** Full cabin-from-scratch requirements, arbitrary destruction of historic camp buildings, simulation-heavy physics collapse, unrestricted deletion of critical objective pieces.

**Acceptance checks:** Fence joints meet cleanly; a plank lines up with a boarded opening; blocked placement explains why; two users contesting one spot never duplicate a piece; placed pieces remain after saving/rejoining; story events can locate a gate, brace, or fence the group actually placed.

## 5. System 2 — Gathering, salvage, inventory, and repair supplies

### Responsibility

Put useful security materials into players' hands without breaking the fiction that they are *protecting*, not looting, the property. Supplies should be found where former camp staff left them: a maintenance shed, sealed supply crates, a lumber stack, fallen branches, and derelict work areas whose contents are authorized for temporary repairs.

### Proposed starting materials

| Resource | Sources | Consumed by |
|---|---|---|
| **Boards / wood** | Maintenance stock, fallen branches, spare fence planks | Fence repair, door boarding, temporary barriers |
| **Fasteners / scrap metal** | Tool shed, approved scrap bins | Gate hardware, braces, lights |
| **Batteries** *(optional in first slice)* | Supply cabinet, issued equipment | Portable lights, backup devices |

**Recommended first implementation:** One real collectible (boards), one shared depot, one practical repair recipe, and a clear indicator of remaining materials. Add scrap and batteries only once the loop works.

**Resource interaction:** Inspect a supply node → intentionally take authorized stock → node quantity decreases → inventory updates → recipe can consume it. The prior tree-chopping technology can still be reused for fallen timber or clearing paths but should not define the first job.

**Inventory model:** Inventory capacity may be per-player for carried tools; **construction stock is a shared session pool** in the initial co-op prototype. This avoids a player hoarding materials required to finish the night's tasks. If we later want individually carried repair kits, we can add them without changing authoritative spending.

**Anti-soft-lock rule:** The game must provide enough supplies to repair any required tutorial objective even if players misplace a permitted piece. Show costs, refunds, and repair availability clearly.

**Acceptance checks:** No duplicate pickup from the same node; two simultaneous withdrawal requests do not duplicate supplies; no negative stock; failed placement spends nothing; incident damage can be repaired after the shift.

## 6. System 5 — Time, security shifts, atmosphere, and scares

### Shift state machine

`BRIEFING / PREP → DUSK → NIGHT_WATCH → SHIFT_END / MORNING → PREP`.

Clock progression and **story progression are separate**. Time changes lighting and permitted activity; story state decides which incidents and conversations can play. First prototype can use a condensed shift for testing without determining final game length.

### Security incident director

A night incident is an authored event with prerequisites, a physical target or location, an escalation sequence, multiplayer visibility policy, player responses, and persistent aftermath. The director must support at least these incident classes:

- **Plausible trespass:** An open service gate, a distant flashlight, a missing toolbox, footsteps near the dining hall. Players initially interpret this as vandalism.
- **Unsettling evidence:** A sign moved between patrols; wet footprints in a locked building; a radio exchange that contradicts a location's history.
- **Controlled jump scare:** A sudden silhouette behind a window, a figure in the infirmary doorway, a startling sound *after* a fair warning cue. It should be brief and not every minute.

**Local versus shared events:** Major events affecting objectives, damage, or evidence are **shared and server-authoritative**. Some harmless atmospheric moments may target one player to create the believable co-op exchange, “Did you see that?” The system records who witnessed what; it must not contradict shared world facts. Solo mode receives the same essential clues.

**Dynamic anchors:** A knock can target an installed boarded window; a light flicker can target a repaired lamp; a perimeter rattle can target a repaired fence. If no suitable player-built piece exists, fall back to a stable authored camp location. Horror should react to player work without requiring every team to construct identical layouts.

### Structure damage and shift consequences

Repairable fixtures and player-authored pieces have durable IDs, condition, and repair costs. Incidents can change condition only through the authoritative damage service. For the prototype, use scuffs, damaged gates, and disabled lights rather than instant destruction of the team's work. End-of-shift reporting shows outstanding damage and missing property. **Whether failure ends a run, costs pay, or only changes later nights remains to be designed.**

### VR scare comfort

No forced head movement or artificial camera pulls. Provide a scare-intensity setting, subtitle support, audio dynamic range choices, and a straightforward pause/comfort option for solo play. In co-op, local pause should not freeze other players; safe-idle behavior needs definition. Suspense should primarily arise from presence, distance, darkness, silence, and unreliable reports.

## 7. The radio contact and the buried story

**Role:** An off-site ranger, old camp caretaker, or base operations dispatcher who knows the grounds. The final job title is open; the function is not. They are the most important recurring NPC voice, not a tutorial narrator.

**Voice:** Practical, lightly humorous, calming when appropriate, never narrating every action. Early calls focus on gates, lights, vandals, and inventory. As the nights advance, the contact recognizes details from the old killings, reframes something they previously said, or declines a question. An unreliable source is a *dramatic possibility*, not a canon twist yet.

**Co-op radio policy:** Shared calls for objectives and major plot; selective one-to-one calls for player-discovered details; queue/interrupt rules so dialogue does not drown out in-game teammate communication. Important clues are stored in a shared log or report board so no player loses the plot because they were fixing a distant fence.

**Narrative boundary:** The killings several years ago and the site's notoriety belong in the initial assignment. **The truth of what happened does not.** Start with the ordinary explanation that thieves and thrill-seekers are getting in. Each shift should make that explanation less satisfactory.

### Sample introductory security briefing (draft dialogue)

> “Alright, quick rundown. A private buyer picked up the old summer camp, and demolition is scheduled once the last inventory checks are done. Until then, the owner wants the property watched. We've had people cutting fences, getting into the cabins, walking off with anything they can carry.”
>
> 
> “Yeah, you've probably heard the stories. A few people were killed out here a few years back, and now everybody thinks they need a souvenir. We're not asking you to solve a cold case. Keep the gates closed, document any damage, and radio base if something looks wrong.”
>
> 
> “There are one to four of you on the shift. Stick to the marked areas, don't confront trespassers, and don't go into the old infirmary unless we tell you it's clear. Your contact on the radio will take it from here. Easy money.”

**Why this opening works:** The job explains why players are here; demolition creates a short deadline; vandals provide a rational cause for every early noise; the old killings are acknowledged without exposition; a single off-limits building plants a mystery. “Easy money” becomes less reassuring with each night.

## 8. Multiplayer is a foundation, not an endgame feature

**Support:** 1–4 total players in a private shared VR session. The host/server owns the placed-world registry, resource transactions, phase and objective state, damage and major incidents. Exact hosting/transport choice is **not yet selected**.

| Concern | First-pass rule |
|---|---|
| World building | Shared authoritative placed-object registry; preview local to builder |
| Materials | Shared stock for repairs; tool equipment is personal |
| Story | Global chapter/shift flags; local discovery tracked and copied to a shared log |
| Radio | Plot dialogue available to all; contextual lines can address a specific player |
| Time | One synchronized clock and shift end; no private fast-forward |
| Incidents | Shared facts and damage; optional local atmospheric events |
| Save | Persist the camp/session, not a separate incompatible world per guard |
| Group behavior | Encourage cooperation and separation through parallel duties; **never require** four players to make progress |

**Important risk:** Four people chatting can dissolve suspense. The answer should not be arbitrary forced separation or voice-chat sabotage. Use patrol routes, simultaneous objectives, distance, and brief asymmetric perceptions to create stories players tell each other. Design each objective to be achievable solo at a slower pace.

## 9. First playable vertical slice — “The First Watch”

| Beat | Player action | What it proves |
|---|---|---|
| **1. Contract** | Spawn at camp entrance; hear briefing and radio introduction. | Solo/co-op session, narrative objective, shared comms |
| **2. Walkthrough** | Locate a cut fence and a maintenance supply box. | Map affordances and security motivation |
| **3. Repair** | Retrieve boards; snap a temporary fence patch or brace; verify access point secured. | Systems 1 and 2, synchronized spending and placement |
| **4. Nightfall** | Get a radio check-in; assigned patrol begins. | Common clock, audio, lighting, objectives |
| **5. First disturbance** | Hear a gate rattle or glimpse a distant flashlight; decide to inspect and report. | Spatial event and radio choice |
| **6. The wrong detail** | A locked building shows evidence of entry; one controlled scare follows prior warning. | Horror escalation, solo/co-op incident handling |
| **7. Morning report** | See new damage/missing item; file report and repair. | Persistent consequence and reason for Night Two |

**Ship test:** The full loop works alone and with four players; two players cannot duplicate one pickup or placement; the objective remains completable even when players refuse to split up; the scare isn't broken by an unusual barricade placement; radio clues can be recovered if someone misses a line; save/rejoin preserves the site's changed state.

**First content budget:** One small camp section (entrance, shed, one cabin/infirmary facade); one breached fence; one material source; two repairable piece types; one central radio contact; a condensed day-night transition; one plausible incident, one unnerving clue, and one jump scare. No combat or roaming enemy AI needed.

## 10. Responsibility boundaries and technical contracts

| ID | System | Authoritative responsibilities | Emits / exposes |
|---|---|---|---|
| **S01** | Session/Network | Players, shared session ID, permissions, replication | PlayerJoined, PlayerLeft, authority context |
| **B01** | Build Grid / Catalog | Footprints, grid coordinates, anchors, valid piece definitions | PlacementCandidates, occupancy query |
| **B02** | Construction Service | Validate+commit+remove one world action | BuildPlaced, BuildRemoved, PlaceRejected |
| **R01** | Supply Nodes | Spawn/remaining quantities; one-time collection rules | SupplyCollected, NodeDepleted |
| **R02** | Shared Stock / Recipes | Atomic cost checks and consumption | StockChanged, SpendRejected |
| **N01** | Shift Clock | Clock, phase transitions, synchronization | PhaseChanged |
| **N02** | Incident Director | Choose allowed incidents, target anchors, cooldown | IncidentTriggered, IncidentResolved |
| **N03** | Radio / Dialogue | Queue, priority, localization, listener visibility | RadioCall, ReplyMade |
| **N04** | Objectives / Story State | Jobs, evidence, clue flags, shift results | ObjectiveChanged, ClueLogged |
| **N05** | Damage / Repair | Fixture and build condition, repair transaction | FixtureDamaged, FixtureRepaired |
| **X01** | Save / Restore | Placed objects, supplies, fixtures, story, shift, incident history | SaveComplete, RestoreComplete |

**Example: repair transaction** `interact with breached fence → target anchor → candidate repair pose → validate + cost → reserve/commit shared boards → place patch → update gate security state → radio acknowledgement → persist`.

**Example: shared scare** `night clock + story prerequisite → choose eligible site anchor → replicate major audio/visual cue to nearby participants → record who witnessed it → update shared evidence/damage if applicable → unlock radio follow-up → persist incident ID`.

**Guiding constraint:** Systems own facts; narration and incident presentation *react* to them. Never let a scare script directly subtract resources, bypass placement validation, or silently mutate the saved world.

## 11. Milestones and decisions to refine

| Milestone | Build | Acceptance gate |
|---|---|---|
| **M1 — Security building** | Camp fixture anchors, grid placement, fence/door repair pieces | Solo placements flush, no overlapping or illegal repairs |
| **M2 — Cooperative material loop** | Shared stock, supply nodes, atomic repair costs | One to four people can collect and repair without duplication |
| **M3 — First shift** | Clock, radio contact, patrol objectives, incident cues | Night watch feels purposeful before any jump scare |
| **M4 — Story scare and aftermath** | One clue, one scare, one piece of damage, shift report | Both solo and co-op understand what happened; consequence persists |
| **M5 — Tone playtest** | Visibility/audio/accessibility tuning, pacing, radio writing | Players can explain their job, tell a memorable story, and want Night Two |

**High-priority design questions:**

1. **Does the game begin with a single first-night contract, or does the buyer promise several shifts up front?** Recommendation: several nights, but prototype one.
2. **Who exactly is the radio voice?** Recommendation: a nearby ranger/operations contact with a personal history with the camp; not necessarily the player's employer.
3. **What does securing the camp mean mechanically?** Recommendation: guard 2–3 key assets per shift (main gate, generator, maintenance shed) with a simple report card.
4. **What is the consequence of failing a security objective?** Recommendation: persistent damage, loss of supplies and altered dialogue; avoid instant mission failure at first.
5. **Can guards physically confront trespassers?** Not in this scope. Keep the initial threats indirect and defer combat and AI.
6. **How strongly supernatural should the game ultimately become?** Leave unrevealed in Chapter One; decide before outlining the full campaign.
7. **Should security fortification remain constrained to specific anchors, or grow into freeform construction?** Begin with validated anchors and a modular grid, with later controlled expansion.

**What changed from v0.2:** The game is no longer locked to single-player; it supports one to four guards. The location is a **pre-existing abandoned summer camp awaiting demolition**, not a wilderness plot being reopened. Resource collection is **salvage for the job**, not primarily tree-chopping. Building is **site fortification and repair**, not mandatory cabin construction. The radio contact is integrated with a short security assignment and its murder-history mystery. The nighttime structure is a **guard shift**, influenced by the tension of *Five Nights at Freddy's* rather than survival waves.

---

*Working document. Specifications and examples are proposals for iteration, not approved code tasks or canon plot twists. Weapon, enemy-AI, multiplayer transport, and full narrative solutions are intentionally left for later design passes.*
