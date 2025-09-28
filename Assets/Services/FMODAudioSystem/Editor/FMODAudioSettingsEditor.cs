#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Services.FMODAudioSystem.Editor
{
    [CustomEditor(typeof(Services.FMODAudioSystem.FMODAudioSettings))]
    public class FMODAudioSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var settings = (Services.FMODAudioSystem.FMODAudioSettings)target;
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

            if (GUILayout.Button("Create AudioManager in Scene"))
            {
                CreateAudioManager(settings);
            }
        }

        private static void CreateAudioManager(Services.FMODAudioSystem.FMODAudioSettings settings)
        {
            var go = new GameObject("FMOD AudioManager");
            var mgr = go.AddComponent<Services.FMODAudioSystem.FMODAudioManager>();

            var so = new SerializedObject(mgr);
            so.Update();
            so.FindProperty("_settings").objectReferenceValue = settings;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = go;
        }
    }
}
#endif
