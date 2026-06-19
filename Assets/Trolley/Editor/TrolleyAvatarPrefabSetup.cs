using UnityEngine;
using UnityEditor;
using UnityEngine.Animations.Rigging;
using VRT.Pilots.Common;
using System.Linq;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Sets up a Humanoid FBX avatar so it can replace P_Mannequin as a VR2Gather
    /// player representation. After running this, save the configured object as a prefab
    /// (e.g. P_Avatar_Trolley_Male, P_Avatar_Trolley_Female) and assign it as
    /// altRepOne/altRepTwo in P_Player_Trolley or P_Self_Player_Trolley.
    ///
    /// Designed for Humanoid Mixamo rigs (Remy, Megan, etc.).
    /// Could be upstreamed to VR2Gather as a generic "Setup Humanoid Avatar" tool.
    ///
    /// What is wired automatically:
    ///   - SyncSkeletonToVRRig.rigTargets  → skeleton bones / IK targets
    ///   - SizeAdjust.Destination/DestinationTop/Bottom → skeleton bones
    ///   - Two Bone IK constraints for arms and legs (leg targets include Rigidbody + BoxCollider + KeepFeetAboveGround)
    ///
    /// What is intentionally left null (filled at runtime by PlayerRepresentationWirer):
    ///   - SyncSkeletonToVRRig.vrTargets
    ///   - SizeAdjust.SourceTop / SourceBottom
    /// </summary>
    public static class TrolleyAvatarPrefabSetup
    {
        [MenuItem("Trolley/Setup Humanoid Avatar Prefab")]
        static void SetupHumanoidAvatarPrefab()
        {
            // Expected hierarchy when this menu item is run:
            //   wrapperGO  (e.g. "P_Avatar_Trolley_Male")   ← select this
            //     └─ modelGO  (e.g. "Remy")                 ← has the Animator
            //
            // This mirrors P_Mannequin's structure:
            //   P_Mannequin  ← SizeAdjust + PlayerRepresentationWirer live here
            //     └─ Ch36_nonPBR  ← RigBuilder + VR Constraints live here

            var wrapperGO = Selection.activeGameObject;
            if (wrapperGO == null)
            {
                Debug.LogError("[TrolleyAvatarPrefabSetup] Select the wrapper GameObject " +
                               "(e.g. P_Avatar_Trolley_Male) in the Hierarchy first.");
                return;
            }

            // The Animator must be on a child (the model root), not the wrapper itself.
            // GetComponentInChildren includes the GO itself, so check for that case explicitly.
            var animator = wrapperGO.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("[TrolleyAvatarPrefabSetup] No Animator found on any child. " +
                               "Drag the FBX (Remy/Megan) as a child of the wrapper GO first.");
                return;
            }
            if (animator.gameObject == wrapperGO)
            {
                Debug.LogError("[TrolleyAvatarPrefabSetup] The Animator is on the selected GO itself, " +
                               "not on a child. Create an empty wrapper GO, put the FBX as its child, " +
                               "then select the wrapper and re-run.");
                return;
            }
            if (!animator.isHuman)
            {
                Debug.LogError($"[TrolleyAvatarPrefabSetup] '{animator.gameObject.name}' has an Animator " +
                               "but its rig is not Humanoid. Open the FBX importer, set Rig > Animation Type " +
                               "to Humanoid, click Apply, then re-run.");
                return;
            }

            var modelGO = animator.gameObject;

            // Collect bones via the Humanoid avatar — model-agnostic
            var headBone      = animator.GetBoneTransform(HumanBodyBones.Head);
            var neckBone      = animator.GetBoneTransform(HumanBodyBones.Neck);
            var hipsBone      = animator.GetBoneTransform(HumanBodyBones.Hips);
            var leftUpperArm  = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var leftLowerArm  = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var leftHandBone  = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var leftUpperLeg  = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var leftLowerLeg  = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            var leftFoot      = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            var rightFoot     = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            var missingBones = new System.Collections.Generic.List<string>();
            if (headBone == null)      missingBones.Add("Head");
            if (neckBone == null)      missingBones.Add("Neck");
            if (hipsBone == null)      missingBones.Add("Hips");
            if (leftUpperArm == null)  missingBones.Add("LeftUpperArm");
            if (leftLowerArm == null)  missingBones.Add("LeftLowerArm");
            if (leftHandBone == null)  missingBones.Add("LeftHand");
            if (rightUpperArm == null) missingBones.Add("RightUpperArm");
            if (rightLowerArm == null) missingBones.Add("RightLowerArm");
            if (rightHandBone == null) missingBones.Add("RightHand");
            if (leftUpperLeg == null)  missingBones.Add("LeftUpperLeg");
            if (leftLowerLeg == null)  missingBones.Add("LeftLowerLeg");
            if (leftFoot == null)      missingBones.Add("LeftFoot");
            if (rightUpperLeg == null) missingBones.Add("RightUpperLeg");
            if (rightLowerLeg == null) missingBones.Add("RightLowerLeg");
            if (rightFoot == null)     missingBones.Add("RightFoot");
            if (missingBones.Count > 0)
            {
                Debug.LogError("[TrolleyAvatarPrefabSetup] Missing bones in Humanoid avatar mapping: " +
                               string.Join(", ", missingBones) + ". " +
                               "Open the FBX importer > Rig > Configure Avatar to check the bone assignments.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Humanoid Avatar Prefab");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. RigBuilder on the model GO (where the Animator is)
            var rigBuilder = modelGO.GetComponent<RigBuilder>();
            if (rigBuilder == null)
                rigBuilder = Undo.AddComponent<RigBuilder>(modelGO);

            // 2. "VR Constraints" child of the model GO — holds the Rig and IK solvers
            var vrConstraintsGO = new GameObject("VR Constraints");
            Undo.RegisterCreatedObjectUndo(vrConstraintsGO, "Create VR Constraints");
            vrConstraintsGO.transform.SetParent(modelGO.transform, false);
            var rig = vrConstraintsGO.AddComponent<Rig>();

            // 3. Two Bone IK for each arm and leg (children of VR Constraints)
            var leftArmTarget  = CreateTwoBoneIK(vrConstraintsGO.transform, "Left Arm IK",
                                     leftUpperArm, leftLowerArm, leftHandBone);
            var rightArmTarget = CreateTwoBoneIK(vrConstraintsGO.transform, "Right Arm IK",
                                     rightUpperArm, rightLowerArm, rightHandBone);

            // Leg IK targets are physics-driven (gravity + collider) so feet rest on the floor.
            // Hint offset is forward (+Z) so knees bend naturally in front of the body.
            var leftLegTarget  = CreateTwoBoneIK(vrConstraintsGO.transform, "Left Leg IK",
                                     leftUpperLeg, leftLowerLeg, leftFoot,
                                     hintOffset: Vector3.forward * 0.3f);
            var rightLegTarget = CreateTwoBoneIK(vrConstraintsGO.transform, "Right Leg IK",
                                     rightUpperLeg, rightLowerLeg, rightFoot,
                                     hintOffset: Vector3.forward * 0.3f);
            AddLegPhysics(leftLegTarget.gameObject,  wrapperGO);
            AddLegPhysics(rightLegTarget.gameObject, wrapperGO);

            // 4. Register rig layer in RigBuilder
            rigBuilder.layers.Add(new RigLayer(rig, true));
            EditorUtility.SetDirty(rigBuilder);

            // 5. SyncSkeletonToVRRig as a child of the wrapper (sibling of the model GO,
            //    matching P_Mannequin's layout).
            //    rigTargets → skeleton bones / IK targets.
            //    vrTargets left null — PlayerRepresentationWirer fills them at runtime.
            var existingSync = wrapperGO.GetComponentInChildren<SyncSkeletonToVRRig>();
            if (existingSync != null)
            {
                Debug.LogError("[TrolleyAvatarPrefabSetup] A SyncSkeletonToVRRig already exists under this GO. " +
                               "Revert to the pre-setup prefab before re-running.");
                Undo.CollapseUndoOperations(undoGroup);
                Undo.RevertAllInCurrentGroup();
                return;
            }

            var syncGO = new GameObject("SyncSkeletonToVRRig");
            Undo.RegisterCreatedObjectUndo(syncGO, "Create SyncSkeletonToVRRig");
            syncGO.transform.SetParent(wrapperGO.transform, false);
            var sync = syncGO.AddComponent<SyncSkeletonToVRRig>();

            // VRMap fields are class references — null on a fresh component; initialize before use
            sync.head      = new SyncSkeletonToVRRig.VRMap();
            sync.neck      = new SyncSkeletonToVRRig.VRMap();
            sync.leftHand  = new SyncSkeletonToVRRig.VRMap();
            sync.rightHand = new SyncSkeletonToVRRig.VRMap();

            // head.rigTarget must NOT be the actual head bone. SyncSkeletonToVRRig.LateUpdate()
            // calls head.Map() first, then neck.Map(). Map() adds a delta to rigTarget, and
            // neck.Map() then moves the entire skeleton (including the head) a second time —
            // causing double-movement. We use a proxy GO instead: it tracks VR head direction
            // (needed for mannequinTransform.forward on line 63 of SyncSkeletonToVRRig) without
            // touching any skeleton bone.
            var headConstraintGO = new GameObject("Head Constraint");
            Undo.RegisterCreatedObjectUndo(headConstraintGO, "Create Head Constraint");
            headConstraintGO.transform.SetParent(vrConstraintsGO.transform, false);
            headConstraintGO.transform.SetPositionAndRotation(headBone.position, headBone.rotation);
            sync.head.rigTarget = headConstraintGO.transform;

            // neck drives the body root position:
            // Map() computes delta = vrTarget − rigSource (neck bone), applies it to rigTarget (hips).
            sync.neck.rigTarget        = hipsBone;
            sync.neck.rigSource        = neckBone;
            sync.neck.positionOnly     = true;
            sync.neck.includeYRotation = true;

            sync.leftHand.rigTarget  = leftArmTarget;
            sync.rightHand.rigTarget = rightArmTarget;
            sync.mannequinTransform  = modelGO.transform;
            EditorUtility.SetDirty(sync);

            // 6. SizeAdjust on the wrapper root.
            //    SourceTop/SourceBottom left null — PlayerRepresentationWirer fills them.
            var sizeAdjust = wrapperGO.GetComponent<SizeAdjust>();
            if (sizeAdjust == null)
                sizeAdjust = Undo.AddComponent<SizeAdjust>(wrapperGO);

            Undo.RecordObject(sizeAdjust, "Wire SizeAdjust");
            sizeAdjust.Destination          = wrapperGO;
            sizeAdjust.DestinationTop       = headBone.gameObject;
            // Use the model root as DestinationBottom: Mixamo FBX roots sit at ground level,
            // so headBone.y − modelRoot.y = full avatar height, matching the player-side
            // measurement (SourceTop = headTop, SourceBottom = player root at ground).
            // Using hipsBone here gives only head-to-hips (~0.9m) and causes ~2x overscale.
            sizeAdjust.DestinationBottom    = modelGO;
            // Don't adjust at Start — the camera height isn't known yet (known platform issue).
            // Instead, adjust when HMD tracking begins (setHeightOnHMDTracking=true, already
            // the source default). The HMD tracking InputAction (XRI Head/IsTracked) must be
            // assigned manually in the Inspector after running this script.
            sizeAdjust.setHeightOnStart     = false;
            sizeAdjust.setHeightOnHMDTracking = true;
            EditorUtility.SetDirty(sizeAdjust);

            // 7. PlayerRepresentationWirer on the wrapper root — no fields to fill at edit time
            if (wrapperGO.GetComponent<PlayerRepresentationWirer>() == null)
                Undo.AddComponent<PlayerRepresentationWirer>(wrapperGO);

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"[TrolleyAvatarPrefabSetup] Done. '{wrapperGO.name}' is ready.\n" +
                      "Next steps:\n" +
                      "  1. Enter Play Mode briefly to let Animation Rigging bake constraint bindings, " +
                      "then exit Play Mode.\n" +
                      $"  2. Drag '{wrapperGO.name}' from Hierarchy to Project to save as a prefab.\n" +
                      "  3. Assign the prefab as altRepOne or altRepTwo in P_Player_Trolley / P_Self_Player_Trolley.");
        }

        /// <summary>
        /// Creates a Two Bone IK solver child under <paramref name="parent"/>.
        /// Returns the IK Target transform, which SyncSkeletonToVRRig will drive at runtime.
        /// <paramref name="hintOffset"/> is added to the mid-bone world position to place the
        /// pole-vector hint; defaults to Vector3.back * 0.2f (elbows-back for arms).
        /// For legs, pass Vector3.forward * 0.3f (knees-forward).
        /// </summary>
        static Transform CreateTwoBoneIK(Transform parent, string label,
            Transform root, Transform mid, Transform tip,
            Vector3? hintOffset = null)
        {
            var ikGO = new GameObject(label);
            Undo.RegisterCreatedObjectUndo(ikGO, $"Create {label}");
            ikGO.transform.SetParent(parent, false);
            var ik = ikGO.AddComponent<TwoBoneIKConstraint>();

            // Target: starts at the tip (wrist/foot) world position/rotation
            var targetGO = new GameObject($"{label} Target");
            Undo.RegisterCreatedObjectUndo(targetGO, $"Create {label} Target");
            targetGO.transform.SetParent(ikGO.transform, false);
            targetGO.transform.SetPositionAndRotation(tip.position, tip.rotation);

            var hintGO = new GameObject($"{label} Hint");
            Undo.RegisterCreatedObjectUndo(hintGO, $"Create {label} Hint");
            hintGO.transform.SetParent(ikGO.transform, false);
            hintGO.transform.position = mid.position + (hintOffset ?? Vector3.back * 0.2f);

            // Wire the constraint via data struct
            var data = ik.data;
            data.root = root;
            data.mid  = mid;
            data.tip  = tip;
            data.target = targetGO.transform;
            data.hint   = hintGO.transform;
            data.targetPositionWeight = 1f;
            data.targetRotationWeight = 1f;
            data.hintWeight           = 1f;
            data.maintainTargetPositionOffset = false;
            data.maintainTargetRotationOffset = false;
            ik.data = data;
            EditorUtility.SetDirty(ik);

            return targetGO.transform;
        }

        /// <summary>
        /// Adds foot-grounding physics to a leg IK target GO, matching P_Mannequin's setup:
        /// - Rigidbody: gravity on, X/Z position frozen, all rotations frozen — foot can only move vertically.
        /// - BoxCollider: solid shoe-sized box so the floor stops the foot from falling through.
        /// - KeepFeetAboveGround: software clamp so the foot never goes below the avatar root's Y.
        /// </summary>
        static void AddLegPhysics(GameObject targetGO, GameObject rootObject)
        {
            var rb = Undo.AddComponent<Rigidbody>(targetGO);
            rb.mass = 0.0000001f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY
                           | RigidbodyConstraints.FreezeRotationZ;
            EditorUtility.SetDirty(rb);

            var col = Undo.AddComponent<BoxCollider>(targetGO);
            col.size   = new Vector3(0.1f, 0.2f, 0.1f);
            col.center = new Vector3(0f, 0.1f, 0f);
            col.isTrigger = false;
            EditorUtility.SetDirty(col);

            var kfag = Undo.AddComponent<KeepFeetAboveGround>(targetGO);
            kfag.RootObject = rootObject;
            EditorUtility.SetDirty(kfag);
        }

        // ─── Save & Wire ──────────────────────────────────────────────────────────

        /// <summary>
        /// Second step after "Setup Humanoid Avatar Prefab" + Play Mode bake.
        /// Saves the selected GO as a prefab and wires it into P_Self_Player_Trolley
        /// (and P_Player_Trolley if found):
        ///   • Adds the avatar as a nested child (if not already present)
        ///   • Sets PlayerControllerBase.altRepOne → the nested avatar instance
        ///   • Sets localScale = (0.5, 0.5, 0.5)
        ///   • Sets SizeAdjust.SourceTop → RiggingAttachPointHeadTop
        ///   • Sets SizeAdjust.SourceBottom → player root GO
        ///
        /// Still requires manual wiring after:
        ///   • SizeAdjust HMD Tracking Action → "XRI Head/IsTracked"
        ///   • ViewAdjust.viewAdjusted event → SizeAdjust.AdjustHeight
        /// </summary>
        [MenuItem("Trolley/Save and Wire Avatar into Players")]
        static void SaveAndWireAvatarIntoPlayers()
        {
            var wrapperGO = Selection.activeGameObject;
            if (wrapperGO == null || wrapperGO.GetComponentInChildren<SyncSkeletonToVRRig>() == null)
            {
                Debug.LogError("[TrolleyAvatarPrefabSetup] Select a wrapper GO that has already been " +
                               "set up via 'Setup Humanoid Avatar Prefab', then run this.");
                return;
            }

            string prefabPath = $"Assets/Trolley/Prefabs/{wrapperGO.name}.prefab";
            var avatarPrefab = PrefabUtility.SaveAsPrefabAsset(wrapperGO, prefabPath);
            if (avatarPrefab == null)
            {
                Debug.LogError($"[TrolleyAvatarPrefabSetup] Failed to save prefab to {prefabPath}.");
                return;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[TrolleyAvatarPrefabSetup] Saved {wrapperGO.name} → {prefabPath}");

            WireAvatarIntoPlayerPrefab(
                "Assets/Trolley/Prefabs/P_Self_Player_Trolley.prefab",
                avatarPrefab, "altRepOne");

            WireAvatarIntoPlayerPrefab(
                "Assets/Trolley/Prefabs/P_Player_Trolley.prefab",
                avatarPrefab, "altRepOne");

            Debug.Log("[TrolleyAvatarPrefabSetup] Done.\n" +
                      "Remaining manual steps:\n" +
                      "  • SizeAdjust > HMD Tracking Action → 'XRI Head/IsTracked' (in the avatar prefab)\n" +
                      "  • ViewAdjust.viewAdjusted → SizeAdjust.AdjustHeight (in P_Self_Player_Trolley)");
        }

        [MenuItem("Trolley/Save and Wire Avatar into Players", validate = true)]
        static bool ValidateSaveAndWireAvatarIntoPlayers()
        {
            var go = Selection.activeGameObject;
            return go != null && go.GetComponentInChildren<SyncSkeletonToVRRig>() != null;
        }

        /// <summary>
        /// Opens <paramref name="playerPrefabPath"/> in prefab-contents mode, finds or adds
        /// the avatar as a nested child, wires <paramref name="altRepField"/> and SizeAdjust
        /// source references, then saves.
        /// </summary>
        static void WireAvatarIntoPlayerPrefab(string playerPrefabPath,
            GameObject avatarPrefab, string altRepField)
        {
            if (!System.IO.File.Exists(playerPrefabPath))
            {
                Debug.Log($"[TrolleyAvatarPrefabSetup] {playerPrefabPath} not found — skipping.");
                return;
            }

            var playerContents = PrefabUtility.LoadPrefabContents(playerPrefabPath);
            try
            {
                // Find existing nested instance by name (avatar prefab names are unique)
                var avatarInstance = playerContents.transform
                    .Cast<Transform>()
                    .Select(t => t.gameObject)
                    .FirstOrDefault(go => go.name == avatarPrefab.name);

                if (avatarInstance == null)
                {
                    avatarInstance = PrefabUtility.InstantiatePrefab(
                        avatarPrefab, playerContents.transform) as GameObject;
                    avatarInstance.SetActive(false);
                    Debug.Log($"[TrolleyAvatarPrefabSetup] Added {avatarPrefab.name} as nested child in {playerPrefabPath}.");
                }

                // Scale — matches the SizeAdjust nativeSize baseline (Remy ~2× too large at scale 1)
                avatarInstance.transform.localPosition = Vector3.zero;
                avatarInstance.transform.localScale    = Vector3.one * 0.5f;

                // altRepOne / altRepTwo on PlayerControllerBase
                var controller = playerContents.GetComponent<PlayerControllerBase>()
                               ?? playerContents.GetComponentInChildren<PlayerControllerBase>(true);
                if (controller != null)
                {
                    var cso = new SerializedObject(controller);
                    cso.FindProperty(altRepField).objectReferenceValue = avatarInstance;
                    cso.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogWarning($"[TrolleyAvatarPrefabSetup] No PlayerControllerBase found in {playerPrefabPath}.");
                }

                // SizeAdjust source references
                var sizeAdjust = avatarInstance.GetComponent<SizeAdjust>();
                if (sizeAdjust != null)
                {
                    var headTop = FindInHierarchy(playerContents.transform, "RiggingAttachPointHeadTop");
                    var sso = new SerializedObject(sizeAdjust);
                    sso.FindProperty("SourceTop").objectReferenceValue    = headTop;
                    sso.FindProperty("SourceBottom").objectReferenceValue = playerContents;
                    sso.ApplyModifiedPropertiesWithoutUndo();

                    if (headTop == null)
                        Debug.LogWarning($"[TrolleyAvatarPrefabSetup] RiggingAttachPointHeadTop not found in {playerPrefabPath} — SourceTop left null.");
                }

                PrefabUtility.SaveAsPrefabAsset(playerContents, playerPrefabPath);
                Debug.Log($"[TrolleyAvatarPrefabSetup] Wired into {playerPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerContents);
            }
        }

        static GameObject FindInHierarchy(Transform root, string name)
        {
            if (root.name == name) return root.gameObject;
            foreach (Transform child in root)
            {
                var found = FindInHierarchy(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ─── Validate ─────────────────────────────────────────────────────────────

        [MenuItem("Trolley/Setup Humanoid Avatar Prefab", validate = true)]
        static bool ValidateSetupHumanoidAvatarPrefab()
        {
            return Selection.activeGameObject != null;
        }
    }
}
