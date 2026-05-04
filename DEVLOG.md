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

- **UI interaction in VR** — XRRayInteractor missing from VR2Gather player; buttons unclickable. Ask dev or switch to controller-button navigation. Highest priority.
- **Driver + Optional narration** — not yet drafted. Bystander narration is done.
- **Driver + Optional scenes** — not yet wired. Run setup scripts, then manually assign audio clips and AudioSource references (do NOT re-wire Bystander).
- **Quest build** — not attempted yet.
- **Driver scene perspective** — Train_Type B model not ideal for inside-the-cab view; may need rethinking.

---

## Timeline

| Day | Goal | Status |
|---|---|---|
| 1 | Scripts + scene scaffolding | ✓ Done |
| 2 | Wire all 5 scenes + questionnaire UX + bug fixes | ✓ Done |
| 3 | Narration audio + train timing + fade transitions + raycaster fix | ✓ Done |
| 4 | UI interaction fix + Driver/Optional scenes + narration | Next |
| 5 | Quest build + on-device test | — |
| 6 | Fixes from on-device test | — |

Target completion: ~3–4 weeks from 2026-04-30.
