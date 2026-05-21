# VRTApp-Trolley Development Log

## Study Overview
Social VR trolley problem experiment. Solo and paired conditions. 3 scenarios, counterbalanced.
Full protocol: `protocol.md`

---

## Progress

### Day 1 (2026-04-30) — ~1 hour
**Done:**
- All 13 C# scripts written and compiling (no errors)
- Editor utility `TrolleySceneSetup.cs` created and run → 5 scenes created
- All 5 scenes added to Build Settings
- `TrolleyQuestions` ScriptableObject created (placeholder questions)
- `TrolleyGameState` + `DataLogger` GameObjects added to `TrolleyTutorial` scene
- Everything committed and pushed to master

---

### Day 2 (2026-05-01) — ~4 hours total
**Done:**
- All 5 scenes fully wired via per-scene editor setup scripts (run from Trolley menu)
- `TrolleyBystander`: train, 2 workers per track, waypoints, lever (red), timer canvas
- `TrolleyDriver`: same but with button instead of lever
- `TrolleyOptional`: button + wall collision + particle burst effect; action track has no workers
- `TrolleyQuestionnaire`: two-booth black room, dim point lights, opaque divider, full Likert UI per booth; Next button, scale endpoint labels, transition panel added
- `TrolleyTutorial`: researcher setup panel (condition + 6 counterbalanced orders), avatar selector wired to Man/Woman prefabs, practice lever + button
- `TrolleyController` switched from `PilotController` to `MonoBehaviour` — fixes "multiple PilotController instances" error
- `DecisionTimer`: status text ("Narration playing…") shown before countdown starts
- `ScenarioRegistry` updated in VRTLoginManager; scenes added to Build Settings
- Man/Woman avatar FBX added to `Assets/Trolley/Models/`, prefabs linked to AvatarSelector
- Everything committed and pushed to master

---

### Day 3 (2026-05-04) — ~4 hours
**Done:**
- Bystander narration drafted and finalised (see `protocol.md`)
- Narration MP3 added to `Assets/Trolley/Audio/` and linked to NarrationPlayer
- `Assets/Trolley/Audio/` folder created; placeholder WAVs committed
- Train approach now starts during narration (not after); speed auto-calculated from actual clip length via `NarrationPlayer.TotalDuration`
- `SceneFader.cs` added — blackout fade between scenes; 2s hold on black before fade-in on scene load
- `WavUtility.cs` added — saves AudioClip as WAV for voice recordings
- `DecisionTimer` repositioned in front of camera (HUD-style) when shown
- `QuestionnaireController` updated: shows consequence text ("You decided to pull the lever…"), Record/Stop buttons for voice reflection, saves WAV to persistentDataPath
- `DataLogger.LogReflection()` added
- `TrolleyGameState.lastDecision` added
- All setup scripts updated to use `TrackedDeviceGraphicRaycaster` instead of `GraphicRaycaster`
- Do NOT re-run setup scripts — manual Inspector assignments (audio clips, AudioSource) are lost on re-wire

**Bugs found during on-device test:**
- Train timing was off — approach was hardcoded to 38s but narration clip is longer (~55s). Fixed by passing actual clip length to TrainController at runtime.
- No ray visible from VR2Gather controller → UI raycast clicking does not work

**Next session starts here:**
- **BLOCKER: UI interaction** — VR2Gather player has no XRRayInteractor; controller ray does not appear. Two options:
  1. Ask senior dev (before May 14 holiday) how to add ray interactor to VR2Gather player
  2. Reimplement questionnaire navigation using controller buttons (thumbstick + trigger) — bypasses ray entirely
- Wire Driver and Optional scenes (run setup scripts, assign audio manually after)
- Draft Driver and Optional narration scripts
- Quest build + on-device test

---

### Day 4 (2026-05-07) — Protocol alignment + Avatar Setup scene

**Context:** Reviewed `STUDY_PROTOCOL_v2_May2026_SL.md` against current Unity build and implemented all gaps. The HUD countdown timer in the Bystander scene is intentional and correct — the protocol text had an error (now fixed in the .md file).

