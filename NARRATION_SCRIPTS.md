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

> ⚠️ **ORDER REVERSED (issue #59): the DRIVER tutorial now runs FIRST, then the bystander tutorial.**
> The "first/second tutorial" wording below — and the fact that buttons are only taught in the bystander
> tutorial (now second) while the driver tutorial assumes they're already known — both still read as if
> bystander-first. These need a content pass + re-recording. The *scene flow* is already driver-first.

### Tutorial — Practice (no one at risk) — TWO ROUNDS, TEN CLIPS

The tutorial is a guided **button round** (intro → monitors → per-monitor → press/back/confirm) then a
**sorting drill**. Record **ten separate clips** so the flow waits for each real press and each monitor's
rim blinks for exactly its own clip — sync is automatic, no timestamp tuning. Each clip maps to a field
on `TutorialBystanderDrill`.

**Blink behaviour:** during the four per-monitor clips the named monitor's green **rim** blinks. The
**buttons are never blinked** — from the button-practice step on, they use their real-scene feedback
(colour changes on click, the selected monitor's rim glows green).

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
| 11 | `…_closing` | `closingClip` | — (after 3 correct, before the next tutorial) |

**1 — `intro`.** > Let's start with a short tutorial. You are now sitting in a control room managing the train track. In this room, you can divert approaching trains using two buttons.

**2 — `monitors`.** All four monitor rims blink together while this plays.
> In front of you, there are four CCTV monitors, each showing a different part of the track.

**3 — `monitor_approach`.** > The top-left monitor shows the train approaching the diverting point.

**4 — `monitor_switch`.** > The top-right monitor shows the diverting point.

**5 — `monitor_main`.** > The bottom-left monitor shows the main track, where the train runs. The button on the left sends the train along the main track.

**6 — `monitor_side`.** > The bottom-right monitor shows the side track. The button on the right diverts the train to the side track.

_(Then a 3-second pause — `introPauseAfter` — before the button practice.)_

**7 — `button_main`.** Button practice begins: the left button (A) is now selected (green, its rim lit)
to match "by default the left button is selected". Waits for the real **right** (B) press.
> Let's try pressing the buttons. As default, the left button is selected so that the train follows the main track. Press the button on the right to divert the train.

**8 — `button_side`.** Waits for the real **left** (A) press.
> Great. Now press the button on the left to send it back to the main track.

**9 — `button_confirm`.**
> Perfect. As you may have noticed, the button you selected is highlighted in green, and the rim of its matching monitor glows green too.

**10 — `sortingtrain` (Round 2 intro).** Then **3 rounds** run — BLUE, RED, BLUE — one train at a time,
no timer; the top-right counter tracks correct decisions out of 3. The train moves at a constant world
speed (slower, half-length run) so the divert reads as a clean turn at the switch.
> Now let's practise sorting the trains. If you see a red train, let it follow the main track. If you see a blue train, press the button on the right to divert it to the side track. Decide before the train reaches the diverting point.

**10b — `approaching`.** Plays before EACH of the 3 rounds, as a fresh train appears at the approach point.
Keep it short (~2 s). NEW — needs recording.
> The next train is approaching.

**11 — `closing`.** Plays after the 3 correct sorts, before the next tutorial loads.
> This is the end of the first tutorial. Now let's move to the second tutorial, where you will be a driver of the train.

**Framing:** RED = do nothing (main track, inaction) · BLUE = press the right button (divert, action).
No one is at risk in either round.

---

### Tutorial 2 — Driver (first-person, no one at risk)

You ARE the driver this time, seated in the cab; the environment slides toward you. Buttons are **not**
re-taught — the participant already practised them in Tutorial 1. Flow: intro → "watch the window" → the
rules → **3 rock-blocker reps** (one track blocked by a rocky barrier: rocks on the main track = divert,
rocks on the side track = stay; order divert, stay, divert) → closing. The screen fades to black between
reps to hide the world resetting. **Four clips only** — `narration_tutorial_driver_*.mp3`, each mapping to
a field on `TutorialDriverDrill`. (Recordings carry their own ~2s trailing pause, so the drill's
`betweenClipsPause` is 0.)

| File | Field | Notes |
|---|---|---|
| `…_intro` | `introClip` | preamble — you're the driver now |
| `…_window` | `windowClip` | where to look |
| `…_sortingtrain` | `sortClip` | the rules; then 3 rock reps run (main, side, main) |
| `…_closing` | `closingClip` | after the 3 reps, before the study begins |

**intro.** > Let's move on to the second tutorial. You are now operating a train. You can divert the train by using two buttons.

**window.** > Watch for obstacles ahead through the front window.

**sortingtrain.** > Ahead, one side of the track is blocked with rocks. Drive the train onto the other side to avoid hitting them. Decide before the train reaches the diverting point. We'll do three practice rounds.

**closing.** > This is the end of the second tutorial.

**Framing:** same avoid-the-blocked-track logic as Tutorial 1's red/blue trains, but you're the driver —
the train turns onto the side track when you divert, instead of you watching it from a control room. The
rocks are a practice scaffold; no obstacles block the track in the real study.

---

## Recording Notes

- Tone: calm, neutral, factual. Not urgent — urgency is provided by the approaching train/vehicle.
- Pace: moderate (~120–130 wpm). Do not rush.
- Record one take per scenario. No music or SFX underneath — ambient scene audio handles atmosphere.
- File naming convention: `narration_bystander.mp3`, `narration_driver.mp3`, `narration_selfharm.mp3`
- Assign to `NarrationPlayer` AudioSource in each scene after running setup scripts.
