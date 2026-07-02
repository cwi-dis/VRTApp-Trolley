# VRTApp-Trolley — Claude Working Notes

This file is for both Jack's and Suzy's Claude sessions. If you discover a workflow pattern, convention, or gotcha that isn't captured here, please add it.

## Team

- **Suzy (suziosaurus)** — primary developer and researcher. Owns all study logic: scenes, scripts, questionnaire, data logging, avatar system, narration, setup scripts.
- **Jack (jackjansen)** — VR2Gather platform maintainer. Owns infrastructure: `VRTPilotConfig`, player prefabs, config file handling, `VRTLoginManager` integration, and anything that touches the VR2Gather package itself.

When in doubt about who owns something, check `git log --follow` on the file.

## Scene ownership: Editor setup scripts

Suzy's workflow uses Editor scripts to build scenes procedurally. GameObjects marked with a `ManagedBySetupScript` component are **owned by a setup script** — do not edit them directly in the Inspector or scene, as they will be overwritten when the script is re-run.

**Rule:** when a UI element, layout, or wiring needs to change, find the setup script and change it there. Then tell the developer to re-run the menu item.

Setup scripts live in `Assets/Trolley/Editor/`:

| Menu item | Script |
|---|---|
| `Trolley > Wire Questionnaire Scene` | `TrolleyQuestionnaireSetup.cs` |
| `Trolley > Driver – Wire Movement` / `Driver – Wire Toggle Buttons` (targeted; full wire removed) | `TrolleyDriverSetup.cs` |
| `Trolley > Build Selfharm From Driver` (duplicates Driver → cliff/mountain on action track) | `TrolleySelfharmSetup.cs` |
| `Trolley > Build Tutorial From Bystander` (duplicates Bystander → no workers, practice) | `TrolleyTutorialBystanderSetup.cs` |
| `Trolley > Build Driver Tutorial From Driver` / `Driver Tutorial – Assign Narration & SFX Clips` (first-person signal drill; does NOT touch Build Settings) | `TrolleyTutorialDriverSetup.cs` |
| `Trolley > Bystander – …` (targeted menus) | `TrolleyBystanderSetup.cs` |
| `Trolley > Wire Researcher Setup Scene` | `TrolleyResearcherSetupSceneSetup.cs` |
| `Trolley > Wire Avatar Setup Scene` | `TrolleyAvatarSetupSceneSetup.cs` |
| `Trolley > Tutorial – Assign Narration & SFX Clips` (non-destructive; wires the 10 narration + 2 SFX clips) | `TrolleyTutorialBystanderSetup.cs` |
| `Trolley > Build Control Room Shell` (enclosing ceiling+walls around the console; operates on open scene, doesn't save) | `TrolleyControlRoomShell.cs` |
| `Trolley > Add Tutorial Skip Button (open scene)` (researcher-only grey skip button; run once per tutorial scene; preserves placement on re-run) | `TrolleySkipButtonSetup.cs` |
| `Trolley > Add Tutorial Start Button (open scene)` (participant-facing world-space "Start Tutorial" UI button — centred canvas, XR-raycastable; wires the drill's `gate` field; opens with an A/B warm-up then waits for Start; rebuilds cleanly on re-run; run once per tutorial scene, then reposition) | `TrolleyTutorialStartSetup.cs` |
| `Trolley > Copy Room Layout: Tutorial → Bystander` (copies world transforms of MonitorGroup/MonitorLabelGroup/ControlRoomShell/Button_TrackA/B/GazeTarget_Buttons; saves Bystander) | `TrolleyRoomLayoutCopy.cs` |
| `Trolley > Setup Humanoid Avatar Prefab` (step 1 of 2 — operates on selected wrapper GO in scene; adds RigBuilder + Two Bone IK for arms and legs + foot colliders + SyncSkeletonToVRRig + SizeAdjust + PlayerRepresentationWirer; see below for full workflow) | `TrolleyAvatarPrefabSetup.cs` |
| `Trolley > Save and Wire Avatar into Players` (step 2 of 2 — saves the wrapper GO as a prefab asset, adds it as `altRepOne` in both `P_Self_Player_Trolley` and `P_Player_Trolley`, wires SizeAdjust SourceTop/SourceBottom and localScale) | `TrolleyAvatarPrefabSetup.cs` |
| `Trolley > Wire GazeTargets` (non-destructive; adds PFB_GazeTarget to ActionTrackWorkers/InactionTrackWorkers/Rock in the open scene; skips already-present; does NOT save) | `TrolleyGazeTargetSetup.cs` |
| `Trolley > Create or Select Timing Config` (creates/selects the shared `TrolleyTimingConfig` asset in `Assets/Trolley/Resources`; one decision-window knob that scales train speed + worker-hide delay across all scenes) | `TrolleyTimingConfigSetup.cs` |
| `Trolley > Copy DriverCab: Driver → Selfharm + TutorialDriver` (places the `DriverCab` prefab at Driver's world transform in both scenes; re-runnable; NON-destructive — leaves the old DriverCabShell for you to remove by hand) | `TrolleyDriverCabCopy.cs` |

Self-harm and Tutorial are built by **duplicating** a known-good scene (Driver / Bystander) and applying targeted edits — they preserve all hand-tuned geometry. Re-running overwrites the target scene, so make manual tweaks only after the final run.

**Gotcha:** setup scripts wire fields by string name via `SerializedObject.FindProperty("fieldName")`. If a C# field is renamed, the string in the setup script must be updated manually — there is no compiler error if they drift. `[FormerlySerializedAs]` on the C# field handles existing scene YAML but does NOT fix the setup script.

## Avatar prefab workflow

Full workflow when creating or re-creating `P_Avatar_Trolley_Male` / `P_Avatar_Trolley_Female`:

1. Open the `tmp` scene and drag the avatar prefab into the Hierarchy.
2. Select the instance and run `Trolley > Setup Humanoid Avatar Prefab`. The script **auto-removes any prior setup** (SyncSkeletonToVRRig GO, VR Constraints GO, RigBuilder) before running, so no manual revert is needed.
3. Enter Play Mode briefly (Animation Rigging needs one play-mode visit to bake constraint bindings), then exit.
3.1. Apply the Play Mode overrides back to the prefab: with the instance selected, click **Overrides** in the Inspector → **Apply All to Prefab**.
3.5. **Optional IK sanity check** (useful after changes to the setup script): while the avatar is in the `tmp` scene, disable `SizeAdjust`, `PlayerRepresentationWirer`, and `SyncSkeletonToVRRig` on the wrapper GO (they require VR2Gather framework and will throw errors in isolation), then enter Play Mode and drag the **IK Target** GOs (Left/Right Arm IK Target, Left/Right Leg IK Target) in the Inspector. Legs and arms should track the targets without lotus position or wild rotation. Also useful: drag P_Mannequin into the scene alongside the avatar to compare — P_Mannequin uses a Generic rig so it always works.
   - **Gotcha:** after the sanity check, **re-enable all three components** (`SizeAdjust`, `PlayerRepresentationWirer`, `SyncSkeletonToVRRig`) before applying overrides to the prefab. If `SyncSkeletonToVRRig` is left disabled in the prefab the avatar will be completely unresponsive to VR tracking at runtime — confirmed 2026-06-28.
   - **Size/orientation check (edit mode, before Play):** with both the new avatar and P_Mannequin in the scene at (0,0,0), temporarily enable both in the Inspector and adjust the avatar wrapper's scale until they overlap in the Scene view. Apply that scale as a prefab override before continuing. (`P_Avatar_Trolley_Male` / Remy: scale **0.475**.)
4. **Wire into each player prefab** (repeat for `P_Self_Player_Trolley` and `P_Player_Trolley`):
   - Drag the player prefab into the `tmp` scene. Drag the avatar prefab as a child of it.
   - Select the avatar child → `Trolley > Wire as AltRepOne` (or `AltRepTwo` for a second avatar). The script wires `altRepOne`, `SizeAdjust` sources, and (for self-player only) `ViewAdjust.viewAdjusted → SizeAdjust.AdjustHeight`.
   - Apply overrides to the player prefab. Remove the player prefab from the scene and repeat for the other player prefab.
   - **Note:** if re-running setup on an existing prefab (not creating fresh), the player prefab wiring (altRepOne, SizeAdjust sources) survives the re-run — only re-run `Wire as AltRepOne` if the wires look broken.
5. **One manual step still required:**
   - **SizeAdjust > HMD Tracking Action** (on the avatar prefab) → assign `XRI Head/IsTracked` from the XRI default input actions asset. (`setHeightOnStart` is already false and `setHeightOnHMDTracking` true by the script; only the `InputActionReference` needs manual wiring.) This field is **preserved** across setup re-runs since SizeAdjust itself is not deleted.

**Gotcha — SizeAdjust DestinationBottom:** must be the model root GO (Mixamo FBX root sits at y=0 in T-pose), not `hipsBone`. Using `hipsBone` gives head-to-hips height (~0.9 m) instead of full height (~1.75 m), causing ~2× overscale. The setup script sets this correctly; just don't override it in the Inspector.

## C# field renames

When renaming a serialized field, add `[FormerlySerializedAs("oldName")]` (from `UnityEngine.Serialization`) to migrate existing scene YAML automatically. Also update the corresponding `FindProperty("...")` call in the setup script.

## Debug markers

- Jack uses `xxxjack` to flag his own temporary debug code.
- Claude should use `xxxclaude` for any temporary debug statements or comments, so they're easy to find and remove.

## Open technical questions

- **Recording file path** — `RecordUserVoice` passes the filename directly to `FileStream` with no path prefix. VRTConfig may remap this to the stats/log output directory (where VRTRun looks), but this needs to be verified in a Quest build.
- **WAV flush on scene unload** — resolved. `VoicePipelineSelf.OnDestroy` calls `reader.Stop()` → `AsyncVoiceReader.AsyncOnStop()` → `StopRecording()` → `FinalizeWavFile`. WAV is written cleanly on scene unload; no explicit stop needed.
- **Marker support in RecordUserVoice** — questionnaire reflection "Done" button has a `// TODO: emit AddMarker("reflection_done")` comment. This requires adding `AddMarker(string name)` to `RecordUserVoice` in the VR2Gather package (not this repo), which outputs a `stats:` line so VRTstatistics can locate the segment in the audio file.

## Avatar lotus-position bug — root cause and fix

**Root cause (confirmed 2026-06-19):** Remy uses a **Humanoid rig**. The Animator resets the hips to its default body position (near floor) every animation step. The RigBuilder leg IK then evaluates with hips at the wrong height → lotus. `SyncSkeletonToVRRig` corrects the hips in LateUpdate, but that runs *after* the IK has already computed the wrong pose. P_Mannequin uses a **Generic rig**, so the Animator never fights the hips — which is why P_Mannequin works without this fix.

**Fix (implemented 2026-06-21 in `TrolleyAvatarPrefabSetup.cs`):** A "Body Constraint" GO is added as the *first* child of VR Constraints. It carries a `MultiPositionConstraint` whose constrained object is `hipsBone` and whose source is the Body Constraint GO's **own Transform**. `SyncSkeletonToVRRig.neck.rigTarget` points to this GO (not directly to `hipsBone`). Each frame: LateUpdate moves the Body Constraint GO to the correct hip position → next frame's animation step runs the Body Constraint (restoring hipsBone to last frame's correct position) → leg IK evaluates with the right hip height → no lotus. 1-frame lag is imperceptible.

`animator.applyRootMotion = false` is also set by the script (Humanoid rigs default to true).

## Avatar arm-tracking bug — root cause and fix

**Root cause (confirmed 2026-06-26):** Unity's animation stream only syncs scene-transform changes for objects **directly referenced** by a constraint (e.g. `TwoBoneIKConstraint.data.target`). `Left Arm IK Offset` was the parent of `Left Arm IK Target`, and `SyncSkeletonToVRRig.leftHand.rigTarget` pointed to the Offset. When `Map()` moved the Offset in the scene, the stream never updated because Offset wasn't a constraint target — only Target was. `Target.localPos = 0` was unchanged, so the stream always saw the initial T-pose hand position.

**Fix (2026-06-26):** `CreateTwoBoneIK` now always returns `targetGO.transform` (the actual TwoBoneIK target), not the Offset GO. `SyncSkeletonToVRRig.rigTarget` therefore points to the same transform the constraint reads from the stream. The Offset GO still exists as a parent (its `localRotation` can be used for future hand-angle correction), but `Map()` drives the Target directly.

**Key rule:** `SyncSkeletonToVRRig.leftHand.rigTarget` and `TwoBoneIKConstraint.data.target` must point to the **same transform**. Never interpose an unrelated parent between them.