**Done:**

**TrolleyGameState.cs — complete rewrite:**
- Removed `Gender` enum; replaced with `AvatarBodyType` (Masculine/Feminine)
- Added `AvatarHeight` enum (Short/Medium/Tall)
- `RelationshipType` updated: removed `Acquaintance`, renamed `Partner` → `RomanticPartner`, added `Colleague`
- `scenarioOrder` default: `TrolleySelfHarm` (was `TrolleyOptional`)
- Added avatar fields: `avatarBodyType`, `skinToneIndex`, `hairColorIndex`, `avatarHeight`
- Added `selfHarmControllerSlot` (int, 0 or 1 — counterbalanced, controls asymmetric self-harm action)
- Added scene name fields: `avatarSetupScene`, `questionnaireScene`, `endScene`
- Added helpers: `IsSelfHarmController(bool isMaster)`, `AvatarConfigString()`

**DataLogger.cs — complete rewrite:**
- New `InteractionAttempt` struct: `participantId` (string), `unixMs` (long)
- `LogDecision` extended: triggeredBy, responseTimeSec, narrationEndTime, windowStartTime, windowEndTime, List<InteractionAttempt>, competitionFlag
- CSV headers updated: gender→bodyType, added avatarConfig, timestamps, interaction attempts, competition flag
- `Meta()` uses `avatarBodyType` and `AvatarConfigString()`

**TrolleyController.cs — targeted additions:**
- Captures `_narrationEndTime` and `_windowStartTime` in `OnNarrationComplete()`
- `OnLocalActionTriggered()`: logs attempt, broadcasts `"interaction:attempt:<id>:<unixMs>"`, then decision
- `OnNetworkMessage()`: handles `"interaction:attempt"` to accumulate remote attempts
- Competition detection: both attempts within 500ms → `competitionFlag = true`
- Self-harm asymmetric control: non-controlling participant's interactable disabled

**QuestionSet.cs — complete rewrite:**
- `postScenarioCommon`: 7 items (agency, responsibility, satisfaction, consequence, time pressure, omission, felt real)
- `postScenarioPairedOnly`: 2 items (partner influence, awareness of partner)
- `postScenarioSelfHarmOnly`: 1 item (felt virtual self in danger)
- `itcSopiItems`: 6-item ITC-SOPI co-presence scale (Likert5)
- `closenessItem`: 1-item closeness measure (Likert7)

**QuestionnaireController.cs — targeted edits:**
- `reflectionDuration`: 15s → 60s
- `BuildConsequenceText()`: handles "selfharm" with steering framing + reflection prompt; paired adds partner question
- `RunQuestionnaire()`: Q10 conditional on selfharm scenario; ITC-SOPI block after scenario 2 (paired only)
- `ShowITCSOPI()` coroutine added; ITC-SOPI logs under `"itcsopi"` scenario ID

**AvatarSelector.cs — targeted fixes:**
- All 4 selection methods made `public`: `SelectBodyType`, `SelectSkinTone`, `SelectHairColor`, `SelectHeight`
- Null guards added throughout `Start()` (fixes NullReferenceException when serialized refs reset)

**TutorialController.cs — targeted fix:**
- `rels` array: replaced `Acquaintance`/`Partner` with `Stranger`, `Colleague`, `Friend`, `RomanticPartner`
- `BeginStudy()` now loads `avatarSetupScene` ("TrolleyAvatarSetup") instead of first scenario directly

**AvatarSetupController.cs — new file:**
- Handles avatar customisation scene (between Tutorial and first scenario)
- Finds all UI by `GameObject.Find()` at runtime — no Inspector wiring needed
- Solo: Confirm immediately loads first scenario; Paired: broadcasts `"avatar:ready"`, waits for partner
- Wires AvatarSelector buttons (`SelectBodyType`, `SelectSkinTone`, `SelectHairColor`, `SelectHeight`)

