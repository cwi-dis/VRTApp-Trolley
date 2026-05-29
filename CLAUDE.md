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
| `Trolley > Wire Bystander Scene` | `TrolleyBystanderSetup.cs` |
| `Trolley > Wire Driver Scene` | `TrolleyDriverSetup.cs` |
| `Trolley > Wire Selfharm Scene` | `TrolleySelfharmSetup.cs` |
| `Trolley > Wire Researcher Setup Scene` | `TrolleyResearcherSetupSceneSetup.cs` |
| `Trolley > Wire Avatar Setup Scene` | `TrolleyAvatarSetupSceneSetup.cs` |

**Gotcha:** setup scripts wire fields by string name via `SerializedObject.FindProperty("fieldName")`. If a C# field is renamed, the string in the setup script must be updated manually — there is no compiler error if they drift. `[FormerlySerializedAs]` on the C# field handles existing scene YAML but does NOT fix the setup script.

## C# field renames

When renaming a serialized field, add `[FormerlySerializedAs("oldName")]` (from `UnityEngine.Serialization`) to migrate existing scene YAML automatically. Also update the corresponding `FindProperty("...")` call in the setup script.

## Debug markers

- Jack uses `xxxjack` to flag his own temporary debug code.
- Claude should use `xxxclaude` for any temporary debug statements or comments, so they're easy to find and remove.

## Open technical questions

- **Recording file path** — `RecordUserVoice` passes the filename directly to `FileStream` with no path prefix. VRTConfig may remap this to the stats/log output directory (where VRTRun looks), but this needs to be verified in a Quest build.
- **WAV flush on scene unload** — resolved. `VoicePipelineSelf.OnDestroy` calls `reader.Stop()` → `AsyncVoiceReader.AsyncOnStop()` → `StopRecording()` → `FinalizeWavFile`. WAV is written cleanly on scene unload; no explicit stop needed.
- **Marker support in RecordUserVoice** — questionnaire reflection "Done" button has a `// TODO: emit AddMarker("reflection_done")` comment. This requires adding `AddMarker(string name)` to `RecordUserVoice` in the VR2Gather package (not this repo), which outputs a `stats:` line so VRTstatistics can locate the segment in the audio file.
