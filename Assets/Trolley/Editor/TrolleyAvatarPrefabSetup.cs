using UnityEngine;
using UnityEditor;
using UnityEngine.Animations.Rigging;
using VRT.Pilots.Common;

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
    ///   - Two Bone IK constraints for arms
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

            // 3. Two Bone IK for each arm (children of VR Constraints)
            var leftArmTarget  = CreateTwoBoneIK(vrConstraintsGO.transform, "Left Arm IK",
                                     leftUpperArm, leftLowerArm, leftHandBone);
            var rightArmTarget = CreateTwoBoneIK(vrConstraintsGO.transform, "Right Arm IK",
                                     rightUpperArm, rightLowerArm, rightHandBone);

            // 4. Register rig layer in RigBuilder
            rigBuilder.layers.Add(new RigLayer(rig, true));
            EditorUtility.SetDirty(rigBuilder);

            // 5. SyncSkeletonToVRRig as a child of the wrapper (sibling of the model GO,
            //    matching P_Mannequin's layout).
            //    rigTargets → skeleton bones / IK targets.
            //    vrTargets left null — PlayerRepresentationWirer fills them at runtime.
            var syncGO = new GameObject("SyncSkeletonToVRRig");
            Undo.RegisterCreatedObjectUndo(syncGO, "Create SyncSkeletonToVRRig");
            syncGO.transform.SetParent(wrapperGO.transform, false);
            var sync = syncGO.AddComponent<SyncSkeletonToVRRig>();

            // VRMap fields are class references — null on a fresh component; initialize before use
            sync.head      = new SyncSkeletonToVRRig.VRMap();
            sync.neck      = new SyncSkeletonToVRRig.VRMap();
            sync.leftHand  = new SyncSkeletonToVRRig.VRMap();
            sync.rightHand = new SyncSkeletonToVRRig.VRMap();

            sync.head.rigTarget = headBone;

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
            sizeAdjust.Destination       = wrapperGO;
            sizeAdjust.DestinationTop    = headBone.gameObject;
            sizeAdjust.DestinationBottom = hipsBone.gameObject;
            sizeAdjust.setHeightOnStart  = true;
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
        /// </summary>
        static Transform CreateTwoBoneIK(Transform parent, string label,
            Transform root, Transform mid, Transform tip)
        {
            var ikGO = new GameObject(label);
            Undo.RegisterCreatedObjectUndo(ikGO, $"Create {label}");
            ikGO.transform.SetParent(parent, false);
            var ik = ikGO.AddComponent<TwoBoneIKConstraint>();

            // Target: starts at the tip (wrist) world position/rotation
            var targetGO = new GameObject($"{label} Target");
            Undo.RegisterCreatedObjectUndo(targetGO, $"Create {label} Target");
            targetGO.transform.SetParent(ikGO.transform, false);
            targetGO.transform.SetPositionAndRotation(tip.position, tip.rotation);

            // Hint: elbow hint pushed back (-Z world) so the arm bends naturally
            var hintGO = new GameObject($"{label} Hint");
            Undo.RegisterCreatedObjectUndo(hintGO, $"Create {label} Hint");
            hintGO.transform.SetParent(ikGO.transform, false);
            hintGO.transform.position = mid.position + Vector3.back * 0.2f;

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

        [MenuItem("Trolley/Setup Humanoid Avatar Prefab", validate = true)]
        static bool ValidateSetupHumanoidAvatarPrefab()
        {
            return Selection.activeGameObject != null;
        }
    }
}
