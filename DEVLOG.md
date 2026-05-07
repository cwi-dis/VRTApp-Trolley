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

**Next session starts here:**
- **Player positions:** Find actual player spawn path in VR2Gather scenes (not `Tool_SceneSetup/Player Initial Locations/Player 1` — that path doesn't exist). Check with other developer or inspect Bystander scene in Unity editor.
- **TrolleyAvatarSetup in Build Settings:** Add `TrolleyAvatarSetup.unity` to Build Settings after running the Wire Avatar Setup Scene editor tool.
- **TrolleyQuestions ScriptableObject:** Delete and recreate in Unity — `QuestionSet.cs` now has new question arrays that won't populate in the existing asset.
- **UI interaction blocker:** Still unresolved — XRRayInteractor missing from VR2Gather player. Ask senior dev before May 14 holiday.
- **Do NOT re-run old per-scene setup scripts** — manual Inspector assignments (audio clips, AudioSource) would be lost.
- Wire Driver and SelfHarm scenarios (narration scripts not yet drafted).

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
| 5 | UI interaction fix + player spawn fix + Driver/SelfHarm scenes + narration | Next |
| 6 | Quest build + on-device test | — |
| 7 | Fixes from on-device test | — |

Target completion: ~3–4 weeks from 2026-04-30.
