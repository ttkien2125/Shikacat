using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        _ = DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f); // Coral Red
        if (GUILayout.Button("🔄 Reset Progress to Level 1", GUILayout.Height(32)))
        {
            GameManager gm = (GameManager)target;
            gm.ResetLevelProgress();
        }
        GUI.backgroundColor = prevColor;
    }
}

