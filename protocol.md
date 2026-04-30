 Study design


  1. How many trolley scenarios, and which variants? (e.g., switch, footbridge, loop, bystander) Will all
  participants see the same set, or is there counterbalancing across scenarios?

  There will be either two or three scenarios. 
  a) bystander with switch or level
  b) driver with switch
  c) (optional) footbridge OR driver with switch, but that gives yourself a consequence by hitting the wall
  Either two or three scenarios will be counterbalanced. If time allows I wish to have three scenarios. All participants should experience all three scenarios.

  2. For the paired condition: do both participants see the dilemma simultaneously and discuss, then each
  vote independently, or does one person make the final call? Is there a time limit to decide?

  For the paird condition, both participants see the dilemma simultaneously and discuss. But throughout all the cases (both solo and paired), the decision time will be around 5 seconds. In this immersive environment, there should be also UI that shows you the time left. If the time pass, then it indicates that the participant didn't make decision which is inaction. There will be only one button or lever, which is an action point, and either of them can trigger the action.
  Since this is the case, before the train actually approaches, there will be a voice narration before the timer starts, to ensure that the participant is fully aware of what is going on in the scenario and what is the consequences of their actions.
  


  3. For the solo condition: is it mechanically the same scene but no partner present, or a genuinely
  different setup?

  For the solo condition, it is mechanically the same scene.


  Decision mechanics
  4. How does a participant actually make the choice in VR — physically pull a lever, press a controller
  button, gaze at a choice panel, something else? Should the lever/action trigger an animation (trolley
  moves, workers react)?

  I was thinking of pull a lever for the bystander and button for the driver. But it seems that the action should be easy enough due to the given decision time span. What is your opinion?


  Questionnaires
  5. When do questionnaires appear — before, after each scenario, after all scenarios, or some mix? Are
  they standard validated scales (e.g., IRI empathy, STAI, moral decision scales) with fixed question
  counts, or custom?

  The questionnaires should appear in between scenes. I'll probably want to do it with custom as there are several questions that I want to ask. 


  6. How do participants answer in VR — controller ray-cast on a Likert panel, laser pointer on a floating
  UI?

  It doesn't really matter, depending on which is the most easiest way to implement and also the conventional way in VR setup.
  The more important notion here is that for the paired participants, I want them not to see or hear each others answers in this in-between questionnaire answering moments. That means that we might need to move them to different scenes. Because other than the questionnaire, I'll also give them around 15 seconds to verify their choices - why did you make this decision - for solo condition as well. For paired condition, it may add additional question if they are satisfied about their decision, or what was the dynamic or influence of another participant.


  Flow & data
  7. What's the full session flow from login to end? (e.g., Login → Consent → Tutorial → Scenario 1 → Q →
  Scenario 2 → Q → Debrief → Exit)

  Consent will be done outside VR. Inside VR, they will go through a short tutorial. Thus the flow would be: tutorial -> scenario 1 -> Q -> scenario 2 -> Q -> scenario 3 -> Q -> maybe additional overal Questionnaire -> Exit. Debrief and interview will be outside of VR. 

  8. What data needs to be logged and where — decision outcome, response time, questionnaire answers,
  written to a local file, REST endpoint, or through VR2Gather's orchestrator?

  Decision outcome, response time, questionanire answers should definitely written to a local file. 
  What else should it better save? I will also do a voice analysis. thus it means that we need to record the voice too. but we anyways need to record the video for the later analysis.

  Platform
  9. Target device — Meta Quest standalone, tethered PC VR, or both? This affects build settings and
  interaction toolkit choices.

  Now we are using Meta Quest. I assume that VR2Gather already have those interaction toolkit installed.