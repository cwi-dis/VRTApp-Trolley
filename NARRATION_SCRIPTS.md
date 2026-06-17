# Narration Scripts — VRTApp-Trolley
## All three scenario voiceovers

Target: ~25 seconds each at moderate pace (~2.2 words/sec = ~55 words max).
All scenarios use a button as the shared action point — referenced consistently below.

---

### Scenario A — Bystander

> You are monitoring the train in the control room. A train is approaching five workers on track A. They are wearing AirPods and cannot hear it coming. If you press the button, the train will divert to track B, where one person is standing. If you do nothing, the train continues toward the five workers.

**Word count:** ~47 | **Est. duration:** ~21 s

---

### Scenario B — Driver

> You are operating a tram, and the brakes have broken. Five workers are on the current track. They are wearing AirPods and cannot hear it coming. If you press the button, the tram will divert to the side track, where one person is standing. If you do nothing, the tram will continue toward the five workers.

**Word count:** ~47 | **Est. duration:** ~21 s

---

### Scenario C — Self-harm

> You are operating a tram, and the brakes have broken. Five workers are on the current track. They are wearing AirPods and cannot hear it coming. If you press the button, the tram will divert to the side and fall down the cliff. If you do nothing, the tram will continue toward the five workers.

**Word count:** ~46 | **Est. duration:** ~21 s

---

### Tutorial — Practice (no one at risk) — TWO ROUNDS, FOUR CLIPS

The tutorial is split into a guided **button round** and a **sorting drill**. Record **four separate
clips** so the flow can wait for each real button press. Files:
`narration_tutorial_intro.mp3`, `_press.mp3`, `_back.mp3`, `_sort.mp3`.

**Clip 1 — `intro` (control room + four monitors).** As it plays, each monitor's green rim blinks in
turn; tune `monitorHighlightTimes` on `TutorialTrainDrill` to match this recording.

> Let's start with a short tutorial for using the buttons. You are sitting in a control room with four CCTV monitors, each showing a different part of the track. The **top-left** monitor shows the train approaching the diverting point. The **top-right** monitor shows the diverting point itself. The **bottom-left** monitor shows the main track, where the train runs — this is controlled by **button A**, on the left. The **bottom-right** monitor shows the diverting track — to send the train there, press **button B**, on the right.

**Word count:** ~80 | **Est. duration:** ~36 s
_(Then a 3-second pause — `introPauseAfter` — before the button practice.)_

**Clip 2 — `press`.** Button B blinks; the flow waits for the real press.

> Let's try it. Press the button on the right to divert the train.

**Word count:** ~13 | **Est. duration:** ~6 s

**Clip 3 — `back`.** Button A blinks; the flow waits for the real press.

> Great. Now press the button on the left to send it back to the main track.

**Word count:** ~16 | **Est. duration:** ~7 s

**Clip 4 — `sort` (Round 2 intro).** Then 5 trains run — RED, BLUE, BLUE, RED, BLUE — ~10 s apart,
no timer; the top-right counter tracks correct decisions out of 5.

> Now let's practise sorting the trains. If a train is **red**, do nothing and let it follow the main track. If a train is **blue**, press the button to divert it to the side track. Decide before the train reaches the diverting point.

**Word count:** ~46 | **Est. duration:** ~21 s

**Framing:** RED = do nothing (main track, inaction) · BLUE = press the button (divert, action). Round 1
uses the existing A/B toggle + the four monitor rims; Round 2 is the colour drill. No one is at risk in
either round.

---

## Recording Notes

- Tone: calm, neutral, factual. Not urgent — urgency is provided by the approaching train/vehicle.
- Pace: moderate (~120–130 wpm). Do not rush.
- Record one take per scenario. No music or SFX underneath — ambient scene audio handles atmosphere.
- File naming convention: `narration_bystander.mp3`, `narration_driver.mp3`, `narration_selfharm.mp3`
- Assign to `NarrationPlayer` AudioSource in each scene after running setup scripts.
