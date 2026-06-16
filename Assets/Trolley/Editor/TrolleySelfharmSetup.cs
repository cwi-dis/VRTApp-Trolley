using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Builds the Self-harm scene by duplicating the (working) Driver scene.
    ///
    ///   Trolley > Build Selfharm From Driver
    ///
    /// Self-harm replicates the Driver cab + environment-movement divert exactly. The only
    /// change is the OUTCOME geometry, per the study protocol (STUDY_PROTOCOL_v2 §Scenario C,
    /// "A concrete barrier is to the side. Steering into the barrier will injure the participant
    /// but spare the workers. Inaction kills the five."):
    ///
    ///   • INACTION (straight) → the five workers ahead (kept from Driver).
    ///   • ACTION   (divert)   → a ROCKY MOUNTAIN / barrier on the side track. Steering into it
    ///                            represents self-harm; a dust impact burst fires on contact.
    ///
    /// So the single side-track worker from Driver is replaced by the rocky-mountain obstacle.
    ///
    /// NOTE — this matches the protocol and the existing questionnaire consequence text +
    /// narration_selfharm.mp3 (action = self-harm). If you instead want INACTION = self-harm,
    /// that flips the H2b self-sacrifice mapping; tell Claude and it's a small change here +
    /// in QuestionnaireController.BuildConsequenceText + a re-recorded narration.
    ///
    /// Non-destructive to the Driver scene (SaveScene saveAsCopy). Overwrites TrolleySelfharm.unity
    /// each run, so make manual tweaks only after the final run.
    /// </summary>
    public static class TrolleySelfharmSetup
    {
        const string SourceScene   = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string SelfharmScene = "Assets/Trolley/Scenes/TrolleySelfharm.unity";
        const string NarrationPath = "Assets/Trolley/Audio/narration_selfharm.mp3";

        static readonly Color RockColor = new Color(0.42f, 0.36f, 0.30f); // grey-brown rock

        [MenuItem("Trolley/Build Selfharm From Driver")]
        public static void BuildSelfharmFromDriver()
        {
            var src = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!src.IsValid())
            {
                Debug.LogError($"Build Selfharm: could not open {SourceScene}.");
                return;
            }

            if (!EditorSceneManager.SaveScene(src, SelfharmScene, saveAsCopy: true))
            {
                Debug.LogError($"Build Selfharm: failed to save copy to {SelfharmScene}.");
                return;
            }
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(SelfharmScene, OpenSceneMode.Single);

            // ── TrolleyController → selfharm ───────────────────────────────────
            var controller = Object.FindFirstObjectByType<TrolleyController>();
            if (controller != null)
            {
                controller.scenarioID = "selfharm";
                controller.isTutorial = false;
                EditorUtility.SetDirty(controller);
            }
            else Debug.LogWarning("Build Selfharm: TrolleyController not found in duplicated scene.");

            // ── Replace the side-track worker(s) with a rocky-mountain barrier ─
            // ActionTrackWorkers ride with TrackEnvironment, so the mountain inherits the same
            // parent + local position to move identically toward the player.
            Transform parent = null;
            Vector3 localPos = new Vector3(6f, 0f, -15f); // sensible fallback if not found
            var actionWorkers = GameObject.Find("ActionTrackWorkers");
            if (actionWorkers != null)
            {
                parent   = actionWorkers.transform.parent;
                localPos = actionWorkers.transform.localPosition;
                Object.DestroyImmediate(actionWorkers);
            }
            else Debug.LogWarning("Build Selfharm: ActionTrackWorkers not found — placing mountain at a fallback position; reposition manually.");

            var mountain = BuildRockyMountain(parent, localPos);
            var impact   = BuildImpactEffect(parent, localPos);

            // ── Wire DriverTrainController ─────────────────────────────────────
            var driver = Object.FindFirstObjectByType<DriverTrainController>();
            if (driver != null)
            {
                var dSO = new SerializedObject(driver);
                // No worker group to hide on the action track now — it's the mountain you crash into.
                dSO.FindProperty("actionHitWorkers").objectReferenceValue = null;
                // INACTION still mows down the five straight-ahead workers (kept from Driver).
                var inaction = GameObject.Find("InactionTrackWorkers");
                if (inaction != null)
                    dSO.FindProperty("inactionHitWorkers").objectReferenceValue = inaction;
                // Self-harm dust burst fires on the ACTION (divert into barrier) outcome.
                dSO.FindProperty("actionImpactEffect").objectReferenceValue = impact;
                dSO.FindProperty("impactOnAction").boolValue = true;
                dSO.ApplyModifiedProperties();
            }
            else Debug.LogWarning("Build Selfharm: DriverTrainController not found — wire the impact effect manually.");

            // ── Narration ──────────────────────────────────────────────────────
            var narration = Object.FindFirstObjectByType<NarrationPlayer>();
            if (narration != null)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(NarrationPath);
                var nSO = new SerializedObject(narration);
                var clipsProp = nSO.FindProperty("clips");
                if (clip != null)
                {
                    clipsProp.arraySize = 1;
                    clipsProp.GetArrayElementAtIndex(0).objectReferenceValue = clip;
                }
                else
                {
                    clipsProp.arraySize = 0;
                    Debug.LogWarning("Build Selfharm: narration_selfharm.mp3 not found — cleared clips.");
                }
                nSO.ApplyModifiedProperties();
            }

            AddToBuildSettings(SelfharmScene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Build Selfharm: TrolleySelfharm.unity created from Driver.\n" +
                      "Done: scenarioID=selfharm, side worker replaced by RockyMountain_SelfHarm + impact burst, " +
                      "DriverTrainController rewired, narration assigned, added to Build Settings.\n" +
                      "MANUAL: position/scale RockyMountain_SelfHarm so the divert visibly crashes into it; " +
                      "tune DriverTrainController.hitDelay to the impact moment. Camera shake on impact is a " +
                      "separate TODO (touches the XR rig — VR2Gather territory).");
        }

        static GameObject BuildRockyMountain(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("RockyMountain_SelfHarm");
            root.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Build Selfharm From Driver";
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            // A few overlapping boulders read as a rocky mountain / barrier.
            AddRock(root.transform, new Vector3(0f, 2f, 0f),     new Vector3(6f, 8f, 4f),  0f);
            AddRock(root.transform, new Vector3(-2.5f, 1f, 1f),  new Vector3(3f, 4f, 3f),  20f);
            AddRock(root.transform, new Vector3(2.5f, 1.2f, -1f),new Vector3(3.5f, 5f, 3f),-15f);
            return root;
        }

        static void AddRock(Transform parent, Vector3 localPos, Vector3 scale, float yaw)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock";
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = localPos;
            rock.transform.localScale = scale;
            rock.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var rend = rock.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = Object.Instantiate(rend.sharedMaterial);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RockColor);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", RockColor);
                rend.sharedMaterial = mat;
            }
        }

        static GameObject BuildImpactEffect(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("SelfHarmImpactEffect");
            go.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Build Selfharm From Driver";
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos + new Vector3(0f, 1.5f, -2f); // in front of the rock face

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 4f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.55f, 0.45f, 0.35f); // dusty brown
            main.loop = false;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });
            go.SetActive(false); // DriverTrainController re-activates + plays it on impact
            return go;
        }

        static void AddToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Build Selfharm: added {scenePath} to Build Settings.");
        }
    }
}
