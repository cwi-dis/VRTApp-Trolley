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

**Next session starts here:**
- Open `TrolleyBystander` scene
- Follow Steps 1–7 (see below) to wire it up fully
- Test in editor before moving to other scenes

---

## Scene Wiring Checklist

### TrolleyBystander — IN PROGRESS
Steps to complete:
- [ ] Create `TrolleyController` empty → add `TrolleyController` component, scenarioID = "bystander"
- [ ] Create `NarrationPlayer` empty → add `NarrationPlayer` + `Audio Source`
- [ ] Create `TimerCanvas` (World Space Canvas) → child `TimerText` (TMP) → add `DecisionTimer`, wire TimerText
- [ ] Create train waypoints: `TrainPaths/ActionPath/Waypoint1...` and `TrainPaths/InactionPath/Waypoint1...`
- [ ] Add `TrainController` to `train_typeB`, wire train + waypoints + worker animators
- [ ] Create `Lever` empty with child cube + `LeverPivot` → add `XRGrabInteractable` + `TrolleyLever`, wire pivot
- [ ] Wire all references into `TrolleyController` inspector
- [ ] Test in editor (solo mode)

### TrolleyDriver — NOT STARTED
- Same as Bystander but swap `TrolleyLever` → `TrolleyButton`, scenarioID = "driver"

### TrolleyOptional — NOT STARTED
- Same as Driver but enable `Has Wall Collision` on `TrainController`, add wall + collision effect

### TrolleyQuestionnaire — NOT STARTED
- Two booths ~25m apart with opaque wall between them
- `QuestionnaireController` with `TrolleyQuestions` asset wired in
- Reflection timer UI + Likert button panels in each booth
- Spatial audio: participants far enough apart that voice doesn't bleed

### TrolleyTutorial — NOT STARTED
- `TrolleyGameState` + `DataLogger` already placed (DontDestroyOnLoad)
- Add `TutorialController` → researcher setup panel UI (condition + scenario order buttons)
- Add `AvatarSelector` → male/female buttons → wire male/female Mixamo prefabs (pending avatar files)
- Practice lever + practice button (same components, no consequences)

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

- **Mixamo avatar files** — not yet added. Avatar selector UI is built but prefabs not wired. Add male/female FBX to `Assets/Trolley/Models/` when ready.
- **Narration audio** — placeholder mode active (4s delay). Add real AudioClips to `Assets/Trolley/Audio/` per scenario when ready.
- **Quest build** — not attempted yet. Scheduled for Day 5.

---

## Timeline

| Day | Goal | Hours |
|---|---|---|
| 2 | Wire TrolleyBystander, test in editor | 4–5h |
| 3 | TrolleyDriver + TrolleyOptional | 4–5h |
| 4 | TrolleyQuestionnaire scene | 4–5h |
| 5 | TrolleyTutorial + full session flow | 4–5h |
| 6 | Quest build + on-device testing + fixes | 4–5h |

Target completion: ~5 working days from 2026-05-01.
