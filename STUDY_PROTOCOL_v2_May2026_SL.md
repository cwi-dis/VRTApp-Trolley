# STUDY PROTOCOL — VERSION 2
## Moral Decision-Making Under Social Influence in Social VR

**Sueyoon Lee (Susie)**
CWI Amsterdam & TU Delft
PhD: Designing Virtual Reality Environments for Sociality
Supervisors: Irene Viola, Pablo Cesar, Alessandro Bozzon, Valeria Pannunzio

Version: 2.0 — May 2026
Target venue: CHI 2027
Ethics board: TU Delft HREC

---

## 1. Study Overview

| Parameter | Detail |
|---|---|
| Internal title | Understanding Dyadic Decision-Making Dynamics in Social VR |
| Participant-facing title | Decision-Making in Emergency Situations |
| Study structure | 2×3 mixed design |
| Between-subjects factor | Condition: Solo vs. Paired |
| Within-subjects factor | Scenario type: Bystander / Driver / Self-harm (3 levels, counterbalanced) |
| Platform | Social VR with stylised customisable avatars, CWI Amsterdam + TU Delft |
| Target N | 40 (20 solo; 20 paired — 10 pairs: 5 strangers, 5 close [friends + romantic partners merged]) |
| Session duration | ~50 minutes |
| Data storage | CWI only |
| Target venue | CHI 2027 |

---

## 2. Theoretical Framing

### 2.1 Human Moral Decision-Making in the Age of AI

AI systems now make decisions on behalf of humans in autonomous vehicles, medical triage, content moderation, and military systems. This transfer of agency does not eliminate human moral responsibility — it concentrates it. The decisions that remain with humans are the edge cases: where the AI defers, where the stakes are highest, where no algorithmic answer is adequate. Understanding how humans make these decisions under time pressure is a practical and ethical necessity, not a philosophical exercise.

Most of what we know about human moral decision-making comes from text-based surveys — written trolley problems, hypothetical choices, unlimited deliberation time. This literature generates philosophical insight but systematically misrepresents how people behave under pressure. VR changes the picture: Francis et al. (2016) found that 63–70% of participants made a utilitarian choice in a VR Fat Man dilemma compared to 10% in text; Niforatos et al. (2020) replicated the pattern in a VR bystander scenario. These are not small differences — they establish that behavioural moral research requires embodied, time-pressured methods.

A further gap remains: **all prior VR moral dilemma studies examined individuals in isolation.** Yet most real contexts where humans face fast, high-stakes moral decisions are not solitary. Two people share a vehicle. Two operators share a control panel. Two colleagues must decide together under pressure — and one of them will act, or neither will. How the presence of a second person shapes that decision is unknown. This study addresses it directly.

### 2.2 Diffusion of Moral Agency

In a dyadic context, a second active agent distributes perceived moral responsibility, reducing the psychological cost of acting (Darley & Latané, 1968). Drawing on the CNI model (Gawronski & Ng, 2024), the I-parameter (general preference for inaction) is expected to decrease in the paired condition because moral agency is shared rather than individually borne. This effect operates most directly in the Bystander and Driver scenarios, where the utilitarian act causes harm to others.

### 2.3 Social Baseline Theory and the Self-Harm Condition

The Self-harm scenario introduces a theoretically distinct dynamic. Here, the utilitarian choice requires participants to sacrifice themselves rather than harm another. Two competing mechanisms operate simultaneously:

**Diffusion of self-risk (toward inaction):** A partner may function as an implicit bystander — each participant waits for the other to act, reducing self-sacrifice rates in stranger pairs relative to solo.

**Social Baseline Theory (toward action):** Coan and Beckes (2015) propose that the brain's threat-response system is calibrated assuming social access. When a trusted partner is present, perceived threat cost decreases. For close pairs (friends, romantic partners), the partner's presence as a co-agent — someone who could also act — may reduce the experienced cost of self-sacrifice, making it psychologically more accessible.

The predicted result is a **crossover by relationship closeness**: stranger pairs show reduced or equivalent self-sacrifice compared to solo (diffusion dominates); close pairs (friends and romantic partners) show maintained or increased self-sacrifice (social baseline dominates). The two-group design — Strangers vs. Close — allows this contrast to be described empirically for the first time. This prediction is novel and untested in the VR moral dilemma literature.

A secondary phenomenon expected within the paired condition is **moral coordination emergence**: pairs enter scenario 1 as individuals and develop an implicit coordination style by scenario 3. Close pairs are expected to establish coordination faster; stranger pairs may remain more independent throughout. Scenario position (1st / 2nd / 3rd in the counterbalanced sequence) is included as an analysis covariate to capture this arc.

### 2.4 Methodological Note: Time Window

The 3–5 second critical decision window serves both ecological validity and methodological function. Dual process theory (Evans & Stanovich, 2013) predicts that time pressure below approximately five seconds suppresses deliberative reasoning and exposes intuitive moral responses. Miller's (2019) preliminary study validated this empirically: 8 seconds produced fixed strategies; 3–4 seconds produced naturalistic responses. The window is timed from critical proximity — not scene start — giving participants full situational context before the pressure begins.

### 2.5 Research Contributions

This study makes four contributions:

1. **Empirical:** The first controlled comparison of solo vs. dyadic moral decision-making in immersive social VR — filling a gap left by every prior VR moral dilemma study.
2. **Theoretical:** The first application of Social Baseline Theory (Coan & Beckes, 2015) to a moral dilemma context, generating a directional, counterintuitive prediction: close-pair presence *increases* self-sacrifice rates while stranger-pair presence *decreases* them.
3. **Methodological:** A mixed-methods design that combines behavioral decision data, per-scenario self-report, and in-session audio to capture the social dynamics of shared moral agency — not just its outcome.
4. **Design-relevant:** Empirical grounding for how VR-based moral training, emergency simulation, or human-AI teaming scenarios should account for the dyadic social context in which many real decisions are made.

