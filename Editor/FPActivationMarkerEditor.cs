namespace FuzzPhyte.Utility.Animation.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(FuzzPhyte.Utility.Animation.Timeline.FPActivationMarker))]
    public class FPActivationMarkerEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Command"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseTrackBindingTarget"));

            var useBinding = serializedObject.FindProperty("UseTrackBindingTarget").boolValue;
            if (!useBinding)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideTarget"));
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Track binding targeting is best handled per-track. For per-marker routing, disable this and set OverrideTarget.",
                    MessageType.Info
                );
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Sticky"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ApplyDuringEvaluate"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
