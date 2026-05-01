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

### Day 2 (2026-05-01) — ~2.5 hours
**Done:**
- All 5 scenes fully wired via per-scene editor setup scripts (run from Trolley menu)
- `TrolleyBystander`: train, 2 workers per track, waypoints, lever, timer canvas
- `TrolleyDriver`: same but with button instead of lever
- `TrolleyOptional`: button + wall collision + particle burst effect; action track has no workers
- `TrolleyQuestionnaire`: two-booth black room, dim point lights, opaque divider, full Likert UI (7 buttons) per booth
- `QuestionnaireController` updated: dual Booth A/B refs — master uses A, non-master uses B (privacy without separate scenes)
- `TrolleyTutorial`: researcher setup panel (condition + 6 counterbalanced orders), avatar selector wired to Man/Woman prefabs, practice lever + button
- `ScenarioRegistry` updated in VRTLoginManager — TrolleyTutorial (+ other scenes for debugging) added
- Man/Woman avatar FBX added to `Assets/Trolley/Models/`, prefabs created and linked to AvatarSelector
- Everything committed to master

**Next session starts here:**
- Full flow test via VRTLogin → Create Room → TrolleyTutorial → Solo → run one complete scenario
- Fix bugs found during test
- Quest build (Android target)

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

- **Narration audio** — placeholder mode active (4s delay). Add real AudioClips to `Assets/Trolley/Audio/` per scenario when ready.
- **Quest build** — not attempted yet. Next priority after editor test passes.
- **Full flow test** — needs VRTLogin → TrolleyTutorial run to catch runtime bugs.

---

## Timeline

| Day | Goal | Status |
|---|---|---|
| 1 | Scripts + scene scaffolding | ✓ Done |
| 2 | Wire all 5 scenes + avatar setup | ✓ Done |
| 3 | Full flow editor test + bug fixes | Next |
| 4 | Quest build + on-device test | — |
| 5 | Fixes from on-device test | — |

Target completion: ~3–4 weeks from 2026-04-30.