### 2.6 Core Research Questions

**RQ1:** Does the presence of a second active participant in immersive social VR alter moral decision-making under time pressure compared to a solo baseline?

**RQ2:** Does this social influence effect differ across three dilemma types — and does the direction reverse when the utilitarian choice requires self-sacrifice rather than harm to others?

---

## 3. Cover Story

Participants are told:

> *"This study examines how people make decisions under time pressure. As AI systems increasingly make decisions on our behalf, understanding how humans decide quickly and intuitively has become increasingly important — and that is what this study investigates."*

No mention of moral dilemmas, ethics, trolley problems, or philosophy appears before the debrief. This framing (a) prevents participants from pre-preparing a reasoned moral stance, (b) suppresses social desirability bias throughout the session, and (c) is genuinely accurate — the AI motivation provides a compelling, topically relevant rationale that participants find credible. The true purpose is revealed in full during the debrief.

---

## 4. Participants

### 4.1 Sample Design

**N = 40 total:**
- Solo condition: 20 participants (each experiences all three scenarios alone)
- Paired condition: 20 participants — 10 pairs, recruited across two relationship groups:

| Relationship type | Pairs | Participants | Operational definition |
|---|---|---|---|
| Strangers | 5 | 10 | No prior acquaintance |
| Close | 5 | 10 | Current romantic partners, OR know each other for at least 6 months and socialise voluntarily outside of work or study |
| **Total paired** | **10** | **20** | |

The Close group merges friends and romantic partners — the theoretically relevant distinction for Social Baseline Theory is trusted vs. non-trusted, not the specific form of close relationship. Relationship type is a **qualitative context variable only** — not a statistical independent variable. The two groups provide a Strangers vs. Close contrast described and illustrated qualitatively, and treated as hypothesis-generating for future work.

### 4.2 Recruitment

Data collection runs across two locations over approximately 6 days:

| Location | Days | Sessions/day | Content |
|---|---|---|---|
| CWI Amsterdam | 4 | 5 | Solo sessions (recruited from CWI and TU Delft researcher network) |
| TU Delft Design building | 2 | 5 | Paired sessions (strangers recruited on-site; close pairs pre-scheduled) |

Solo sessions run entirely at CWI to reduce logistics complexity. Stranger pairs are recruited on the day from the TU Delft Design building. Close pairs (friends and romantic partners) are pre-scheduled before data collection begins — these are the hardest to coordinate and should be confirmed first.

Paired participants must attend simultaneously at the same lab location. Recruitment materials note: *"Pairs may be strangers, colleagues, friends, or romantic partners — all relationship types are welcome."*

A contingency day is reserved for attrition replacements. With 15% expected attrition (~7 participants), the total buffer needed is approximately 5–6 additional sessions.

### 4.3 Exclusion Criteria

- Significant VR motion sickness history
- Under 18 years of age

### 4.4 Sample Size Justification

