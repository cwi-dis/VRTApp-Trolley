using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ManagedBySetupScript))]
public class ManagedBySetupScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var t = (ManagedBySetupScript)target;
        EditorGUILayout.HelpBox(
            $"This GameObject is managed by a setup script.\n" +
            $"To modify it, re-run:  {t.menuItem}",
            MessageType.Warning);
    }
}