**TrolleyAvatarSetupSceneSetup.cs — new Editor script:**
- Menu: `Trolley > Wire Avatar Setup Scene`
- Duplicates `TrolleyTutorial.unity` → `TrolleyAvatarSetup.unity` (preserves VR2Gather/XR rig)
- Strips researcher UI; builds full avatar selection canvas (body type, skin tones ×6, hair colours ×6, height, status text, confirm button)
- Uses `GraphicRaycaster` temporarily (XRRayInteractor still missing — same blocker as questionnaire UI)

**TrolleyPlayerPositions.cs — new Editor script:**
- Menu: `Trolley > Set Player Positions`
- Target: copy P1=(0,0,-0.5) P2=(0.6,0,-0.5) to Tutorial, AvatarSetup, Bystander, Driver, TrolleyOptional
- Questionnaire keeps booth positions: P1=(0,0,-0.5), P2=(0,0,-30.5)
- **Status: script written but player spawn path `Tool_SceneSetup/Player Initial Locations/Player 1` does not exist in Tutorial scene — VR2Gather may handle spawn differently. Needs investigation tomorrow.**

**STUDY_PROTOCOL_v2_May2026_SL.md:**
- Corrected timer description: "no visible countdown timer" → accurate description of HUD timer that exists

---

### Day 5 (2026-05-08) — Scene recovery + TrolleySelfharm + questionnaire flow fix