For the primary between-subjects comparison (H1: solo vs. paired decision rates per scenario type), N=20 per group provides 80% power to detect a large effect (Cohen's h ≥ 0.64, alpha = .05, two-tailed) — h = 0.64 requires n ≈ 20 per group by Cohen's formula. For a medium effect (h = 0.5), power is approximately 62%. Equal group sizes (20/20) are statistically optimal for this comparison.

The large-effect justification is empirically grounded: prior VR moral dilemma studies report consistently large effect sizes — Francis et al. (2016) found a 53–60 percentage point difference in utilitarian choice between text and VR conditions; Niforatos et al. (2020) showed the same direction. Social influence effects on decision-making are expected to be of comparable magnitude. N=40 is consistent with the precedent of prior accepted VR studies in this paradigm (Miller, 2019) and reflects the practical recruitment constraint of 6 data collection days.

The within-subjects structure (3 scenarios per participant) increases precision for within-person contrasts — H2a/H2b and decision latency — without inflating the between-subjects comparison. Mixed-effects logistic regression is the primary analysis approach, correctly accounting for the repeated-measures structure.

Plan for up to 15% attrition (~6 participants); participants who cannot complete all three scenarios are replaced.

---

## 5. Scenario Design

### 5.1 Three Dilemma Types

Three variants of the trolley problem are used, differing in the nature of the required action and who bears the moral cost.

| | Bystander | Driver | Self-harm |
|---|---|---|---|
| Type | Outcome-based | Action-based (indirect) | Action-based (self-sacrificial) |
| Role | Random bystander | Tram operator | Vehicle driver |
| Mechanism | Press button (on lever housing) | Press button (on cab control panel) | Press button (on dashboard) |
| Who bears cost | One stranger | One stranger (via causal implication) | The participant |
| Involvement | Impersonal | Semi-personal (causally implicated) | Personal (self-interest at stake) |
| Prior reference | Niforatos et al., 2020 | Foot, 1967 | Miller, 2019 |

**Interaction design — standardised button across all scenarios:** All three scenarios use a single button press. The button is contextually placed in each scene (lever housing for Bystander/Driver, dashboard for Self-harm) to read as ecologically plausible, but the interaction is functionally identical. This standardisation enables clean cross-scenario comparison of decision rates and latencies without confounding by interaction difficulty or modality. In the paired condition, "first button press wins" is unambiguous.

**On the gradient:** The three scenarios represent escalating personal stake — from no personal cost (Bystander) to causal implication without personal risk (Driver) to required self-sacrifice (Self-harm). This differs from prior Fat Man designs, which varied directness of physical contact. The Self-harm scenario is grounded in Miller's (2019) driving simulator paradigm and is designed to test the Social Baseline Theory prediction (Section 2.3).

### 5.2 Scenario Descriptions

**Scenario A — Bystander (Outcome-based)**

The participant stands on a railway platform as a random bystander. A runaway train approaches. Five workers on the main track are wearing noise-cancelling headphones and cannot hear it. One person stands on the side track. A switch control is within reach.

Voiceover (~25 seconds): *"A train with failed brakes is approaching. Five workers on the main track cannot hear it coming. There is a switch control in front of you — if you press it, the train diverts to the side track where one person is standing. If you do nothing, the train continues toward the five workers."*

---

**Scenario B — Driver (Action-based, indirect)**

The participant operates a tram whose brakes have failed. Five workers are on the main track; one person is on the side track. The participant is causally implicated regardless of choice.

Voiceover (~25 seconds): *"You are operating this tram. The brakes have failed. Ahead, five workers are on the main track — there is a divert control on your panel that can redirect the tram to a side track where one worker is standing. If you do nothing, the tram continues toward the five workers."*

---

**Scenario C — Self-harm (Action-based, self-sacrificial)**

The participant is driving a vehicle that has lost its brakes on a mountain road. Five workers are ahead. A concrete barrier is to the side. Steering into the barrier will injure the participant but spare the workers. Inaction kills the five.

Voiceover (~25 seconds): *"The brakes have failed. Five workers are on the road ahead. There is a concrete barrier to your left — if you steer into it, you will be injured but the workers will be safe. If you do nothing, the vehicle continues toward the five workers."*

Adapted directly from Miller's (2019) driving simulator paradigm. Miller found that participants showed less physical resistance to computer-initiated self-harm than to computer-initiated harm of others — suggesting this condition may produce different baseline action rates than expected.

---

**Paired condition — Self-harm design note:**

Both participants are passengers in the same vehicle. Either participant can press the steering control — the first action registers as the pair's decision (same "first press wins" mechanic as Bystander and Driver). This symmetric design preserves full shared agency: both participants are simultaneously potential actors, which is the correct condition for Social Baseline Theory's load-sharing prediction. The question is whether a trusted partner's co-presence as a fellow potential actor reduces the perceived cost of self-sacrifice relative to deciding alone.

### 5.3 Victim Identity

Adult male victims only, fixed across all scenarios and all participants. Niforatos et al. (2020) established no significant difference between adult victim categories. Child victims are excluded entirely.

### 5.4 Scenario Order

Counterbalanced within each condition using a Latin square. Six possible orderings assigned sequentially to participants.

---

## 6. Avatar Design

### 6.1 Avatar System

The study uses stylised avatars with participant-driven customisation. Real-time point cloud (cwipc) is not used; the study runs at TU Delft (Design building) for recruitment speed. At the start of Phase 1, participants complete a brief avatar customisation UI (under 2 minutes):

| Option | Choices | Rationale |
|---|---|---|
| Body type / silhouette | Masculine / Feminine | Primary gender signal; affects partner recognisability for close dyads |
| Skin tone | 6 swatches | Self-representation and naturalness |
| Hair colour | 6 swatches | Visual distinctiveness for partner recognition |
| Height | Short / Medium / Tall | Reduces visual confusion between participants |

**Design principle:** Customisation serves *partner recognisability* for close dyads — a romantic partner must be able to identify their partner's avatar within the 3–5 second critical window. This is achieved without point cloud complexity.

### 6.2 Avatar Configuration Logging

Log each participant's avatar selections (body type, skin tone, hair colour, height). If both participants in a pair select visually similar configurations (same body type + same hair + similar skin tone), flag the session for qualitative analysis — reduced visual distinctiveness may affect social presence.

### 6.3 Proteus Effect Consideration

Avatar appearance can influence wearer behaviour (Yee & Bailenson, 2007). Self-selected appearance reduces Proteus Effect risk compared to researcher-assigned avatars. Include avatar body type selection as a covariate in H4 (personality + voice → who acts), since masculine-body-type selection may independently predict dominant decision behaviour.

---

## 7. Session Structure

| Phase | Content | Duration |
|---|---|---|
| Phase 1 | Arrival, consent, pre-questionnaire, avatar customisation, headset fitting | 13 min |
| Phase 2 | VR tutorial | 4 min |
| Phase 3 | 3 scenarios (~4.5 min each) + ITC-SOPI between scenarios 2–3 | 16 min |
| Phase 4 | Headset removal, post-items, interview | 13 min |
| Phase 5 | Full debrief | 4 min |
| **Total** | | **~50 min** |

---

### Phase 1 — Arrival, Consent, Pre-Measures (13 minutes)

**Step 1: Welcome and Cover Story (2 min)**

The experimenter delivers the cover story framing. No moral content is mentioned. For paired participants, both are present in the same room from arrival — they are introduced and can speak briefly before headset fitting. This brief pre-VR contact establishes baseline social presence, particularly for stranger pairs.

**Step 2: Consent (2 min)**

The participant reads and signs the informed consent form. The form explains that the study involves time-pressured decisions in virtual environments, that audio will be recorded, and that the full purpose will be explained after the study.

**Step 3: Pre-Experiment Questionnaire (5 min)**

| Measure | Items | Scale | Note |
|---|---|---|---|
| Big Five Inventory Short Form (BFI-10) | 10 | 5-pt Likert | Conscientiousness and Agreeableness are primary predictors for H4 |
| VR familiarity (custom 3-item) | 3 | 5-pt Likert | Frequency, comfort level, motion sickness history |
| Demographics | ~5 | Free / categorical | Age, gender, relationship to partner (paired only), native language, relationship duration in months (paired only — friends and romantic partners; not applicable to strangers) |
| IOS Scale (Aron et al., 1992) | 1 | Visual (7 overlapping circles) | Paired only. Captures actual relationship closeness, not just category label. Critical for H2 Social Baseline analysis. Takes 10 seconds. |

**Step 4: Headset Fitting (4 min)**

The experimenter assists with headset fitting. For the paired condition, both participants load into a shared neutral lobby and confirm mutual visibility and spatial audio. A brief connection test is run — participants wave at each other to confirm avatar visibility. Customised avatars from Step 3 are loaded for both participants.

---

### Phase 2 — VR Tutorial (4 minutes)

Participants enter a neutral training environment with no moral content. They practise:
- Pressing the action button (identical mechanic used across all three scenarios)
- Head movement, spatial navigation, general VR orientation

For the paired condition, both participants are in the training space together. Free interaction is explicitly encouraged: *"Feel free to explore the space and interact with each other."* This establishes social presence before any scenario content begins.

---

### Phase 3 — Three Scenarios (16 minutes)

Each scenario follows an identical five-sub-phase structure. Scenario order is counterbalanced across participants.

---

**Sub-phase A: Stage Setting and Voiceover (~60 seconds)**

The scene loads. Ambient sounds play: railway noise, distant train, environmental context. The situation is visible before the voiceover begins — participants orient to the scene. Voiceover plays (~25 seconds, maximum 3 sentences). After the voiceover, the hazard appears in the far distance, approaching naturally. Sound and visual scale increase continuously. **There is no visible countdown timer.** The escalating approach creates urgency without the deliberative cue a visible timer would introduce.

**Rationale:** Abrupt starts reduce ecological validity (Miller, 2019). Stage setting ensures participants are fully situated before the critical window opens.

---

**Sub-phase B: Critical Decision Window (3–5 seconds)**

The hazard reaches critical proximity. The action point becomes urgent. This is the only window in which intervention is physically possible. Miller's (2019) preliminary study confirmed that 3–4 seconds suppresses deliberative reasoning and social desirability responding; 8 seconds was too long.

*Solo condition:* Participant acts or does not act independently.

*Paired condition:* Both participants are in the scene simultaneously. There is **one shared action point**. Either participant can act; the first action registers as the pair's decision. If both participants attempt the control within a 500ms window, both attempts are logged as competition data — the first timestamp is the registered decision. Inaction is logged if neither acts.

**Unity technical requirement:** When Participant A presses the button, Participant B must see A's arm movement and the button visually responding in real time — shared, synchronised object state across both clients. Every interaction attempt must be logged with timestamp, participant ID, and action type.

**Competition definition:** Both participants attempt the shared control within 500ms of each other. Log the sequence, not just the winner.

---

**Sub-phase C: Immediate End**

The scenario ends immediately after the decision window closes. The scene cuts to black. No consequence animation, no sound of impact, no aftermath sequence.

**Rationale:** Showing consequences contaminates subsequent scenarios emotionally and adds no analytic value (Miller, 2019). The decision is the unit of analysis.

---

**Sub-phase D: Post-Scenario Questionnaire (Private, ~90 seconds)**

For the paired condition, participants are teleported to **separate private virtual spaces** before the questionnaire appears — audio and visually isolated. This ensures independent self-report data.

| # | Item | Construct | Condition |
|---|---|---|---|
| 1 | I felt in control of the decision I made. | Perceived agency | All |
| 2 | I felt personally responsible for the outcome. | Perceived responsibility | All |
| 3 | I am satisfied with the decision I made. | Decision satisfaction | All |
| 4 | I carefully considered the consequences before deciding. | Consequence consideration | All |
| 5 | The time pressure significantly affected my decision. | Time pressure sensitivity | All |
| 6 | I considered not acting at all. | Omission tendency | All |
| 7 | This situation felt real to me. | Ecological validity check | All |
| 8 | My partner influenced my decision. | Perceived partner influence | Paired only |
| 9 | I was aware of my partner's presence during the decision. | Per-scenario social presence proxy | Paired only |
| 10 | I felt that my virtual self was in danger. | Threat perception | Self-harm scenario only |

All items: 5-point Likert (1 = Strongly Disagree, 5 = Strongly Agree).

**ITC-SOPI co-presence check** (6 items, 5-point Likert) is administered to paired participants between scenarios 2 and 3 only — not after every scenario, to limit questionnaire fatigue.

---

**Sub-phase E: Verbal Reflection (60 seconds)**

After the questionnaire, participants return to the shared neutral space (or remain alone for solo). A text prompt appears:

> *"In a few sentences — what was going through your mind? What did you decide and why?"*

Paired condition additional prompt:
> *"Did the other person affect your decision? If so, how?"*

Audio recorded. No experimenter prompting. A 60-second soft timer is displayed; participants press a button when finished or wait for the timer. A 15-second rest screen appears before the next scenario loads.

---

### Phase 4 — Post-Experiment Measures (13 minutes)

**Headset Removal and Welfare Check (1 min)**

Headsets are removed. The experimenter checks in: *"How are you feeling? Take a moment if you need to."* This check is mandatory. If a participant shows significant distress, the interview is shortened and psychological support is offered immediately.

**Post-Experiment Single Items (2 min)**

| Item | Scale | Condition |
|---|---|---|
| This experience changed how I feel about my partner. | 5-pt Likert + optional free-text | Paired only |
| What aspects of the VR environment made you most aware of your partner's presence? | Open-ended | Paired only |
| At any point during the study, did you suspect its true purpose? If yes, when? | Yes/No + when | All |

The first item is directly relevant to the thesis: does a shared moral dilemma experience in VR affect social bonds? The third item is a cover story effectiveness check.

**Semi-Structured Interview (10 min)**

Conducted by the experimenter. Audio recorded.

Themes are ordered by priority. **Priority 1** (always ask, ~7 min): cover all regardless of time. **Priority 2** (if time allows, ~3 min): pick 1–2 based on what emerged in Phase 3 verbal reflections.

**Priority 1 — Always ask:**

| Theme | Example question | Condition |
|---|---|---|
| Overall reflection | *"Looking back at all three scenarios — what was going through your mind? Was there a point where you felt most conflicted?"* | All |
| Decision reasoning | *"What was the main factor in your decision?"* | All |
| Self-harm distinction | *"Was there something different about the scenario where the cost fell on you rather than on others?"* | All |
| Omission tendency | *"Did you consider not acting at all? What made you stay or change your mind?"* | All |
| (Paired) Mutual influence | *"Did you and your partner discuss what to do? Who spoke first? Did the other person affect your decision?"* | Paired |
| (Paired) Comparative experience | *"What would have felt different doing this alone? Did their presence change how you experienced the scenarios?"* | Paired |
| (Paired) Coordination evolution | *"Did your approach change across the three scenarios — did you feel more like a team by the end?"* | Paired |

**Priority 2 — If time allows:**

| Theme | Example question | Condition |
|---|---|---|
| (Paired) Role negotiation | *"Did one of you naturally take charge? How did that feel?"* | Paired |
| (Paired) Synchrony | *"Was there a moment where you and your partner were clearly in sync — or clearly not?"* | Paired |
| Responsibility | *"Who do you feel is responsible for the outcome?"* | All |
| VR environment + design feedback | *"Did the virtual environment feel real enough to take the decision seriously? What made you most aware of your partner's presence?"* | All / Paired |

---

### Phase 5 — Full Debrief (4 minutes)

Delivered verbally by the experimenter and provided as a written sheet.

- True purpose revealed: the study examined moral decision-making under time pressure, and how the presence of another person in social VR influences these decisions.
- Cover story explained: why it was used and confirmation that all scenarios were simulated with no real-world consequences.
- Normalisation: there is no right or wrong answer; variation in responses is expected and scientifically interesting.
- Psychological support signposting: written card with contact information provided to all participants.
- Opportunity for participant questions.

---

## 8. Measures

### 8.1 Behavioural Measures (Unity Auto-Recorded)

| Measure | Description | Solo | Paired |
|---|---|---|---|
| Binary decision | Action (1) vs. inaction (0) | ✓ | ✓ |
| Decision latency | Milliseconds from narration end to action (or window close if no action) | ✓ | ✓ |
| Who performed the action | Participant A / Participant B / Neither | — | ✓ |
| All interaction attempts | Timestamp + participant ID for every attempt on the shared control | — | ✓ |
| Physical competition | Whether both participants attempted the shared control within 500ms | — | ✓ |
| Omission rate per scenario type | Proportion of inaction across scenario types | ✓ | ✓ |

### 8.2 Voice Analysis (Paired Condition Only)

| Measure | Method | What it captures |
|---|---|---|
| Speaking time per participant | Pyannote.audio speaker diarization | Who spoke more overall |
| Speech initiation | Diarization + manual annotation | Who speaks first after voiceover ends |
| Turn-taking and interruptions | Manual annotation | Conversation structure and dominance |
| Speech rate (words per minute) | Automated transcription + word count | Urgency and cognitive load |
| Mean F0 (fundamental frequency) | PRAAT | Arousal and emotional activation proxy |
| Sentiment analysis | VADER or equivalent on transcripts | Emotional valence across scenario types; compare actor vs. deferrer |

**Voice dominance operationalisation:** A participant is classified as dominant in a given scenario if they (a) speak for more than 60% of total conversation time OR (b) initiate at least two of three main discussion turns AND speak first after the voiceover ends.

### 8.3 Self-Report Questionnaires

| Measure | Scale | Solo | Paired | When |
|---|---|---|---|---|
| Perceived agency | 5-pt Likert | ✓ | ✓ | Post-scenario |
| Perceived responsibility | 5-pt Likert | ✓ | ✓ | Post-scenario |
| Decision satisfaction | 5-pt Likert | ✓ | ✓ | Post-scenario |
| Consequence consideration | 5-pt Likert | ✓ | ✓ | Post-scenario |
| Time pressure sensitivity | 5-pt Likert | ✓ | ✓ | Post-scenario |
| Omission tendency | 5-pt Likert | ✓ | ✓ | Post-scenario |
| Ecological validity check ("felt real") | 5-pt Likert | ✓ | ✓ | Post-scenario |
| Perceived partner influence | 5-pt Likert | — | ✓ | Post-scenario |
| Per-scenario social presence proxy | 5-pt Likert | — | ✓ | Post-scenario |
| Threat perception | 5-pt Likert | — | ✓ | Self-harm scenario only |
| ITC-SOPI co-presence (6 items) | 5-pt Likert | — | ✓ | Between scenarios 2–3 |
| "Close to partner right now" | 7-pt single item | — | ✓ | Between scenarios 2–3 |
| Experience changed how I feel about partner | 5-pt Likert + free-text | — | ✓ | Post-experiment |
| Big Five BFI-10 | 5-pt Likert | ✓ | ✓ | Pre-experiment |
| VR familiarity (custom 3-item) | 5-pt Likert | ✓ | ✓ | Pre-experiment |
| IOS Scale (relationship closeness) | Visual 7-point | — | ✓ | Pre-experiment |

### 8.4 Qualitative Data

| Source | Format | Condition | When |
|---|---|---|---|
| Per-scenario verbal reflections | Audio + transcript | All | After each scenario |
| In-scenario conversation (decision window + reflection) | Audio + transcript | Paired only | During each paired scenario |
| Semi-structured interview | Audio + transcript | All | Phase 4 |

### 8.5 Excluded Measures (with Justification)

**Eye tracking:** Removed due to technical complexity and resource constraints.

**Physiological measures (heart rate, EDA):** Excluded. Prior studies confirm autonomic arousal does not predict moral decision outcome (Francis et al., 2016; Miller, 2019; Navarrete et al., 2012). Including these measures would add setup time and participant burden without analytic benefit.

**Pre-experiment hypothetical choice:** Not included. The core research questions concern dyadic social influence dynamics, not the gap between hypothetical and actual choices (already established in prior literature). Qualitative verbal reflections after each scenario provide sufficient data on decision reasoning.

---

## 9. Hypotheses

### H1 — Social Co-Presence Alters Moral Decisions

Paired participants will make significantly different decisions than solo participants for at least one dilemma type, indicating that real-time social co-presence in VR alters moral decision-making under time pressure.

*Theoretical basis:* Diffusion of moral agency (Darley & Latané, 1968; Gawronski & Ng, 2024). A second active participant reduces the individual psychological cost of acting by distributing perceived responsibility.

---

### H2a — Social Influence Increases Utilitarian Action in Harm-to-Others Conditions

For the Bystander and Driver conditions, paired participants will show higher utilitarian action rates than solo participants. The presence of a second active agent distributes perceived moral responsibility, reducing the individual cost of acting when harm falls on a third party.

*Theoretical basis:* CNI model I-parameter (Gawronski & Ng, 2024); diffusion of responsibility (Darley & Latané, 1968).

---

### H2b — Self-Harm Condition Shows Relationship-Modulated Pattern (Exploratory)

In the Self-harm condition, the direction of the social influence effect is predicted to depend on relationship closeness — producing a crossover pattern:

- **Stranger pairs:** Reduced or equivalent self-sacrifice relative to solo. Each participant may wait for the other to act (diffusion of self-risk; implicit bystander effect).
- **Close pairs (friends and romantic partners):** Maintained or increased self-sacrifice relative to solo. A trusted partner's co-presence as a fellow potential actor reduces the perceived threat cost of self-harm (Social Baseline Theory; Coan & Beckes, 2015).

*This is an exploratory, descriptive prediction. With N=5 pairs per group, statistical testing is not appropriate. The finding generates the first empirical comparison of dyadic self-sacrifice decisions by relationship closeness in VR moral dilemma research — treated as hypothesis-generating for future work. Quantitative decision rates provide directional context; the primary contribution is qualitative, characterising how stranger vs. close pairs navigate shared self-sacrifice decisions through verbal negotiation, role emergence, and coordination dynamics.*

---

### H3 — Social Influence Reduces Perceived Agency

Within the paired condition, participants who report higher perceived partner influence will report lower perceived agency and lower decision satisfaction, regardless of the direction of their final decision.

*Theoretical basis:* When another person is perceived as influencing a moral decision, individual ownership of that decision is reduced, manifesting in self-attribution measures (agency, responsibility) and evaluative measures (satisfaction).

---

### H4 — Personality and Voice Predict Who Acts (Exploratory)

In the paired condition, the participant who physically performs the action will score higher on conscientiousness and lower on agreeableness than the participant who defers, and will demonstrate voice dominance (higher speaking time; initiates discussion first).

*Analysis approach:* Descriptive comparison of conscientiousness and agreeableness between actor and deferrer groups (means and 95% confidence intervals). No regression — sample size does not support reliable regression coefficients. This hypothesis is exploratory and hypothesis-generating for future work.

---

## 10. Analysis Plan

### 10.1 Quantitative Analysis

**H1 — Solo vs. Paired Decision Rates (Primary)**

For each of the three dilemma types separately, a chi-square test of independence compares utilitarian decision proportions between solo and paired conditions.
- Test: Pearson chi-square (2-sided)
- Effect size: Cohen's h and odds ratios with 95% confidence intervals
- Significance threshold: α = .05
- Report effect sizes and confidence intervals alongside p-values

**H2a — Cross-Scenario Comparison (Bystander + Driver)**

Within each condition (solo, paired), McNemar's test compares decision rates across Bystander and Driver scenarios (within-subjects binary outcomes), establishing whether scenario type modulates the social influence effect.

**H2b — Self-Harm Typology (Exploratory, Descriptive Only)**

No inferential tests. Report decision rates per group (Strangers / Close) with exact counts and proportions. Scenario position (1st / 2nd / 3rd in counterbalanced order) is included as a covariate — expected to reflect moral coordination emergence, with decision rates and conversation patterns shifting as pairs develop a shared approach across scenarios. Relationship duration (in months, for close pairs) is reported as contextual information. Use the qualitative layer — approach-phase in-scenario audio, verbal reflections, interview themes — to explain the observed pattern. Present as a cross-case typology contrasting Strangers vs. Close, not a statistical comparison.

**Moral coordination emergence analysis:** Using the approach-phase audio (scene load to critical window), characterise whether and how pairs verbalise coordination across the three scenarios. Expected pattern: close pairs establish implicit roles by scenario 2; stranger pairs remain more independent. This is a secondary qualitative finding reported alongside H2b.

**H3 — Partner Influence, Agency, Satisfaction**

Pearson correlation (or Spearman if non-normal) between perceived partner influence score and agency/satisfaction scores, collapsed across scenarios within the paired condition.

Consider a simple mediation: does "who physically acted" mediate the relationship between perceived partner influence and perceived agency? This provides a cleaner causal story.

**H4 — Personality and Voice Dominance (Exploratory)**

Descriptive comparison: mean conscientiousness and agreeableness for actor vs. deferrer participants in the paired condition, with 95% confidence intervals. No logistic regression.

**Additional Quantitative Analyses**

- *Decision latency:* Mixed-model ANOVA (condition × scenario type), using latency from narration end. Log-transform if skewed. Expected: paired condition produces shorter latency (social facilitation) in Bystander/Driver; longer in Self-harm (deference uncertainty).
- *Omission rate:* Chi-square per scenario type and condition. Expected: Bystander shows highest omission rate.
- *Co-presence as moderator:* ITC-SOPI score as continuous moderator in regression models predicting perceived partner influence and decision change.
- *Ecological validity check:* Compare "felt real" scores across scenario types. If Self-harm scores are significantly lower, flag as a validity threat for that condition.

### 10.2 Qualitative Analysis

Reflexive thematic analysis (Braun & Clarke, 2006) applied to all qualitative data sources.

**Primary analytical questions:**
1. What mechanisms of social influence operate in paired interactions? (decision pressure, verbal persuasion, deference, role negotiation)
2. How do participants narrate responsibility and agency after a decision? Does this differ between actor and deferrer?
3. How does the experience differ across the three dilemma types? What makes the Self-harm condition qualitatively distinct?
4. What is the role of the VR environment and avatar representation in the social dynamics?

**For the Self-harm + relationship type typology:**
Present the three relationship groups as a cross-case comparison (3–4 pairs per type). Identify patterns in:
- Who initiated the decision verbally
- Whether self-sacrifice was discussed or unilateral
- How participants describe the role of their partner in the decision to sacrifice (or not)

### 10.3 Mixed-Method Integration

Convergent parallel approach: both strands analysed independently, then compared at interpretation.

| Quantitative finding | Qualitative triangulation |
|---|---|
| H1: Decision rates differ between solo and paired | Interview: how participants describe the partner's influence on their decision process |
| H2: Self-harm crossover by relationship type | Verbal reflections + interview: how participants narrate the partner's role in a self-sacrifice decision |
| H3: Partner influence → lower agency | Interview: ownership and responsibility attribution after dyadic decisions |
| H4: Voice dominance predicts who acts | In-scenario audio: role negotiation, verbal pressure, silence patterns |

**Complexity cases:** Prepare at least 3 cases where quantitative and qualitative data diverge (e.g., a participant reports high partner influence but voice data shows they spoke more and acted first). These are analytically significant and discussed in the paper.

---

## 11. Ethical Considerations

### 11.1 Standard Protocol

- All participants complete informed consent before any VR exposure
- Participants can withdraw at any point without consequence or explanation
- No deception beyond the cover story, which is fully explained in the debrief
- All data stored at CWI only, pseudonymised prior to analysis
- Audio recordings deleted after transcription (within 3 months)
- Questionnaire and behavioural data retained for 10 years

### 11.2 Participant Wellbeing

The Self-harm scenario involves a virtual representation of personal injury. This is substantially less ethically sensitive than a Fat Man footbridge condition, as the cost falls on the participant and no other avatar is harmed. The injury is represented abstractly (camera shake, brief visual effect).

A welfare check is conducted after Phase 3 regardless of condition. Participants are reminded before the Self-harm scenario that they can stop at any time.

### 11.3 Debrief and Signposting

All participants receive a written debrief sheet and a psychological support card, provided regardless of whether the participant shows any signs of distress.

---

## 12. Unity Technical Specification

### 12.1 Avatar System

- Stylised customisable avatars (body type, skin tone, hair colour, height) — see Section 6
- Both participants must be visible to each other with latency under 100ms
- Log all avatar configuration choices per participant as covariates

### 12.2 Shared Interaction Object

- One button per scenario; shared across both clients with synchronised state
- When Participant A presses: B sees A's arm movement AND the button responds visually in real time
- Interaction enabled only during the critical decision window; disabled immediately when window closes OR when first action registers
- No further interaction possible after window closes

### 12.3 Logging Requirements

For each scenario, log at minimum:
- `scenario_id`, `participant_id`, `condition` (solo/paired)
- `narration_end_timestamp` (when voiceover finishes — latency is measured from this point)
- `critical_window_start_timestamp`
- `critical_window_end_timestamp` (window close or first action, whichever is first)
- `interaction_attempts[]`: array of {`participant_id`, `timestamp`, `action_type`} for every attempt
- `decision_registered`: `{participant_id, timestamp, action_type}` or `null` if inaction
- `competition_flag`: boolean — true if both participants attempted within 500ms of each other
- `avatar_config`: body type, skin tone, hair colour, height

### 12.4 Questionnaire Privacy (Paired Condition)

After each scenario, participants are teleported to separate private virtual rooms before the questionnaire appears. Rooms are audio- and visually isolated. The ITC-SOPI and closeness item (between scenarios 2 and 3) is also completed in private rooms before participants are reunited.

### 12.5 Audio Recording

All audio is recorded throughout the session:
- Separate channel per participant
- Recording begins at scene load, ends when rest screen appears
- During verbal reflection sub-phase: a soft timer (60s) is displayed; the experimenter does not intervene

---

## 13. Change Log from Previous Versions

| Parameter | Previous (final_protocol.docx) | Version 2 |
|---|---|---|
| Ought-behavior gap / ought-self framing | Present in theoretical framing, interview, and written reflection | **Removed entirely** — not the core RQ; qualitative reflections cover decision reasoning adequately |
| H2 framing | Actionness gradient (Fat Man > Driver > Bystander) | **Social Baseline Theory crossover** — direction reversal in Self-harm by relationship closeness |
| H2 theoretical basis | CNI model I-parameter + directness of action | **Coan & Beckes (2015) Social Baseline Theory added** for Self-harm prediction |
| H4 analysis method | Binary logistic regression | **Simplified to descriptive comparison** — regression underpowered at N≈18 paired |
| Sample size | N=40–50 (10–12 pairs) | **N=40 exactly (10 pairs: 4 strangers, 3 friends, 3 romantic partners)** |
| Relationship type group sizes | Unspecified | **Explicit: 4/3/3 design with qualitative-only analysis per group** |
| IOS Scale | Not included | **Added pre-experiment (paired only)** |
| Post-scenario items | 7 items | **10 items — added: "felt real", per-scenario social presence proxy, Self-harm threat perception** |
| Between-scenario item | ITC-SOPI only | **Added: "How close do you feel to your partner right now?" (1 item)** |
| Post-experiment items | Interview only | **Added: "changed how I feel about partner"; "what made you aware of partner"; cover story check** |
| Interview questions | 9 themes | **13 themes — added: overall reflection; comparative experience; synchrony; design feedback** |
| Written reflection (Phase 4) | Standalone 3-minute written section | **Removed — questions integrated into interview ("Overall reflection across scenarios"; "Comparative experience")** |
| Cover story | "Decision-Making in Challenging Environments" | **AI framing** — topically credible, suppresses moral priming |
| Cover story effectiveness check | Not included | **Added as post-experiment item** |
| Avatar design guidance | Not addressed | **New section — self-customised stylised avatars (body type, skin tone, hair, height); cwipc removed; study at TU Delft** |
| Unity technical spec | Not specified | **New section — shared object synchronisation, logging requirements, competition definition** |
| Self-harm paired design | Not specified | **Asymmetric: one participant controls, other is co-present** |
| Sentiment analysis | Not included | **Added to voice analysis pipeline** |
| Sample size | N=48 (24 solo + 24 paired = 12 pairs) | **N=40 (20 solo + 20 paired = 10 pairs)** — power for h≥0.64 maintained at N=20/group |
| Relationship groups | 4 groups (strangers / colleagues / friends / romantic partners, 3 pairs each) | **2 groups (Strangers vs. Close, 5 pairs each)** — Close merges friends + romantic; colleagues dropped; contrast maps directly onto SBT trusted/non-trusted distinction |
| Self-harm paired design | Asymmetric — one participant controls, other is co-present but cannot act | **Symmetric — either participant can act; first press wins** — restores full shared agency required by SBT load-sharing prediction |
| H2b framing | Empirical typology across 4-group gradient | **Strangers vs. Close qualitative contrast** — quantitative rates as context; primary contribution is verbal negotiation and coordination emergence |
| Scenario position | Not included | **Added as analysis covariate** — captures moral coordination emergence arc across scenarios 1→3 |
| Interview | Priority 1: 6 themes | **Added: coordination evolution question** — "Did your approach change across the three scenarios — did you feel more like a team by the end?" |
| SBT section | Colleagues as intermediate position | **Removed** — 2-group design makes intermediate category unnecessary |
| Moral coordination emergence | Not included | **Added as secondary qualitative finding** — approach-phase audio used to characterise how stranger vs. close pairs develop shared decision style across scenarios |

---

## 14. Key References

Aron, A., Aron, E.N., & Smollan, D. (1992). Inclusion of other in the self scale and the structure of interpersonal closeness. *Journal of Personality and Social Psychology*, 63(4), 596–612.

Braun, V., & Clarke, V. (2006). Using thematic analysis in psychology. *Qualitative Research in Psychology*, 3(2), 77–101.

Coan, J.A., Schaefer, H.S., & Davidson, R.J. (2006). Lending a hand: Social regulation of the neural response to threat. *Psychological Science*, 17(12), 1032–1039.

Coan, J.A., & Beckes, L. (2015). The social baseline theory and the developmental neuroscience of resilience. In *The Oxford Handbook of Developmental Psychopathology.*

Darley, J.M., & Latané, B. (1968). Bystander intervention in emergencies: Diffusion of responsibility. *Journal of Personality and Social Psychology*, 8(4), 377–383.

Evans, J.St.B.T., & Stanovich, K.E. (2013). Dual-process theories of higher cognition: Advancing the debate. *Perspectives on Psychological Science*, 8(3), 223–241.

Foot, P. (1967). The problem of abortion and the doctrine of double effect. *Oxford Review*, 5, 1–7.

Francis, K.B., Howard, C., Howard, I.S., Gummerum, M., Ganis, G., Anderson, G., & Terbeck, S. (2016). Virtual morality: Transitioning from moral judgment to moral action? *PLOS ONE*, 11(10), e0164374.

Gawronski, B., & Ng, N.L. (2024). Beyond trolleyology: The CNI model of moral dilemma responses. *Personality and Social Psychology Review.*

Miller, D.B. (2019). *Human-computer conflicts in partially-automated driving.* Doctoral dissertation, Stanford University.

Monin, B., Pizarro, D.A., & Beer, J.S. (2007). Deciding versus reacting: Conceptions of moral judgment and the reason-affect debate. *Review of General Psychology*, 11(2), 99–111.

Navarrete, C.D., McDonald, M.M., Mott, M.L., & Asher, B. (2012). Virtual morality: Emotion and action in a simulated three-dimensional "trolley problem". *Emotion*, 12(2), 364–370.

Niforatos, E., Metsis, V., & Langheinrich, M. (2020). Would you do it? Enacting moral dilemmas in virtual reality for understanding ethical decision-making. *CHI 2020.*

Rammstedt, B., & John, O.P. (2007). Measuring personality in one minute or less: A 10-item short version of the Big Five Inventory in English and German. *Journal of Research in Personality*, 41(1), 203–212.

Sütfeld, L.J.R., Gast, R., König, P., & Pipa, G. (2017). Using virtual reality to assess ethical decisions in road traffic scenarios. *Frontiers in Behavioral Neuroscience*, 11, 122.

Yee, N., & Bailenson, J. (2007). The Proteus effect: The effect of transformed self-representation on behavior. *Human Communication Research*, 33(3), 271–290.

Zaleska, M., & Kogan, N. (1971). Level of risk selected by individuals and groups when deciding for self and for others. *Journal of Personality*, 39(2), 198–213.
