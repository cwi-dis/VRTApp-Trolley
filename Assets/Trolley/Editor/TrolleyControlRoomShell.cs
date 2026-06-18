using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Builds a simple enclosing shell (ceiling + 4 walls) around the existing control-room console,
    /// for scenes where the cabin prefab's own outer shell was disabled but the console parts are kept.
    /// The floor is left to the scene's existing "Floor".
    ///
    ///   Trolley > Build Control Room Shell
    ///
    /// Creates a "ControlRoomShell" parent with named wall children (Ceiling, Wall_Back/Front/Left/Right)
    /// that you can nudge, scale, or disable individually in the Inspector — e.g. disable Wall_Front if it
    /// sits in front of the monitors. Each wall is a thin box rendered from the room side, so it shows
    /// correctly from inside. Colliders are stripped so they don't block gaze/ray interactions.
    ///
    /// Operates on the OPEN scene and does NOT save — review, then save yourself. Fully undoable (Ctrl+Z).
    /// Tune the constants below and re-run, or just move the wall children afterwards.
    /// </summary>
    public static class TrolleyControlRoomShell
    {
        // Inner room volume in metres: width(x), height(y), depth(z), and where its centre sits.
        static readonly Vector3 Center = new Vector3(0f, 1.75f, 1.5f);
        static readonly Vector3 Inner  = new Vector3(6f, 3.5f, 7f);
        const float Thickness = 0.2f;
        // No floor is built — the scene already has its own "Floor".

        [MenuItem("Trolley/Build Control Room Shell")]
        public static void Build()
        {
            var existing = GameObject.Find("ControlRoomShell");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Control Room Shell",
                        "A 'ControlRoomShell' already exists in this scene. Replace it?", "Replace", "Cancel"))
                    return;
                Undo.DestroyObjectImmediate(existing);
            }

            var mat = FindRoomMaterial();

            var root = new GameObject("ControlRoomShell");
            Undo.RegisterCreatedObjectUndo(root, "Build Control Room Shell");

            float hw = Inner.x * 0.5f, hh = Inner.y * 0.5f, hd = Inner.z * 0.5f;
            float top = Center.y + hh;
            float outerW = Inner.x + Thickness * 2f, outerD = Inner.z + Thickness * 2f;

            AddBox(root, mat, "Ceiling",
                   new Vector3(Center.x, top + Thickness * 0.5f, Center.z),
                   new Vector3(outerW, Thickness, outerD));

            AddBox(root, mat, "Wall_Back",
                   new Vector3(Center.x, Center.y, Center.z - hd - Thickness * 0.5f),
                   new Vector3(outerW, Inner.y, Thickness));
            AddBox(root, mat, "Wall_Front",
                   new Vector3(Center.x, Center.y, Center.z + hd + Thickness * 0.5f),
                   new Vector3(outerW, Inner.y, Thickness));
            AddBox(root, mat, "Wall_Left",
                   new Vector3(Center.x - hw - Thickness * 0.5f, Center.y, Center.z),
                   new Vector3(Thickness, Inner.y, Inner.z));
            AddBox(root, mat, "Wall_Right",
                   new Vector3(Center.x + hw + Thickness * 0.5f, Center.y, Center.z),
                   new Vector3(Thickness, Inner.y, Inner.z));

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("Build Control Room Shell: created Ceiling + 4 walls under 'ControlRoomShell' " +
                      $"(inner {Inner.x}×{Inner.y}×{Inner.z} m, centre {Center}). " +
                      "Nudge/scale/disable any face in the Inspector (e.g. disable Wall_Front if it covers " +
                      "the monitors). Save the scene when happy; Ctrl+Z removes it.");
        }

        static void AddBox(GameObject parent, Material mat, string name, Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = size;

            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col); // visual only — don't block gaze/ray casts

            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            Undo.RegisterCreatedObjectUndo(go, "Build Control Room Shell");
        }

        // Reuse the scene Floor's material so the shell matches; fall back to a neutral grey.
        static Material FindRoomMaterial()
        {
            var floor = GameObject.Find("Floor");
            if (floor != null)
            {
                var r = floor.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null) return r.sharedMaterial;
            }
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = "M_ControlRoomShell" };
            if (mat.HasProperty("_Color")) mat.color = new Color(0.55f, 0.55f, 0.58f);
            return mat;
        }
    }
}