**Context:** Discovered commit c650eb9 (Jack's GraphicRaycaster fix) had re-run setup scripts and wiped all manual Inspector state from 4 scenes. Full recovery + several pending items from Day 4 completed.

**Done:**

**Scene recovery (c650eb9 damage):**
- Restored TrolleyBystander, TrolleyDriver, TrolleyQuestionnaire, TrolleyAvatarSetup via `git checkout <pre-damage commit> -- <scene>` 
- Manually reapplied DisableUserMovement prefab override (8-line YAML, fileID 1154743739069929190) to all 4 restored scenes

**DisableUserMovement — all scenario scenes:**
- Applied to: TrolleyBystander, TrolleyDriver, TrolleyQuestionnaire, TrolleyAvatarSetup
- TrolleyTutorial excluded (researcher operates UI during setup)
- Override: `m_Enabled: 1`, `autoDisable: 1` on DisableUserMovement component in P_Self_Player prefab

**TrolleyOptional → TrolleySelfharm rename:**
- `git mv` TrolleyOptionalSetup.cs → TrolleySelfharmSetup.cs; all references updated across 6 files
- TrolleyOptional.unity deleted; TrolleySelfharm.unity is new (must run setup script to populate)

**TrolleySelfharmSetup.cs (cliff variant of Driver):**
- Menu: `Trolley > Wire Selfharm Scene`; ScenePath: `TrolleySelfharm.unity`
- 5 workers on InactionTrackWorkers (center z=22, spacing 1.2); no action-track workers
- Cliff geometry: CliffFace brown cube (5×6×2 at 4,2,31) + CliffEdge flat cube (at 4,−1,29)
- CliffCollisionEffect: dusty brown particle burst; scenarioID: `"selfharm"`

**Avatar swatch blackout bug — fixed (AvatarSelector.cs):**
- Root cause: `SetHighlight()` overwrote swatch `Image.color` with flat blue/grey, losing original swatch colors permanently
- Fix: `CaptureColors()` records base colors at `Start()`; `HighlightSwatchGroup()` dims unselected swatches to `base × 0.4f` instead of replacing with a flat color

**TrolleyTutorialSetup.cs:**
- Removed Male/Female avatar row (avatar selection is in AvatarSetup scene, not Tutorial)
- All rows shifted up 0.07 to fill gap; OrderLabels/OrderScenes updated to TrolleySelfharm convention
- Added TrolleyGameState + DataLogger singleton creation — no longer needs manual placement

**QuestionnaireController.cs:**
- ITC-SOPI moved from after scenario 2 to after all 3 scenarios (`!HasMoreScenarios()`)
- Selfharm consequence text: "You diverted the train into the cliff, saving the five workers. The impact put your own safety at risk."

**Height selection removed:**
- `AvatarSelector.cs`: removed shortButton, mediumButton, tallButton, SelectHeight()
- `TrolleyGameState.cs`: removed AvatarHeight enum and avatarHeight field
- `AvatarSetupController.cs`, `TrolleyAvatarSetupSceneSetup.cs`, `TrolleyAvatarUISetup.cs`: all height wiring removed

**Committed and pushed** to branch `1-vr2gather-14`.

**Must do in Unity Editor before testing (deadline: 2026-05-15):**
1. Re-run `Trolley > Wire Tutorial Scene`
2. Run `Trolley > Wire Selfharm Scene` (new)
3. Re-run `Trolley > Wire Avatar Setup Scene`
4. Add `TrolleySelfharm.unity` to Build Settings manually
5. Recreate `TrolleyQuestions` ScriptableObject (QuestionSet.cs fields changed)

**Must test (deadline: 2026-05-15):**
- Full flow: Tutorial → AvatarSetup → Scenario → Questionnaire × 3 → ITC-SOPI (paired) → end
- CSV output: decisions + questionnaire files appear in `Application.persistentDataPath`
- DisableUserMovement: no locomotion in scenario/questionnaire scenes
- Swatch selection: original colors preserved; unselected swatches dimmed to 40%
- TrolleySelfharm: cliff geometry, worker placement, button trigger, particle burst

**Next session starts here:**
- **BLOCKER (highest priority):** XRRayInteractor missing from VR2Gather player — buttons unclickable in headset. Ask Jack before May 14 holiday.
- Wire TrolleySelfharm scene narration (draft narration script; assign audio after running setup)
- Driver narration script not yet drafted
- Avatar body type prefab swap: not yet implemented (visual only — no functional gap in current flow)
- Skin/hair color: visual application to avatar mesh not yet implemented
- Quest build not attempted

---

### Day 7 (2026-05-21) — Bystander scene finishing + Driver scene environment-movement rewrite

**Context:** Continued from Day 6. Finished Bystander scene CCTV monitor layout. Rewrote Driver scene to use environment-movement approach (TrackEnvironment moves toward stationary player) to simulate driver-cab first-person perspective. Multiple console error fixes for solo editor testing.

**Done:**

**TrolleyBystanderSetup.cs — CCTV monitor layout:**
- 4 RenderTextures + 4 CCTV cameras in 2×2 grid mounted on control room wall
- Monitor grid derived from manually placed single monitor in scene (position extracted from scene YAML)
- All container `SetParent` calls use `false` — fixes `(-1000,0,0)` offset bug caused by `worldPositionStays=true` default when parenting to TrackEnvironment at x=1000

**Console error fixes (solo editor testing without VR2Gather backend):**
- `TrolleyController.cs`: null-safe all `VRTOrchestratorSingleton.Comm` accesses; added `hasSession` guard so `SendTypeEventToAll` is only called when `SelfUser != null`; `DataLogger.Instance?.LogDecision()`
- `TrainController.cs`: added `modelForwardYaw` field; `TriggerWorkers` now checks animator parameter existence before calling `SetTrigger` — prevents hash-not-found errors
- `TrolleyController.cs`: `trainController` null check in `BeginNarration()`

**TrolleyDriverSetup.cs — environment-movement rewrite:**
- `TrackEnvironment` root starts at world `(0,0,60)` and moves toward player at origin — environment slides past as if you're in the cab
- Workers (`InactionTrackWorkers` ×5, `ActionTrackWorkers` ×1) are children of `TrackEnvironment` at local positions; ride with the environment until the fork
- Waypoints (`TrackPaths`) are root-level world-space objects — fixed targets for `TrainController`
- Approach: `z=60 → z=30 → z=0`; Inaction: `z=-20 → z=-50`; Action: `(3,0,-15) → (6,0,-40)`
- `TrainController.train` = `TrackEnvironment.transform` (no train mesh needed)
- Setup script reuses existing `TrackEnvironment` instead of destroying it — preserves manually placed track geometry (StraightRail)
- `OpenScene` skipped if scene already open — prevents reload discarding unsaved manual changes
- Old root-level `TrainPaths`, `InactionTrackWorkers`, `ActionTrackWorkers` added to cleanup list
- Approach duration: 76s (half speed from original 38s)
- GitHub issues #6 (Bystander) and #7 (Driver) updated with progress comments

**Committed and pushed:** `TrolleyDriverSetup.cs`, `TrolleyDriver.unity`, `TrolleyBystander.unity`

**Next session starts here:**
- Wire SelfHarm scene (replicate Driver environment-movement approach into `TrolleySelfharmSetup.cs`)
- Draft narration scripts for Driver and SelfHarm scenes
- Avatar setup swatch wiring fix (carry from Day 6)
- Quest build + on-device test

---

### Day 6 (2026-05-14) — Avatar Setup scene: two-station layout + material swap

**Context:** Continued avatar setup scene implementation. Protocol fixes from previous sessions now applied; focus was on getting real-time avatar customisation working in the scene.

**Done:**

**Scene renames finalised:**
- `TrolleyTutorial` → `TrolleyResearcherSetup` (researcher operates this, not participant)
- `TutorialController` → `ResearcherSetupController`
- `TrolleyTutorialSetup.cs` → `TrolleyResearcherSetupSceneSetup.cs`
- Build Settings updated; `TrolleyPlayerPositions.cs` updated to exclude ResearcherSetup and AvatarSetup from auto-position script

**Protocol fixes applied:**
- `QuestionSet.cs`: Q4 text corrected; Q10 moved to postScenarioCommon; Q11 (partner presence) added to postScenarioPairedOnly; postScenarioSelfHarmOnly removed
- `QuestionnaireController.cs`: removed selfharm-only block; all scenarios say "button" in consequence text; null guard on DataLogger.Instance
- `TrolleyController.cs`: removed self-harm asymmetric control block
- `TrolleyGameState.cs`: removed selfHarmControllerSlot; RelationshipType simplified to `{ NotApplicable, Stranger, Close }`
- `DecisionTimer.cs`: duration 5s → 8s
- `ResearcherSetupController.cs`: relationship buttons simplified to Stranger/Close only

**AvatarSetupController.cs — complete rewrite:**
- Removed all `GameObject.Find()` calls
- Two-station architecture: `stationARoot`, `selectorA`, `confirmButtonA`, `statusTextA` + B equivalents
- Solo: only Station A active; Paired: both active
- In paired: master's confirm button = Station A; non-master = Station B (other disabled)
- Network sync via `TrolleyAvatarReadyMessage`; master relays to all; both load next scene when both confirmed

**AvatarSelector.cs — material swap approach:**
- Removed `MaterialPropertyBlock` entirely (was causing serialisation exception)
- Added `skinToneMaterials[6]` and `hairColorMaterials[6]` SerializedFields
- `SelectSkinTone` / `SelectHairColor` now call `SwapMaterial` → sets `renderer.sharedMaterial`
- `Awake()` removed; `_mpb` removed; `_BaseColor` push removed

**TrolleyAvatarSetupSceneSetup.cs — full rewrite:**
- Single wide canvas (1440×680) with two sub-panels: StationA (left, blue P1 header) and StationB (right, brown P2 header)
- Each panel: body type buttons, 6 skin swatch buttons, 6 hair swatch buttons, status text, Confirm button
- `AvatarPreview_A` and `AvatarPreview_B` placeholder GameObjects at (±1.1, 0, 3.2) facing participants
- `AvatarSelector_A` and `AvatarSelector_B` created and wired via `SerializedObject`
- `AvatarSetupController` wired with all SerializedField refs

**TrolleyAvatarMaterialsCreate.cs — new Editor script:**
- Menu: `Trolley > Create Avatar Materials`
- Creates 6 skin tone mats (Light → Darkest) and 6 hair colour mats (Black → Grey)
- Clones `M_Lever.mat` via `AssetDatabase.CopyAsset` (avoids Shader.Find null), then clears all inherited textures and sets `_Color` / `_BaseColor`
- 12 `.mat` files saved to `Assets/Trolley/Materials/`

**Pending (carry to next session):**
- `skinToneButtons` array on `AvatarSelector_A` is likely empty — swatch button clicks not reaching listener
- Fix: re-run `Trolley > Wire Avatar Setup Scene`, then reassign avatar previews and renderers manually
- Avatar body renderers and hair renderers still need assigning in Inspector after FBX hierarchy inspection

---

## Key Decisions & Patterns

**Network sync pattern (TrolleyController):**
- Whoever triggers the physical action calls `SendMessageToAll("decision:action:<playerID>")` AND applies outcome locally
- Other client applies on receipt via `OnUserMessageReceivedEvent`
- `SendMessageToAll` does NOT echo to sender — always handle locally + broadcast
- Timer start: master broadcasts `"timer:start"`, master also starts locally; non-master starts on receipt
- Inaction: each client handles timer expiry locally (timers are in lockstep from master broadcast)

**Interactable guard:**
- `TrolleyInteractable` base class has `_triggered` bool — first press wins, subsequent ignored
- Call `SetActive(false)` after decision to disable further interaction

**Scene flow (DontDestroyOnLoad):**
- `TrolleyGameState` and `DataLogger` live in Tutorial scene, persist across all scenes
- `TrolleyGameState.AdvanceScenario()` called in `TrolleyController.TransitionOut()` before loading questionnaire
- `QuestionnaireController` reads `lastCompletedScenarioID` for logging, `NextScenarioScene()` for next scene

**Lever (TrolleyLever):**
- Uses XRGrabInteractable + monitors `Quaternion.Angle(leverPivot.localRotation, _restRotation)`
- Threshold: 40 degrees from rest position
- `leverPivot` is a child transform that physically rotates — assign in inspector

**Button (TrolleyButton):**
- Uses XRSimpleInteractable, fires on `selectEntered`
- Optional `buttonMesh` moves down by `pressDepth` for visual feedback

**Data output location:**
- `Application.persistentDataPath` on Quest = `/sdcard/Android/data/<packagename>/files/`
- Two CSV files per session: `decisions_<timestamp>.csv` and `questionnaire_<timestamp>.csv`

---

## Pending / Blockers

- **UI interaction in VR** — XRRayInteractor missing from VR2Gather player; buttons unclickable in headset. Affects questionnaire AND avatar setup scene. Ask senior dev before May 14 holiday. Highest priority.
- **Shared button state sync** — Implementation approach known; not blocked.
- **Player spawn path** — ✓ Resolved.
- **TrolleyAvatarSetup in Build Settings** — must be added manually after running `Trolley > Wire Avatar Setup Scene`.
- **TrolleyQuestions ScriptableObject** — delete and recreate in Unity; `QuestionSet.cs` has new question arrays.
- **Driver + SelfHarm narration** — not yet drafted. Bystander narration done.
- **Driver + SelfHarm scenes** — not yet wired. Run setup scripts, assign audio manually (do NOT re-wire Bystander).
- **Quest build** — not attempted yet.
- **Driver scene perspective** — Train_Type B model not ideal for inside-the-cab view; may need rethinking.
- **Another developer has open branch** — do not commit/push until branch is merged.

---

## Timeline

| Day | Goal | Status |
|---|---|---|
| 1 | Scripts + scene scaffolding | ✓ Done |
| 2 | Wire all 5 scenes + questionnaire UX + bug fixes | ✓ Done |
| 3 | Narration audio + train timing + fade transitions + raycaster fix | ✓ Done |
| 4 | Protocol alignment + avatar setup scene + DataLogger/QuestionSet rewrites | ✓ Done |
| 5 | Scene recovery + TrolleySelfharm + swatch fix + questionnaire flow | ✓ Done |
| 6 | Avatar setup scene: two-station layout, material swap, 12 materials created | ✓ Done |
| 7 | Bystander CCTV monitors; Driver environment-movement rewrite; solo editor fixes | ✓ Done |
| 8 | Fix swatch wiring; wire Selfharm scene; narration scripts | Next |
| 9 | Quest build + on-device test | — |
| 10 | Fixes from on-device test | — |

Target completion: ~3–4 weeks from 2026-04-30.
