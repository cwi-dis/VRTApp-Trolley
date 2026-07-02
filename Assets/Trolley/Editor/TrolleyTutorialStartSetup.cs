using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Adds a participant-facing world-space "Start Tutorial" panel to the CURRENTLY OPEN tutorial scene
    /// and wires it into that scene's drill (TutorialBystanderDrill or TutorialDriverDrill).
    ///
    ///   Trolley > Add Tutorial Start Button (open scene)
    ///
    /// Builds a dark rounded card with a title ("Driver Tutorial" / "Bystander Tutorial") and a green
    /// "Start" button (XR-raycastable, same pattern as the questionnaire panels), a TutorialGate to drive
    /// it, and assigns the gate to the drill's 'gate' field. With a Start button wired, the drill opens
    /// with a free A/B warm-up and waits for the press before beginning. Rebuilds cleanly on re-run.
    ///
    /// Placement: centred in front of the seat by default — reposition OBJ_TutorialStart per scene so it
    /// sits in the participant's view. Run once in each tutorial scene.
    /// </summary>
    public static class TrolleyTutorialStartSetup
    {
        const string HostName = "OBJ_TutorialStart";
        static readonly Color CardBg     = new Color(0.05f, 0.05f, 0.06f);
        static readonly Color TextPrimary= new Color(0.96f, 0.96f, 0.97f);
        static readonly Color TextMuted  = new Color(0.60f, 0.62f, 0.66f);
        static readonly Color BtnGreen   = new Color(0.25f, 0.61f, 0.21f);

        [MenuItem("Trolley/Add Tutorial Start Button (open scene)")]
        public static void AddStartButton()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.Contains("Tutorial"))
            {
                Debug.LogError("Add Start Button: open a TrolleyTutorial* scene first — this menu builds into the open scene.");
                return;
            }

            // Preserve placement across re-runs: keep the host's transform if it already exists, then
            // rebuild fresh (also handles switching from the old physical button to this UI panel).
            Vector3 pos = new Vector3(0f, 1.4f, 1.2f);   // centred in front of the seat — reposition per scene
            Quaternion rot = Quaternion.Euler(0f, 180f, 0f);
            var existing = GameObject.Find(HostName);
            if (existing != null)
            {
                pos = existing.transform.position;
                rot = existing.transform.rotation;
                Object.DestroyImmediate(existing);
            }

            // Host holds the gate and stays active; the canvas child is what the gate shows/hides.
            var host = new GameObject(HostName);
            host.transform.SetPositionAndRotation(pos, rot);
            var gate = host.AddComponent<TutorialGate>();

            // World-space canvas + XR raycaster (same pattern as the questionnaire panels).
            var canvasGO = new GameObject("StartCanvas");
            canvasGO.transform.SetParent(host.transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 380f);
            canvasGO.transform.localScale = Vector3.one * 0.003f;

            // Card background — dark, rounded (matches the reflection panel aesthetic).
            AddRoundedImage(canvasGO, CardBg);

            // Title — names the tutorial so it doesn't read as a random floating button.
            var title = CreateTMP("Title", canvasGO, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.9f),
                44, SceneTitle());
            title.alignment = TextAlignmentOptions.Center;
            title.color = TextPrimary;
            title.fontStyle = FontStyles.Bold;

            // Subtitle — a short cue.
            var sub = CreateTMP("Subtitle", canvasGO, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.6f),
                24, "Press Start when you're ready.");
            sub.alignment = TextAlignmentOptions.Center;
            sub.color = TextMuted;

            // Start button — centred, green.
            var btnGO = new GameObject("StartButton");
            btnGO.transform.SetParent(canvasGO.transform, false);
            var brect = btnGO.AddComponent<RectTransform>();
            brect.anchorMin = new Vector2(0.34f, 0.12f);
            brect.anchorMax = new Vector2(0.66f, 0.34f);
            brect.offsetMin = brect.offsetMax = Vector2.zero;
            AddRoundedImage(btnGO, BtnGreen);
            var button = btnGO.AddComponent<Button>();

            var label = CreateTMP("Label", btnGO, Vector2.zero, Vector2.one, 34, "Start");
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            // Wire the gate (uiButton + visualRoot = canvas) and the drill's 'gate' field.
            var gSO = new SerializedObject(gate);
            gSO.FindProperty("uiButton").objectReferenceValue = button;
            gSO.FindProperty("visualRoot").objectReferenceValue = canvasGO;
            gSO.ApplyModifiedProperties();

            bool wiredDrill = WireDrillGate(gate);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"Add Start Button: world-space '{SceneTitle()}' start panel ready in {scene.name} — " +
                      $"drill.gate {(wiredDrill ? "✓" : "✗ — wire it manually on the drill")}.\n" +
                      "MANUAL: reposition OBJ_TutorialStart so the panel sits centred in the participant's view. " +
                      "Needs an EventSystem with an XR UI input module in the scene (the VR2Gather rig provides one).");
        }

        // Title from whichever drill is in the scene.
        static string SceneTitle()
        {
            if (Object.FindFirstObjectByType<TutorialDriverDrill>() != null) return "Driver Tutorial";
            if (Object.FindFirstObjectByType<TutorialBystanderDrill>() != null) return "Bystander Tutorial";
            return "Tutorial";
        }

        // Assign the gate to whichever drill is in the scene (bystander or driver). Both expose 'gate'.
        static bool WireDrillGate(TutorialGate gate)
        {
            foreach (var drill in new MonoBehaviour[] {
                         Object.FindFirstObjectByType<TutorialBystanderDrill>(),
                         Object.FindFirstObjectByType<TutorialDriverDrill>() })
            {
                if (drill == null) continue;
                var so = new SerializedObject(drill);
                var prop = so.FindProperty("gate");
                if (prop == null) continue;
                prop.objectReferenceValue = gate;
                so.ApplyModifiedProperties();
                return true;
            }
            return false;
        }

        // ── UI helpers ──────────────────────────────────────────────────────────

        static void AddRoundedImage(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            var spr = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Sliced; }
            img.color = color;
        }

        static TextMeshProUGUI CreateTMP(string name, GameObject parent, Vector2 min, Vector2 max,
            float fontSize, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.color = Color.white; tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }
    }
}
