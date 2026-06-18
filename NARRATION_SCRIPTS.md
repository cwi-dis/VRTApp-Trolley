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

### Tutorial — Practice (no one at risk) — TWO ROUNDS, TEN CLIPS

The tutorial is a guided **button round** (intro → monitors → per-monitor → press/back/confirm) then a
**sorting drill**. Record **ten separate clips** so the flow waits for each real press and each monitor's
rim blinks for exactly its own clip — sync is automatic, no timestamp tuning. Each clip maps to a field
on `TutorialTrainDrill`.

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
| 10 | `…_sortingtrain` | `sortClip` | — (then 5 trains run) |
| 11 | `…_closing` | `closingClip` | — (after 5 correct, before tutorial 2) |

**1 — `intro`.** > Let's start with a short tutorial. You are now sitting in a control room managing the train track. In this room, you can divert trains approaching by switching two buttons.

**2 — `monitors`.** All four monitor rims blink together while this plays.
> On the front, there are four CCTV monitors, each showing a different part of the track.

**3 — `monitor_approach`.** > The top-left monitor shows the train approaching the diverting point.

**4 — `monitor_switch`.** > The top-right monitor shows the diverting point.

**5 — `monitor_main`.** > The bottom-left monitor shows the main track, where the train runs. The button on the left sends the train along the main track.

**6 — `monitor_side`.** > The bottom-right monitor shows the side track. The button on the right diverts the train to the side track.

_(Then a 3-second pause — `introPauseAfter` — before the button practice.)_

**7 — `button_main`.** Button practice begins: the left button (A) is now selected (green, its rim lit)
to match "by default the left button is selected". Waits for the real **right** (B) press.
> Let's try pressing the buttons. By default, the left button is selected, so the train follows the main track. Press the button on the right to divert the train.

**8 — `button_side`.** Waits for the real **left** (A) press.
> Great. Now press the button on the left to send it back to the main track.

**9 — `button_confirm`.**
> Perfect. As you may have noticed, the button you selected is highlighted in green, and the rim of its matching monitor glows green too.

**10 — `sortingtrain` (Round 2 intro).** Then 5 trains run — RED, BLUE, BLUE, RED, BLUE — ~10 s apart,
no timer; the top-right counter tracks correct decisions out of 5.
> Now let's practise sorting the trains. If you see a red train, let it follow the main track. If you see a blue train, press the button on the right to divert it to the side track. Decide before the train reaches the diverting point.

**Framing:** RED = do nothing (main track, inaction) · BLUE = press the right button (divert, action).
No one is at risk in either round.

---

### Tutorial 2 — Driver (first-person, no one at risk)

You ARE the driver this time, seated in the cab; the environment slides toward you. Round 1 = intro +
button practice; Round 2 = a signal-light drill (a light ahead turns BLUE = divert / RED = stay, 5 reps).
Files: `narration_tutorial_driver_*.mp3`. Each clip maps to a field on `TutorialDriverDrill`.

| File | Field | Notes |
|---|---|---|
| `…_intro` | `introClip` | preamble |
| `…_buttons` | `buttonsClip` | — |
| `…_signal` | `signalClip` | signal light blinks while this plays |
| `…_button_main` | `pressClip` | waits for the real right (B) press |
| `…_button_side` | `backClip` | waits for the real left (A) press |
| `…_button_confirm` | `confirmClip` | — |
| `…_sortingtrain` | `sortClip` | then 5 signal reps run |
| `…_closing` | `closingClip` | after 5 correct, before the study begins |

**intro.** > Now it's your turn to drive. You're sitting in the cab, operating the tram yourself. Don't worry — this is just practice, and no one is at risk.

**buttons.** > In front of you are two buttons. The button on the left keeps the tram on the main track. The button on the right diverts it to the side track.

**signal.** > Watch the signal light ahead. When it turns blue, divert by pressing the right button. When it stays red, keep to the main track by doing nothing.

**button_main.** > Let's try it. Press the button on the right to divert the tram.

**button_side.** > Great. Now press the button on the left to return to the main track.

**button_confirm.** > Perfect. The button you press lights up green, just like the real controls.

**sortingtrain.** > Now let's practise. Remember: a blue signal means divert, a red signal means stay. Decide before you reach the switch.

**closing.** > That's the end of the tutorials. The real study is about to begin.

**Framing:** identical red/blue meaning to Tutorial 1, but you're the driver — the tram turns onto the
side track when you divert, instead of you watching it from a control room.

---

## Recording Notes

- Tone: calm, neutral, factual. Not urgent — urgency is provided by the approaching train/vehicle.
- Pace: moderate (~120–130 wpm). Do not rush.
- Record one take per scenario. No music or SFX underneath — ambient scene audio handles atmosphere.
- File naming convention: `narration_bystander.mp3`, `narration_driver.mp3`, `narration_selfharm.mp3`
- Assign to `NarrationPlayer` AudioSource in each scene after running setup scripts.
