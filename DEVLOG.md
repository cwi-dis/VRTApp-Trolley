# VRTApp-Trolley Development Log

## Study Overview
Social VR trolley problem experiment. Solo and paired conditions. 3 scenarios, counterbalanced.
Full protocol: `protocol.md`

---

## Progress

### Day 20 (2026-06-30) — Decision-window data fix, timer/narration UI removal, seat–button layout (#77, #18)

**Context:** Post-first-experiment iteration. Committed to `master` as `42e10ce`.

**`windowStartTimestamp` fix — `42e10ce` (#77):**
- `TrolleyController.OnNarrationComplete()` set `_narrationEndTime` and `_windowStartTime` to `DateTime.Now` on consecutive lines, so the two were identical by construction in every decision log. Moved `_windowStartTime` into a new `StartDecisionWindow()` helper that sets it immediately before `decisionTimer.StartCountdown()`, and routed both the master/solo path (`OnNarrationComplete`) and non-master path (`OnTimerStart`) through it. `windowStartTimestamp` now marks the actual countdown start on each client — the zero point `responseTimeMs` is measured from. Solo/master: still ~identical to narration end (correct by design — window + train both start then); non-master: now reflects the network-delayed timer start instead of being mislogged.
- Posted a follow-up on #77 flagging two related data issues for Jack: `responseTimeMs` is effectively constant (~window duration) because the decision only commits at timer expiry, and press timestamps (UTC Unix ms) vs window timestamps (local string) are in different time bases. Left for Jack to review before changing the stats format.

**Decision-timer HUD removed — `42e10ce` (#18):**
- Added a `showHud` flag to `DecisionTimer` (default off) gating the numeric countdown text. Countdown, `OnTimerExpired`, and reaction-time logging are unchanged — the approaching train is now the only time-pressure cue (matches the moral-dilemma VR literature, which uses environmental pressure, not a visible counter).

**"Narration playing…" label — `42e10ce`:**
- The static `StatusText` TMP label (cosmetic, not referenced by any script, not synced to narration) was still showing in Driver/Selfharm/TutorialDriver. Deactivated the GameObject in those three scenes, matching the two Bystander scenes where it was already off.

**Seat–button centering and layout — `42e10ce`:**
- Found the Driver track buttons sat ~10 cm left of the seat midpoint (favouring Player1: 0.40 m vs 0.60 m horizontal reach). Centred Button_TrackA/B on the seat midpoint (x=0.059) in Driver/Selfharm/TutorialDriver (shift +0.10175 in world x, spacing and −20° tilt preserved).
- Decided to leave both players where they are: solo player stays at the left seat so solo is physically identical to a paired Player1 (only the partner's presence differs — the IV). No runtime reposition.
- Added `TrolleyBystanderLayoutAdapt.cs` (`Trolley > Adapt Driver Layout`): reads the Driver seat↔button geometry from the live hierarchy and reproduces it in Bystander + TutorialBystander (seats 1.0 m apart, symmetric around the console buttons), keeping Player1's height. Ran it — bystander seats now 1.0 m apart (−3.59437 / −2.59437). Vertical reach in bystander is ~10 cm shorter than driver (lower seat + lower console); left as-is since the environments differ and the seat height is monitor-tuned.

**Position analysis note:** Player/button transforms live in nested prefab instances (package `Tool_scenesetup` → `Tool_scenesetup_Trolley` variant → scene instance) with fileID remapping, so static scene-file parsing is unreliable for seats — use the live-hierarchy editor tools instead.

---

### Day 19 (2026-06-30) — Data pipeline hardening, questionnaire logic fix, arm IK fix (#58, #75, #76)

**Context:** Post-first-experiment session focused on data logging reliability and two bug fixes surfaced by the experiment run.

**Questionnaire logic fix — `2cafeb1` (#75):**
- Practice questionnaire was incorrectly skipping the think-aloud reflection when `questionnaireInVR` was false. Full behaviour matrix: inVR+practice → reflection→Likert→done; inVR+real → reflection→many Likert→done; !inVR+practice → reflection→done; !inVR+real → reflection→paper-form prompt→done. Restructured `RunQuestionnaire()` so `ShowReflection()` is always called first.

**Stats mirroring and DataLogger hardening — `cd77b70`, `38b3670`, `f0f0b7d`, `1d05f7d`, `039d1e9` (#58):**
- Decision data and avatar selection now mirrored to the VR2Gather `stats:` output via `Cwipc.Statistics.Output()`, guarded by `#if VRT_WITH_STATS`.
- `DataLogger` made always-on (removed `SetExportEnabled`, `StartSession` now private and called from `Awake()`). Moved from ResearcherSetup scene (no longer in flow) to `TrolleyAvatarSetup` scene as a DontDestroyOnLoad object.
- `ResearcherSetupController` retired from session flow; `Awake()` logs error and disables the component if ever activated.
- `TrolleyGameState.Awake()` detects duplicate instances (second session without quit) and sets `StaleSession = true`; `AvatarSetupController.Start()` shows an error message in that case.
- Stats output file renamed from `stats-from-develop.json` to `stats.log`.
- Issue #77 filed: `narrationEndTimestamp` and `windowStartTimestamp` are identical in all decisions — `_narrationEndTime` in `TrolleyController` is not recorded separately. Assigned to Suzy.

**Arm IK elbow fix — `96a872f` (#76):**
- Both avatar prefabs had elbows bending upward. Root cause: the Two Bone IK pole-vector hint was placed almost at the elbow (Vector3.back * 0.2f in world space), giving a near-degenerate pole vector. Fixed manually in both prefabs, and `TrolleyAvatarPrefabSetup.cs` updated to use `Vector3.back * 0.7f + Vector3.down * 0.4f` for future re-runs.

---

### Day 18 (2026-06-29–30) — Experiment prep: avatar constraint fixes, position adjustments, questionnaire mute (#71)

**Context:** Branch `53-avatars` was merged and tagged `exp-20260629.1` — the first experiment run. The day started with final avatar constraint fixes, then in-editor position adjustments for sitting players, and closed with a quick fix to mute cross-participant audio in the questionnaire scene.

**Avatar constraint fixes — `5142296`, `56429f3` (53-avatars):**
- **Body Constraint disabled** (`MultiPositionConstraint`) on both avatar prefabs — without it, the Humanoid Animator resets hips to T-pose every frame and `SyncSkeletonToVRRig.neck.Map()` accumulates a delta each frame, sending hips to ~y=42.5 m after ~10 seconds in VR. Enabled `MultiPositionConstraint` on both prefabs.
- **Female TwoBoneIK all disabled** — all four `TwoBoneIKConstraint`s on `P_Avatar_Trolley_Female` were also disabled, leaving it with no IK at all. Enabled all four.
- Setup script now explicitly sets `MultiPositionConstraint.enabled = true` after `AddComponent` to prevent Play Mode baking from leaving it disabled in the prefab again.

**Position adjustments for sitting players — `e24c31c`, `fe9c89c`, `ca44cfd`, `9461fa6` (master):**
- Player spawn heights in `Tool_scenesetup_Trolley.prefab` adjusted for seated participants — now correct for Driver, Driver Tutorial, and Self-harm scenes.
- Additional position tweaks to `TrolleyBystander` and `TrolleyDriver` scenes.
- "Banana Man" placeholder asset disabled in `TrolleyDriver`.
- Bystander tutorial player positions fixed.

**Experiment run:** `53-avatars` merged into `master`, tagged `exp-20260629.1`.

**Questionnaire audio mute — `007b9d5` (#71, 2026-06-30):**
- Participants should not hear each other during the questionnaire, but microphones must stay on for local `RecordUserVoice` recording.
- Fix: set `AudioSource.mute = true` on the `Voice` child of the `P_Player_Trolley` instance embedded in `Tool_scenesetup_Trolley` within the questionnaire scene. `SessionPlayersManager.PlayerPrefab` points to this in-scene instance, so every `Instantiate(PlayerPrefab)` copies the mute flag. `VoicePipelineOther.Init()` only sets `audioSource.enabled = true`, never clears `mute` — flag survives initialisation. Confirmed working.

---

### Day 17 (2026-06-28) — Avatar debugging: female tracking dead, hand pose, barrier double-fire, SizeAdjust workaround

**Context:** Pre-experiment debugging session on `53-avatars`. Four fixes, all on the avatar prefabs and `AvatarSetupController`.

**Female avatar completely unresponsive to VR tracking — `10a549f`:**
- `SyncSkeletonToVRRig` had been left disabled on `P_Avatar_Trolley_Female` after an IK sanity check (the check requires temporarily disabling it). The component was never re-enabled before applying prefab overrides.
- Fix: re-enabled in the prefab. Added a warning to `CLAUDE.md` avatar workflow: after a sanity check, re-enable all three components (`SyncSkeletonToVRRig`, `SizeAdjust`, `PlayerRepresentationWirer`) before applying overrides — a disabled `SyncSkeletonToVRRig` in the saved prefab makes the avatar completely unresponsive.

**Hand pose fix — `46fa59a`:**
- `P_Self_Player_Trolley` had `leftHand`/`rightHand` tracking targets pointing to the raw `Left/Right Controller` transforms. Switched to `RiggingAttachPointLeftHand`/`RiggingAttachPointRightHand`, which are calibrated to the actual wrist position and orientation. Fixes hand pose on both avatars. Ref: VR2Gather#332.

**Avatar selection barrier firing on first Confirm — `b7c4ad9`:**
- The barrier that waits for both players to confirm their avatar was firing after only one player confirmed. Root cause: `OnAvatarUpdate` was calling `readyTrigger.Trigger()` when it received the partner's `isDone` message, so the local barrier received two triggers (one from local `OnLocalConfirm`, one echoed back via the partner). Fix: removed the `readyTrigger.Trigger()` call from `OnAvatarUpdate` — the barrier is now driven solely by each player's own `OnLocalConfirm()`.

**SizeAdjust disabled as visibility workaround — `d4513a4`:**
- Avatars were not visible on the other player's machine (or on a non-VR desktop). Disabled `SizeAdjust` on both `P_Avatar_Trolley_Male` and `P_Avatar_Trolley_Female` as a temporary diagnostic step to isolate the cause.

---

### Day 15 (2026-06-23) — Pilot fixes: tutorial flow, questionnaire matrix, narration levels

**Context:** First working session after the Jun 22 pilot with Jack. Worked through issues #59 (tutorial
issues) and #61 (driver scene design), then reworked the post-scenario questionnaire. Six commits, all
pushed to `origin/master`.

**Tutorial fixes (#59) — `2de3748`, `1af19ed`, `f149489`:**
- **Divert bug** — in the bystander tutorial the blue train went straight, then "appeared" on the side
  track a few seconds later. Cause: the drill advanced the train at a fixed spline-*t* rate, not constant
  world speed; after the fork the branch's short segments crawl at that t-rate. Fixed `TutorialTrainDrill`
  to move at constant world speed, like the real `TrainController`.
- **Bystander drill** cut 5 → 3 rounds (BLUE/RED/BLUE), slower + shorter run, shorter gap, with a "next
  train approaching" narration before each round (new clip).
- **Order reversed to driver-first** (Jack's call — simpler concept first), via `TrolleyGameState.preSequence`.
  Narration still says "first/second tutorial" + buttons only taught in bystander → flagged for a re-record.
- **Researcher skip button** — grey `Button_Skip` prefab + `TutorialSkipButton`; editor menu
  instantiates/adopts it and repoints the networked button's `OnTrigger → Skip()`.
- **Start button + A/B warm-up** — green Start button (`TutorialGate` + `TrolleyTutorialStartSetup`): the
  tutorial opens with the A/B buttons live for free practice, then waits for Start (fixes "too fast"; also
  gives button exposure before the now-first driver drill). Null-safe fallback to `startDelay`.
- **Ghost disabled** (Banana Man inactive), control-room ceiling enabled, monitors + workers repositioned
  (Suzy, in-editor).

**Questionnaire rework — `e784a40`:**
- All 19 post-scenario items → **7-point** Likert.
- **Grouped pages** by construct (3/3/5/3 common + 5 paired) instead of one question per screen.
- Rebuilt the panel as a **Likert matrix**: a shared word-anchor column header (Strongly disagree … Strongly
  agree) + a row of circles (built-in Knob sprite) per question, neutral colours (no red/green gradient — by
  request), no construct titles (avoids biasing answers). Response indices unchanged. Thin **bottom progress
  bar** fills page/total (solo 4, paired 5). `QuestionnaireController` + `TrolleyQuestionnaireSetup` +
  `QuestionSet`; practice scene rebuilt to match.

**Narration volume — `2f3a5c4`, `9d2ccde`:** all narration to **50%** — `narrationVolume` field on both
tutorial drills and on `NarrationPlayer` (covers Bystander/Driver/Self-harm). SFX untouched.

**Open for next session:** questionnaire in-headset layout check (tune `colsLeft`/`rowGap`/circle size);
confirm the Start button is in the driver tutorial scene; #61 geometry (window, board height, train floor);
verify #59 failure feedback + the full driver-first flow.

### Day 16 (2026-06-25) — #53 avatars: merge master, loader/appearance scripts, foot colliders off

**Context:** Resumed work on branch `53-avatars`. Merged `master` (two commits ahead on
`P_Self_Player_Trolley`), then implemented the runtime avatar selection and appearance system.
Six commits on `53-avatars`.

**Merge resolution — `67961ce`:**
- `CLAUDE.md`: kept both sides — avatar setup rows (53-avatars) + `Wire GazeTargets` row (master).
- `P_Self_Player_Trolley.prefab`: took `--theirs` (master) to preserve GazeTarget/movement fixes
  from `905aa59` and `cf9pb35`, then re-ran `Wire as AltRepOne` (Male) + `Wire as AltRepTwo` (Female)
  to restore avatar wiring.
- **Bug fixed in `WireViewAdjusted`:** idempotency check was matching on `m_MethodName` alone, so
  wiring a second avatar overwrote the first's `viewAdjusted` slot. Now matches on both `m_Target`
  and `m_MethodName` — each avatar gets its own persistent call entry.

**Foot colliders disabled — `3348949`:**
- `BoxCollider` + `KeepFeetAboveGround` disabled on all four leg IK Target GOs in both avatar prefabs.
- Feet now clip through the floor on deep crouch rather than colliding — acceptable for this
  experience (people won't bend much; seated users will have lower body hidden behind virtual chairs).
- Setup script default (colliders on) preserved for other use cases.

**Runtime avatar loader — `494c312`:**
- `TrolleyAvatarConfig` extracted from `VRTPilotConfig.cs` into its own file.
- `TrolleyAvatarLoader` (new MonoBehaviour on both player prefabs): at `Start()`, determines config
  index via `TrolleyGameState.LocalAvatarConfigIndex` / `OtherAvatarConfigIndex` (master=0,
  non-master=1), maps `bodyType` ("Masculine"/"Feminine") to `AppDefinedRepresentationOne/Two`,
  calls `SetRepresentation()`, then calls `TrolleyAvatarAppearance.ApplyConfig()` on the newly
  active altRep.
- `TrolleyAvatarAppearance` (new MonoBehaviour, attach to avatar prefabs): `MaterialPropertyBlock`
  tinting of `Body` and `Hair` `SkinnedMeshRenderer` children. Color arrays populated in Inspector
  to match `AvatarSelector` swatches — **pending Sueyoon**, who owns the color values.
- `TrolleyGameState` + `VRTPilotConfig` GO added to `TrolleyAvatarSetup` scene so they persist via
  `DontDestroyOnLoad` into all subsequent scenes (previously relied on the now-removed researcher
  setup scene).
- Tested in solo mode with `pilotconfig.json` — avatar selection works. Tinting no-ops until
  color arrays are populated.

**Open for next session:** populate `TrolleyAvatarAppearance` color arrays (Sueyoon); remaining
#53 items; `xxxclaude` comment in `TrolleyGameState.LocalAvatarConfigIndex` to clean up.
Issue #65 opened for dead researcher-setup scene/scripts (assign to Sueyoon).

**Day 16 continued — #54 start: appearance colors, mirror layout — `2cc7b07`, `dece250`:**
- Read `TrolleyAvatarSetupSceneSetup.cs` and extracted the two color tables (`SkinTones`,
  `HairColors`) that `AvatarSelector` uses. Added `Trolley > Wire Trolley Avatar Appearance Colors`
  menu item to `TrolleyAvatarPrefabSetup.cs` — loads both avatar prefabs directly via
  `AssetDatabase.LoadAssetAtPath`, populates `TrolleyAvatarAppearance.skinToneColors` /
  `hairColors`, saves. Kept in a clearly labelled section noting it is Trolley-specific (Remy +
  Megan). Run and verified.
- `TrolleyAvatarSetup` scene: added `OBJ_Mirror`, repositioned and rotated UI panels for the
  side-by-side facing-mirror layout. Tested solo — can see own avatar in mirror.

**Open for next session:** `AvatarSelector` live reload (`TrolleyAvatarLoader.Reload()`), broadcast
on every parameter change (not just confirm), `OnAvatarReady` reload on other player. Static preview
models to remove or hide. Full paired test.

### Day 14 (2026-06-19) — Arrow button graphics; driver tutorial reworked to a rock-blocker drill

**Context:** Picked up Day 13's two open threads — the placeholder A/B button labels and the unfinished
driver tutorial — then redesigned the driver tutorial's practice mechanic and locked the final narration.
Four commits landed on master (not pushed): `f0c2a39` buttons, `3aa00d4` first (signal-light) driver
tutorial — **superseded the same day**, `47d75e1` final narration + scripts, `e22d8d6` rock-blocker
rework + player-spawn copy.

**Button graphics — A/B text → arrows (commit f0c2a39):**
- The decision buttons showed literal "A" / "B" TMP labels. Replaced them with arrow icons: a straight-up
  arrow for TrackA (stay on main track / inaction), an elbow-right arrow for TrackB (divert / action).
- **Generated the PNGs myself** (`Assets/Trolley/Textures/button_arrow_straight.png`,
  `button_arrow_divert.png`) — 256×256, white fill + dark edge, transparent bg, 4× supersampled, via a
  throwaway Pillow venv. White reads on both the grey (unselected) and green (selected) button states.
- **Editor script** `TrolleyButtonGraphicSetup.cs` (menu `Trolley > Buttons – Swap A_B Labels To Arrow
  Graphics`): disables (doesn't delete) the TMP label, adds a child `ArrowIcon` quad decal anchored on
  the label transform, strips its collider so it never blocks the XR interactable, assigns the icon
  material. Idempotent — re-runnable.
- **Pink-button fix:** first pass rendered magenta. Cause = URP doesn't resolve the URP/Unlit surface
  keywords on a bare material. Switched the icon material to **`Sprites/Default`** (built-in, URP-safe,
  unlit + alpha-blended) with a force shader-reset to repair the already-created material.
- Both buttons are **prefab variants**, so editing the two prefab assets propagates the graphic to all
  five scenes at once (Bystander, Driver, Self-harm, both tutorials).

**Driver tutorial — first built as a signal-light drill (commit 3aa00d4):** 3 reps with a red/blue signal
ahead (blue = divert, red = stay), fade-to-black reset between reps (`SceneFader.FadeFromBlack`), score
canvas `… / 3`. This shipped, then got **replaced the same day** — see below.

**Driver tutorial — redesigned to a rock-blocker drill (commit e22d8d6):**
- **Signal light → rocks.** Suzy's call: a colour-coded light is an arbitrary rule to memorise; a physical
  obstacle is self-explanatory and fits the first-person cab (you steer away from what's blocking you).
  Two rocky-mountain barriers (reusing the self-harm look), one per track — the **blocked** track is the
  one to avoid (rocks on main → divert; rocks on side → stay). Built by `TrolleyDriverTutorialSetup` at
  the captured worker positions, so they ride in on the correct track.
- **Buttons no longer re-taught** — already practised in the bystander tutorial. Narration cut to **4
  clips**: intro → window → sortingtrain (rules) → closing.
- **Divert no longer fires early.** The turn now triggers at `approachDistance = 76` (= the real Driver
  scene's 9.5 speed × 8 s decision window) instead of a short 60, so it lines up with the fork.
- **Train keeps moving** — removed the 4 s dead stop between reps; it now rolls continuously, the reset is
  hidden under the fade, and a shared `_traveled` counter keeps the fork aligned despite the motion.
- **Beep held** until the train reaches the rocks (`beepAfterForkDistance`) — was firing at the fork (too
  early). Decision still locks silently at the fork.
- **Neutral button state** during the intro (`SetNeutral()`); was pre-selecting A.

**Player spawns (commit e22d8d6):** `TrolleyDriver` corrected by hand (canonical). New editor menu
`Trolley > Copy Player Locations: Driver → Selfharm + TutorialDriver` copies the `Player Initial
Locations/Player1+2` world transforms into the other two scenes (finds them inside the
`Tool_scenesetup_Trolley` prefab by parent name).

**Still open (Suzy, in Unity):**
- Confirm `Approach Distance = 76` on `TutorialDriverDrill` in the scene (C# default updated; the existing
  scene still had 60) and playtest the driver tutorial in headset — divert-at-fork, continuous motion,
  beep timing, neutral start.
- Check `RockBlocker_Main` / `RockBlocker_Side` sit cleanly on their tracks; the side "stay" round may read
  weakly from the cab (angle/scale it up if so).
- Eyeball the arrow icon facing on a button; optionally flip the divert arrow to bend left.

### Day 13 (2026-06-18) — Bystander tutorial polish, neutral buttons, room shell, real questionnaire, Tutorial 2 (driver) scaffolding

**Context:** Suzy recorded all bystander-tutorial narration and iterated through playtests with Claude
(full agency to edit scripts/scenes through commit). Big day: finished the bystander tutorial, applied
the "neutral until pressable" button behaviour to the real scenes, built room tooling, loaded the real
questionnaire, renamed the tutorial scene, and scaffolded the second (driver) tutorial.

**Bystander tutorial (`TutorialTrainDrill.cs`) — finished + reworked:**
- Intro is now **per-monitor, clip-driven**: preamble (`intro` + `monitors`, monitors blinks all four
  rims) then one clip per monitor; each rim (and, for main/side, its A/B button) blinks for exactly its
  clip's length — sync is automatic, no `monitorHighlightTimes`. Button pulse colour is **green**.
- **Neutral start:** both buttons unselected / no rim lit through the whole intro; the default selection
  only appears at button practice. Button practice no longer blinks (real-scene feedback).
- **Uniform 2s pause** after every narration clip (`betweenClipsPause`; replaced introPauseAfter/preSortPause).
- **Round-2 trains time-based** (`roundStartT`/`roundEndT`/`roundDuration`) — the world-speed value was
  crawling over the ~1000-unit Bystander spline. Span halved (0.25→0.75) at half speed.
- **Divert fixed:** the train switches tracks AT the fork (deferred divert) and runs the branch fully, so
  it shows on the divert monitor instead of vanishing. **Sound** plays when the train reaches the fork.
- **Closing clip** after 5 correct; `nextSceneAfterDrill` (was `practiceQuestionnaireScene`) chains the
  flow **Bystander tutorial → Driver tutorial → one practice questionnaire**.
- 10 narration recordings + generated `sfx_correct/wrong.wav`; non-destructive
  `Trolley > Tutorial – Assign Narration & SFX Clips` wires all 12.

**Neutral buttons in the REAL scenes (`TrolleyToggleDecision.cs`):** the toggle starts neutral and only
reveals the current selection when `SetInteractionEnabled(true)` is called (decision window open). Purely
visual — `IsAction`/decision logic unchanged. Covers Bystander, Driver, Selfharm (shared toggle).

**Room tooling (new editor scripts):** `Trolley > Build Control Room Shell` (encloses the console after
its prefab shell was disabled) and `Trolley > Copy Room Layout: Tutorial → Bystander` (copies world
transforms of MonitorGroup/MonitorLabelGroup/ControlRoomShell/buttons/gaze so the Bystander room matches
the rescaled tutorial). Applied to Bystander.

**Real questionnaire (`TrolleyQuestions.asset` + `QuestionSet.cs`):** loaded the full item set —
**14 common** (agency ×3, responsibility ×3, decision-evaluation ×5, threat/seriousness ×3) and **5
paired-only** (partner influence). All Likert5 / "Strongly disagree–agree" (group 3 can be switched to
7-point if wanted). Controller iterates the arrays, so the count change is safe.

**Scene rename:** `TrolleyTutorial` → **`TrolleyTutorialBystander`** (file+meta, Build Settings path
in place, `TrolleyGameState.tutorialScene`, ScenarioRegistry data[2], layout-copy path). Build Settings
ORDER left to Suzy.

**Tutorial 2 — Driver (new, scaffolded):**
- `TutorialDriverDrill.cs` — standalone, mirrors the two-round structure but uses **environment-movement**
  (TrackEnvironment slides toward the seated player; divert yaws about DivertMarker, same rate as
  `DriverTrainController`). Round 2 = **signal-light drill**: a light ahead turns BLUE (divert) / RED (stay),
  5 reps; tram switches at the fork; sound at the fork.
- `TrolleyDriverTutorialSetup.cs` — `Trolley > Build Driver Tutorial From Driver` duplicates Driver, copies
  movement params off `DriverTrainController`, strips it + TrolleyController + both worker groups, adds the
  drill + SignalLight + score + SFX + narration source. **Does NOT touch Build Settings.** Plus a
  non-destructive `Driver Tutorial – Assign Narration & SFX Clips` menu.
- Driver narration script drafted in `NARRATION_SCRIPTS.md` (Suzy records `narration_tutorial_driver_*.mp3`).

**End-of-day state (2026-06-18):**
- ✅ Bystander tutorial: narration recorded + wired; playtested through several fixes. The divert-at-fork
  + sound-timing fix (commit `e200eb0`) was committed but is the last thing pending a fresh playtest.
- ✅ Neutral buttons, room tooling, real questionnaire, scene rename — committed.
- ⏳ **Driver tutorial:** code + setup written but NOT run/tested. Suzy to: run `Build Driver Tutorial From
  Driver`, ADD `TrolleyTutorialDriver` to Build Settings, record driver narration + run the assign menu,
  place SignalLight/score canvas, tune `approachDistance`/`postForkDistance`, playtest.
- ⏳ Bystander `closingClip` field not yet serialized in the scene — run the Assign Clips menu (or drag) to wire it.

**Committed** to `master` (NOT pushed) — 11 commits this session: audio, drill, toggle, room tooling,
questionnaire, rename, Tutorial 2 scaffolding, DEVLOG, Assign-menu robustness, bystander clip wiring,
generated driver scene. ✅ Suzy ran the Assign menu (bystander clips wired) and `Build Driver Tutorial
From Driver` (TrolleyTutorialDriver scene now exists), both committed.

**Open for tomorrow (Day 14):**
1. **Narration audit (all scenes except bystander tutorial, which is done):** real scenes reference
   `Narration_Bystander.mp3` (Bystander), `narration_driver.mp3` (Driver), `narration_selfharm.mp3`
   (Self-harm) — verify the content is current. **Driver tutorial: 8 clips still to record**
   (`narration_tutorial_driver_*.mp3`). Stale/unreferenced duplicates: `Narration_Bystander.wav`,
   `Narration_Driver.wav`, `Narration_Optional.wav` (safe to delete later).
2. **Button UI → graphic:** the A/B buttons are text ("A"/"B") in every scene; replace with a graphic/icon
   throughout (Bystander, Driver, Self-harm, both tutorials). Note `TrolleyToggleDecision` recolours the
   button renderer by name ("Button" child) — a graphic swap should keep that renderer reachable.

---

### Day 12 (2026-06-17) — Tutorial redesigned into TWO ROUNDS + practice questionnaire

**Context:** Suzy reviewed the Day 11 single-round drill and reworked it into a **two-round** tutorial (guided button round, then a sorting drill). She records a new 4-clip narration. Constraint: stay in **tutorial-only** scripts/scene — do NOT touch shared controllers (TrolleyController, etc.). Permission granted through commit (master, not pushed). Can't run Unity — deliverables are code + setup scripts; not compile-checked in Editor.

A single-round first pass earlier in the session (no-timer, 10-train, /10) was reworked into the two-round version below before committing. The practice-questionnaire half (and the no-timer / spatial-commit mechanic) carried over from that pass unchanged.

**TutorialTrainDrill.cs — rewritten as a two-round flow (still fully standalone, touches no shared controller):**
- **Round 1 — button familiarisation (guided).** Plays an intro clip describing the four CCTV monitors; each monitor's green rim **blinks in turn** as it's named (`monitorHighlightTimes`, tunable to the recording), then a 3s pause (`introPauseAfter`). Then "press to divert" → button B blinks, waits for the **real** B press (side monitor lights via the toggle); "now change it back" → button A blinks, waits for the real A press. Reuses the existing `TrolleyToggleDecision` (A=main/RimA, B=divert/RimB) and the four monitor rims — no new highlight system.
- **Round 2 — sorting drill.** Fixed order **RED, BLUE, BLUE, RED, BLUE** (5 trains, was 10), ~10s apart (`interRoundDelay`). **No timer** — decision commits spatially when the train passes `divertThreshold`. Counter `Correct decisions: N / 5` (hidden during Round 1, shown for Round 2).
- Ends by loading the **practice questionnaire** scene (`practiceQuestionnaireScene`), then the first real scenario.
- Narration is now **four separate clips** (`introClip`/`pressClip`/`backClip`/`sortClip`) on a dedicated `narrationSource` — separate clips let the flow genuinely wait for each button press. Removed the old single `narrationPlayer` path.
- Button prompt blink uses a bright contrasting colour (button A starts green-selected, so green-on-green wouldn't show). After each guided press, `toggle.ApplyRemoteState(...)` re-asserts the toggle's own colours/rims.

**TrolleyTutorialSetup.cs — wiring for the two rounds:**
- Reads `buttonA`/`buttonB`/`rimA`/`rimB` straight off the `ToggleDecision`'s serialized fields (the buttons are inside a prefab, so `GameObject.Find` by name won't reach them).
- **Clones the existing rim** onto `Monitor_WestView` + `Monitor_SwitchPoint` (→ `RimApproach`/`RimSwitch`) so all four monitors can blink — reuses the same rim object, not new code.
- Creates a `TutorialNarration` AudioSource, loads the 4 clips if present (warns + leaves null otherwise), wires everything, silences the carried-over Bystander NarrationPlayer. Score canvas default → `/ 5`.

**Narration:** `NARRATION_SCRIPTS.md` tutorial section rewritten as the 4-clip, two-round script (intro ~80w, press ~13w, back ~16w, sort ~46w) drafted from Suzy's outline. Files: `narration_tutorial_{intro,press,back,sort}.mp3`.

**Driver tutorial:** Suzy will add one later (same pattern, explained then) — out of scope today.

**Practice ("fake") questionnaire — reuses the real `QuestionnaireController` via a new `practiceMode`:**
- `QuestionnaireController.cs`: added `practiceMode` (default **false** — real questionnaire unchanged), `practiceQuestionSet`, `practiceNextScene`. In practice mode: generic reflection prompt (`BuildConsequenceText`), **no DataLogger writes** (answers + reflection skipped), skips the paired-only block, transition text says "the study is about to begin", and `ExecuteSceneLoad` loads the **first real scenario** (`NextScenarioScene()` — doesn't advance the index, so no scenario is consumed). Participants still practise the slider + Done/Record controls and (paired) the partner-sync barrier.
- New `TrolleyPracticeQuestionnaireSetup.cs` — `Trolley > Build Practice Questionnaire From Questionnaire`: duplicates `TrolleyQuestionnaire.unity` → `TrolleyPracticeQuestionnaire.unity` (saveAsCopy, preserves hand-tuned booths/UI), flips `practiceMode=true`, creates+assigns `PracticeQuestions.asset` (2 dummy Likert items), adds to Build Settings. Non-destructive to the real scene.

**Housekeeping:** added `voicerecording-*.wav` to `.gitignore` (9 test recordings were cluttering the root, per Day 11 note).

**Late fix (shared util, not a controller):** `GazeDetector.cs` threw `MissingReferenceException` on scene unload — `m_CurrentTarget?.NotifyGazeExit()` in `OnDisable`/`Update` used `?.`, which ignores Unity's destroyed-object state, so a `GazeTarget` torn down with the scene still got called. Switched to Unity-aware `if (m_CurrentTarget != null)` checks. Pre-existing bug, surfaced while testing the tutorial transitions.

**End-of-day state (2026-06-17):**
- ✅ Suzy rewired the two scripts in-Editor (`TutorialTrainDrill` two-round + `GazeDetector` fix confirmed working).
- ⏳ The four narration MP3s are NOT recorded yet — Suzy records them tomorrow.

**Tomorrow — start here:**
1. **Record the 4 clips** and drop them in `Assets/Trolley/Audio/` with exact names: `narration_tutorial_intro.mp3`, `_press.mp3`, `_back.mp3`, `_sort.mp3` (scripts in `NARRATION_SCRIPTS.md`). Then **drag each onto its field** on `TutorialTrainDrill` (`introClip`/`pressClip`/`backClip`/`sortClip`) — do NOT re-run `Build Tutorial From Bystander`, it would wipe the manual rewire + rim nudging. (Re-running only auto-assigns clips; not worth losing the tweaks.)
2. **Tune `monitorHighlightTimes`** on `TutorialTrainDrill` so each rim blinks when the intro clip names that monitor (default `1,5,9,13`s).
3. **Assign ding/buzz** to `DrillSFX` (`correctClip`/`wrongClip`) — no such asset exists yet.
4. **Set `divertThreshold`** to the actual switch point on the rail; tune `trainSpeed`.
5. **Run `Trolley > Build Practice Questionnaire From Questionnaire`** if not done — creates the after-scene `TrolleyPracticeQuestionnaire`.
6. **Playtest the full chain:** Round 1 (intro blinks + button practice waits for real B then A press) → Round 2 (5 trains R,B,B,R,B, counter /5) → practice questionnaire → first scenario.
7. **Driver tutorial** — Suzy to explain; build with the same two-round pattern.

**Committed** to `master` (NOT pushed) in three grouped commits: (1) tutorial two-round flow + scene, (2) practice questionnaire + scene/asset, (3) GazeDetector fix + chore/docs. Includes Suzy's in-Editor rewire of `TrolleyTutorial.unity` and the generated `TrolleyPracticeQuestionnaire` scene + `PracticeQuestions` asset. Compile not verified by Claude (no Unity here); the two-round + gaze scripts confirmed working in-Editor by Suzy.

---

### Day 11 (2026-06-16) — Selfharm-from-Driver, Tutorial-from-Bystander, questionnaire P2 fix (Claude, autonomous session)

**Context:** Suzy asked for the three remaining items this week, then left for the day and approved running everything (no commit/push — she commits after reviewing output). Constraint: do NOT touch ResearcherSetup or AvatarSetup. Claude cannot run Unity menus or test 2 clients, so deliverables are code + scripts to run in Unity.

**⚠️ Self-harm action/inaction conflict — flagged for Suzy:**
- Verbal instruction during session: "inaction means self-harm (rocky mountain)".
- BUT `STUDY_PROTOCOL_v2` §Scenario C says **action (steer into side barrier) = self-harm; inaction = the five die**, and the existing `QuestionnaireController.BuildConsequenceText` + `narration_selfharm.mp3` both already encode action=self-harm.
- Built it **per the protocol** (action = divert into rocky-mountain barrier on the side track; inaction = five workers ahead) to avoid silently reversing the H2b self-sacrifice mapping. Flipping is a small change in `TrolleySelfharmSetup` + `BuildConsequenceText` + re-recorded narration if she really wants inaction=self-harm.
- **RESOLVED (2026-06-16):** Suzy confirmed **action = self-harm** (tram diverts right into the rocky mountain), **inaction = the five die** — matches the protocol + existing code. No flip, no narration re-record needed.

**Task 1 — `Trolley > Build Selfharm From Driver` (rewrote `TrolleySelfharmSetup.cs`):**
- Old script (spline `TrainController`, build-from-scratch) was stale — Driver now uses `DriverTrainController` (environment-movement). New script **duplicates TrolleyDriver.unity → TrolleySelfharm.unity** (SaveScene saveAsCopy, preserves Driver's hand-tuned cab/tram/movement), sets `scenarioID=selfharm`, replaces the single side-track worker with `RockyMountain_SelfHarm` (boulder cluster) + `SelfHarmImpactEffect` dust burst, rewires `DriverTrainController` (actionHitWorkers=null, inactionHitWorkers=the five, actionImpactEffect wired), assigns `narration_selfharm.mp3`, adds to Build Settings.
- `DriverTrainController.cs`: added optional `actionImpactEffect` + `impactOnAction` — fires the burst hitDelay after the chosen outcome (null/no-op in Driver).

**Task 2 — `Trolley > Build Tutorial From Bystander` (new `TrolleyTutorialSetup.cs`) — redesigned mid-session as a colour DRILL:**
- Suzy clarified the tutorial is a practice mini-game, not the single-decision flow: a sequence of trains one at a time, each RED or BLUE — **RED = do nothing (runs left/straight), BLUE = press the button (diverts right)** — with a top-right `Correct: X / 10` counter and a ding/buzz per round (5 red + 5 blue, shuffled). She asked for a *completely separate* controller so the real `TrolleyController` is untouched.
- **`TutorialTrainDrill.cs` (new, standalone):** self-contained spline follow (reuses the Bystander rail: spline 0 straight/left, 1 branch/right), recolours the train each round, reads `TrolleyToggleDecision.IsAction` for input, scores, plays SFX, then loads the first real scenario (`NextScenarioScene()`). Paired = independent (each player runs their own drill). Does NOT touch TrolleyController.
- **Setup script** duplicates Bystander → Tutorial, removes both worker groups, **removes the TrolleyController + TrainController components from this scene only**, hides the idle TimerCanvas, builds a `DrillScoreCanvas` (world-space, reposition to taste) + `DrillSFX`, and wires `TutorialTrainDrill`. Adds to Build Settings.
- `TrolleyController.isTutorial` flag + `TrolleyGameState.tutorialScene` field still added (harmless; the drill supersedes the isTutorial path but the flag remains a general capability).
- Tutorial narration script added to `NARRATION_SCRIPTS.md`.
- **MANUAL (left for Suzy):** assign ding/buzz clips (correctClip/wrongClip) + `narration_tutorial.mp3`; reposition DrillScoreCanvas; tune trainSpeed/decisionWindow; to run in the flow, point `AvatarSetupController` at `TrolleyGameState.tutorialScene` (one line — AvatarSetup is off-limits to Claude).

**Task 3 — questionnaire "Player 2 sees blank booth":**
- `QuestionnaireController.cs`: added a null-session guard (paired + no orchestrator no longer NREs) and a diagnostic log (role / booth A|B / camera pos / distance to that booth's panel — a large distance = player spawned at the wrong booth, i.e. a spawn-placement bug not a UI bug).
- `TrolleyQuestionnaire.unity`: corrected the Booth B player spawn to match Booth A's working offset, shifted −30 in z: x −5 → **−5.05**, z −33.4 → **−32.05** (Booth A player is −5.05/−2.05; Booth B canvas is 30 units further). This was Suzy's in-progress edit; values were inconsistent with Booth A.
- The diagnostic will confirm in-headset whether the remaining cause (if any) is placement vs. VR2Gather assigning both players to the same spawn slot.

**End-of-day state — committed (4 atomic commits on `master`, NOT pushed):**
- `981316c` Selfharm · `0b863c8` Tutorial · `e13331e` Questionnaire · `f02a480` Docs.
- Compile fix mid-session: `TutorialTrainDrill` `Random.Range` was ambiguous (Unity.Mathematics vs UnityEngine) → fully qualified to `UnityEngine.Random`.
- ✅ **Selfharm:** built via the new menu + verified working in the Editor by Suzy.
- ✅ **Tutorial drill:** scene re-built with `TutorialTrainDrill`, compiles. NOT yet polished or playtested.
- ✅ **Questionnaire P2 fix:** committed. NOT yet 2-client tested.
- 8 `voicerecording-*.wav` test recordings left untracked (not committed). A `.gitignore` rule `voicerecording-*.wav` would hide them.

**Tomorrow — start here:**
1. **Tutorial polish:** on `TutorialTrainDrill`, assign `correctClip`/`wrongClip` (ding/buzz — no such asset exists yet) and `narration_tutorial.mp3`; reposition `DrillScoreCanvas` to the top-right of the player's view; playtest and tune `trainSpeed` (6) / `decisionWindow` (3s).
2. **Tutorial in flow (optional):** point `AvatarSetupController.ExecuteLoad` at `TrolleyGameState.tutorialScene` (one line; AvatarSetup is yours to edit, off-limits to Claude).
3. **Selfharm:** position/scale `RockyMountain_SelfHarm` so the divert visibly crashes into it; tune `DriverTrainController.hitDelay`. Camera shake on impact is still a TODO (touches the XR rig — VR2Gather/Jack territory).
4. **Questionnaire:** 2-client test; read the `[Questionnaire]` log line on P2 — want `booth=B` + small distance. If distance is large, the cause is VR2Gather assigning the spawn slot, not the transform (next thing to dig into).

---

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

**Also done (Day 8 continuation):**
- Added 81-720 Yauza tram GLB (via GLTFast package) as exterior in Driver scene
- Tram exterior positioned outside front window; interior remains DriverCabShell with control panel adapted
- Closes "Train_Type B not ideal" blocker

### Day 8 (2026-05-22) — Bystander scene polish + TrainController rewrite

**Done:**

**TrolleyBystander.unity — coordinate cleanup:**
- Rail coordinates aligned (x=0 main track, Rail.L x=101.6, z values regularised)
- Monitor y positions fixed (floating-point artifact 1.7281251 → 1.728)
- Train start position corrected (z=38 → z=−302); all approach waypoints moved to z=392
- modelForwardYaw set to 180 (train mesh faces −Z)
- New prefabs added: DivertingRail.prefab, TrackEnvironment.prefab

**TrainController.cs — full rewrite:**
- Replaced 3-phase approach/wait/branch logic with simple start→end linear movement
- Fields: `train`, `startPoint`, `endPoint`, `decisionWindowSeconds` (8s), `ambientAudioSource`
- Speed auto-calculated: distance / (narrationDuration + decisionWindowSeconds)
- `StartApproach(float narrationDuration)` called by TrolleyController with NarrationPlayer.TotalDuration
- Ambient audio loops while moving, stops on arrival

**NarrationPlayer.cs — loop fix:**
- Added explicit `audioSource.loop = false` before each clip plays
- Renamed header to "Audio — narration (plays once, no loop)" to distinguish from ambient source

**Also done (same session):**

**TrolleyDriverSetup.cs — rewrite to match new TrainController API:**
- Replaced `approachPath`/`inactionPath`/`actionPath` waypoint arrays with `StartPoint` (0,0,60) and `EndPoint` (0,0,−60)
- Removed `approachDuration` — speed now auto-calculated by TrainController (distance ÷ narration + 8s)
- Removed `inactionTrackWorkers`/`actionTrackWorkers` from TrainController wiring
- Added ambient AudioSource on TrackEnvironment, wired to `ambientAudioSource`
- Narration AudioSource: explicit `loop = false`, `playOnAwake = false`
- Timer display corrected: "5.0" → "8.0"
- Fixed Unity `??` null-check trap — replaced with explicit `if == null` guards
- Added `SetSerializedProp` helper with null guard so missing fields warn instead of abort
- Light creation moved to before TrainController wiring so it survives any later exception

---

### Day 10 (2026-06-09) — Bystander spline-based train + CCTVBlackout restore + UI polish

**Done:**

**TrainController.cs — full rewrite (spline-based):**
- Replaced waypoint/endpoint logic with Unity Splines package (`com.unity.splines 2.2`)
- Single `SplineContainer rail` field with 2 splines: index 0 = straight (inaction), index 1 = branch (action)
- `StartApproach()`: uses `GetNearestPoint` to start from train's placed position in scene (no jump to t=0)
- `ExecuteAction()`: uses `GetNearestPoint` to find equivalent position on spline 1 — train continues smoothly without restarting
- Removed `ambientAudioSource`, `startPoint`, `endPoint`, `actionEndPoint` fields
- `trainSpeed` = world units/sec; `modelForwardYaw = 180` for train mesh facing −Z

**TrolleyDriverSetup.cs / TrolleySelfharmSetup.cs:**
- Replaced `StartPoint`/`EndPoint`/`ActionEndPoint` waypoint creation with `Rail` SplineContainer (2 splines)
- Added `using UnityEngine.Splines`; old endpoint wiring removed

**CCTVBlackout.cs — restored:**
- Was deleted in previous session; restored from git history
- `TrolleyController` now has `[SerializeField] CCTVBlackout cctvBlackout` (optional)
- Blackout fires after configurable `blackoutDelay` (default 2s) via `Invoke`

**DecisionTimer.cs:**
- Removed `statusText` field entirely — user doesn't need "Narration playing…" text
- `StatusText` GameObject disabled in Bystander scene

**TimerCanvas — Bystander:**
- Scale reduced 50% (0.005 → 0.0025); moved 1 unit further back (z=1 → z=2)

**TrolleyBystanderSetup.cs — created then deleted:**
- Created to wire CCTVBlackout; deleted after one use (no longer needed)

**Committed:** `1e83257` — "Bystander: spline-based train movement with action divert — basic functions working"

**Bystander scene status: basic functions all working**
- Narration → train starts → decision window → A/B toggle → divert or straight → CCTV blackout → transition

**Next session starts here:**
- **Driver scene** — replicate Bystander working functions. Do NOT run setup scripts (destroys scene). Wire manually or surgically.
  - TrainController: assign `Rail` SplineContainer, draw 2 splines, set `trainSpeed`
  - ToggleDecision: run `Trolley > Driver – Wire Toggle Buttons` (safe, non-destructive)
  - StatusText: disable in scene manually
  - Test full flow

---

### Day 9 (2026-05-25) — Bystander toggle buttons + narration scripts + end-to-end test

**Done:**

**Narration scripts — all three scenarios finalised:**
- All ~21 seconds / ~47 words each. Saved to `NARRATION_SCRIPTS.md`.
- Bystander: control room CCTV monitoring, Track A (5 workers) vs Track B (1 person), button press diverts
- Driver: operating a tram with broken brakes, Track A vs side track, 8 seconds to decide
- Self-harm: same but divert = tram falls off cliff (self at risk)

**TrolleyToggleDecision.cs — new script:**
- A/B toggle for Bystander scene. A = inaction (default, highlighted green). B = action.
- `PressA()` / `PressB()` are public — wired to NetworkButton OnTrigger events in Inspector
- `IsAction` bool read by TrolleyController at timer expiry (OnWindowClose)
- Material color: `renderer.material.SetColor("_BaseColor")` with `"_Color"` fallback
- `FindChildRenderer(go, "Button")` targets Button child mesh, not Rim
- `rimA` / `rimB` GameObjects: green rim quads on Track 1 East and Track 2 East monitors, toggled with decision state

**CCTVBlackout.cs — new script:**
- `GameObject[] monitorOverlays` — black quads parented in front of each monitor quad
- `Blackout()` enables all overlays when decision is made (called by TrolleyController)

**TrolleyController.cs — updated:**
- Added `[SerializeField] TrolleyToggleDecision toggleDecision` (optional, Bystander only)
- Added `[SerializeField] CCTVBlackout cctvBlackout` (optional, Bystander only)
- `OnWindowClose()`: reads `toggleDecision.IsAction` at timer expiry → routes to ApplyAction or ApplyInaction
- Auto-creates TrolleyGameState + DataLogger in Start() if missing (enables standalone scene testing)
- All interactable/toggleDecision/cctvBlackout accesses null-safe

**TrolleyBystanderSetup.cs — new targeted menu items (non-destructive, preserve manual geometry):**
- `Trolley > Bystander – Wire Toggle Buttons` — wires OBJ_NetworkButton_A/B to ToggleDecision
- `Trolley > Bystander – Add Monitor Rims` — creates green rim quads on monitors [2] and [3]
- `Trolley > Bystander – Add CCTV Blackout` — creates black overlay quads, wires CCTVBlackout
- `Trolley > Bystander – Add Monitor Labels` — 4 world-space canvases: "Track 1 – West View", "Tracks 1 & 2 – Switch Point", "Track 1 – East View", "Track 2 – East View"
- `Trolley > Bystander – Fix Train Controller` — wires startPoint(z=−302)/endPoint(z=80)

**Bugs fixed:**
- Pink materials: setup script used URP shader on Built-in RP project → switched to `Unlit/Color`
- Button clicks not reaching console: VR2Gather NetworkButton uses its own OnTrigger UnityEvent, not XRSimpleInteractable.selectEntered → made PressA/PressB public, user wires them in Inspector
- Wrong renderer (Rim grabbed instead of Button): `FindChildRenderer` searches by child name
- Questionnaire showing scenario=unknown when testing standalone: TrolleyGameState singleton missing → fixed by auto-creating in TrolleyController.Start()

**Issues closed:**
- #3 Disable user movement — done Day 5, now closed
- #6 Design Bystander scene — core flow confirmed working end-to-end

**Next session (Tue 2026-05-27):**
1. Driver scene — wire buttons, assign narration audio, test TrainController
2. Self-harm scene — run `Trolley > Wire Selfharm Scene`, assign narration
3. Tutorial scene — build/wire TrolleyTutorial
4. Thu buffer: Quest build + on-device test

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
| 8 | Driver scene polish: room resize, toggle buttons, train-after-narration, track divert | ✓ Done |
| — | Jun 1–2: Self-harm scene, ResearcherSetup scene, Driver narration, Quest build | — |
| — | Jun 8–12: Conference (Athlone) — remote minor fixes only | — |
| — | Jun 15: Pilot test with Jack | — |
| — | Jun 22: Final fixes from pilot | — |
| — | Jun 29: Experiments begin (2 weeks) | — |

**Division of labour (from Jun 1):**
- Sueyoon: Unity scenes, scripts, Quest build
- Jack: TrolleyExperiment repo, VRTStatistics experiment settings, XRRayInteractor blocker

**Remaining critical items before Quest build:**
- Self-harm scene: run `Trolley > Wire Selfharm Scene`, assign narration audio
- ResearcherSetup scene: wire and test
- Driver: assign narration audio clip
- All scenes: on-device test, XRRayInteractor resolution (Jack)
