# Narration Scripts — VRTApp-Trolley
## All three scenario voiceovers

Target: ~25 seconds each at moderate pace (~2.2 words/sec = ~55 words max).
All scenarios use a button as the shared action point — referenced consistently below.

---

### Scenario A — Bystander (`narration_bystander.mp3`)

> You are monitoring trains in a control room. On the CCTV monitors, you see workers on the tracks. Five workers are on the main track and one worker is on the side track. A train is approaching and cannot be stopped. You have to hit either five workers or one worker.

---

### Scenario B — Driver (`narration_driver.mp3`)

> You are operating a train. You see workers on the tracks. Five workers are on the main track and one worker is on the side track. The brake is broken. You have to hit either five workers or one worker.

---

### Scenario C — Self-harm (`narration_selfharm.mp3`)

> You are operating a train. You see the tracks ahead. Five workers are on the main track and rocks are on the side track. The brake is broken. You have to either hit five workers or crash into the rocks.

---

> **Order: the DRIVER tutorial runs FIRST (and teaches the buttons), then the BYSTANDER tutorial.**
> Recordings carry their own pauses / SSML `<break>`s, so the drills' `betweenClipsPause` stays 0 — no
> timestamp tuning. "wait: press …" steps hold until the real press; each per-monitor clip blinks its
> rim for exactly its own length. The *scene flow* is driver-first.

### Tutorial 1 — Driver (first-person; teaches the buttons; no one at risk)

You ARE the driver, seated in the cab; the environment slides toward you. Flow:
intro → "watch the window" → **button practice** (press right / press left / confirm) → the rules →
**3 rock-blocker reps** (one track blocked by rocks: rocks on the main track = divert, rocks on the side
track = stay; order divert, stay, divert) → closing. The screen fades to black between reps. **Seven
clips**, `narration_tutorial_driver_*.mp3`, each mapping to a field on `TutorialDriverDrill`.

| File (`narration_tutorial_driver_*.mp3`) | Field | Notes |
|---|---|---|
| `…_intro` | `introClip` | preamble — you're the driver now |
| `…_window` | `windowClip` | where to look |
| `…_button_try1` | `buttonTry1Clip` | waits for the real RIGHT (B) press |
| `…_button_try2` | `buttonTry2Clip` | waits for the real LEFT (A) press |
| `…_button_try3` | `buttonTry3Clip` | confirm — selected button glows green |
| `…_sortingtrain` | `sortClip` | the rules; then 3 rock reps run (main, side, main) |
| `…_closing` | `closingClip` | after the 3 reps, before the bystander tutorial |

**intro.** > Let's start with a short tutorial. You are now operating a train. You can divert the train by using two buttons.

**window.** > Watch for obstacles ahead through the front window.

**button_try1.** Left (A) selected by default; waits for the real **right** (B) press.
> Let's try the buttons. By default, the left button keeps the train on the main track. Press the right button to steer it to the other side.

**button_try2.** Waits for the real **left** (A) press.
> Good. Now press the left button to steer back.

**button_try3.**
> The button you select is highlighted in green.

**sortingtrain.** Then **3 rock reps** run (main, side, main).
> Ahead, one side of the track is blocked with rocks. Drive the train onto the other side to avoid hitting them. Decide before the train reaches the diverting point. We'll do three practice rounds.

**closing.**
> This is the end of the first tutorial.

**Framing:** avoid the blocked track — the train turns onto the clear side when you divert. The rocks are
a practice scaffold; no obstacles block the track in the real study.

---

### Tutorial 2 — Bystander (control room; no one at risk) — TWELVE CLIPS

A guided **button round** (intro → monitors → per-monitor → press/back/confirm) then a **sorting drill**.
Each clip maps to a field on `TutorialBystanderDrill`; each per-monitor clip blinks its rim for exactly
its own length (sync automatic). Buttons are never blinked — from the button-practice step on they use
their real-scene feedback (colour on click, the matching monitor rim glows green).

| # | File (`narration_tutorial_bystander_*.mp3`) | Field | Blinks |
|---|---|---|---|
| 1 | `…_intro` | `introClip` | — (preamble) |
| 2 | `…_monitors` | `monitorsClip` | all four rims together |
| 3 | `…_monitor_approach` | `introApproachClip` | rimApproach (top-left) |
| 4 | `…_monitor_switch` | `introSwitchClip` | rimSwitch (top-right) |
| 5 | `…_monitor_main` | `introMainClip` | rimMain (bottom-left) |
| 6 | `…_monitor_side` | `introSideClip` | rimSide (bottom-right) |
| 7 | `…_button_main` | `pressClip` | — (waits for real B / divert) |
| 8 | `…_button_side` | `backClip` | — (waits for real A / main) |
| 9 | `…_button_confirm` | `confirmClip` | — |
| 10 | `…_sortingtrain` | `sortClip` | — (the rules; then 3 rounds run) |
| 10b | `…_approaching` | `approachingClip` | — (before EACH round, as the train appears) |
| 11 | `…_closing` | `closingClip` | — (ends the tutorials, before the real scenarios) |

**intro.** > Let's move on to the second tutorial. You are now sitting in a control room managing the train track. In this room, you can divert approaching trains using two buttons.

**monitors.** All four monitor rims blink together while this plays.
> In front of you, there are four CCTV monitors, each showing a different part of the track.

**monitor_approach.** > The top-left monitor shows the train approaching the diverting point.

**monitor_switch.** > The top-right monitor shows the diverting point.

**monitor_main.** > The bottom-left monitor shows the main track, where the train runs. The button on the left sends the train along the main track.

**monitor_side.** > The bottom-right monitor shows the side track. The button on the right diverts the train to the side track.

**button_main.** Left button (A) selected by default; waits for the real **right** (B) press.
> Let's try pressing the buttons. By default, the left button is selected so that the train follows the main track. Press the button on the right to divert the train.

**button_side.** Waits for the real **left** (A) press.
> Great. Now press the button on the left to send it back to the main track.

**button_confirm.**
> Perfect. As you may have noticed, the button you selected is highlighted in green, and the rim of its matching monitor glows green too.

**sortingtrain.** Then **3 rounds** run — BLUE, RED, BLUE — one train at a time, no timer; the top-right
counter tracks correct decisions out of 3.
> Now let's practice sorting the trains. If you see a red train, let it follow the main track. If you see a blue train, press the button on the right to divert it to the side track. Decide before the train reaches the diverting point.

**approaching.** Plays before EACH of the 3 rounds, as a fresh train appears at the approach point.
> The next train is approaching.

**closing.** Plays after the 3 sorts, before the real scenarios begin.
> This is the end of the tutorials. Next, the real scenarios will begin.

**Framing:** RED = do nothing (main track, inaction) · BLUE = press the right button (divert, action).
No one is at risk in either round.

---

## Recording Notes

- Tone: calm, neutral, factual. Not urgent — urgency is provided by the approaching train/vehicle.
- Pace: moderate (~120–130 wpm). Do not rush.
- Record one take per scenario. No music or SFX underneath — ambient scene audio handles atmosphere.
- File naming convention: `narration_bystander.mp3`, `narration_driver.mp3`, `narration_selfharm.mp3`
- Assign to `NarrationPlayer` AudioSource in each scene after running setup scripts.
