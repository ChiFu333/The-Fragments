#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Services.FMODAudioSystem.Editor
{
    [CustomEditor(typeof(Services.FMODAudioSystem.FMODAudioSettingsAsset))]
    public class FMODAudioSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var settings = (Services.FMODAudioSystem.FMODAudioSettingsAsset)target;
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

            if (GUILayout.Button("Create AudioManager in Scene"))
            {
                CreateAudioManager(settings);
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Quick Create Services", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (settings.MusicServiceAsset == null && GUILayout.Button("Music Service"))
                {
                    settings.MusicServiceAsset = CreateAsset<Services.FMODAudioSystem.FMODMusicServiceAsset>("FMODMusicServiceAsset");
                }
                if (settings.EventServiceAsset == null && GUILayout.Button("Event Service"))
                {
                    settings.EventServiceAsset = CreateAsset<Services.FMODAudioSystem.FMODEventServiceAsset>("FMODEventServiceAsset");
                }
                if (settings.BusServiceAsset == null && GUILayout.Button("Bus Service"))
                {
                    settings.BusServiceAsset = CreateAsset<Services.FMODAudioSystem.FMODBusServiceAsset>("FMODBusServiceAsset");
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (settings.ParameterServiceAsset == null && GUILayout.Button("Parameter Service"))
                {
                    settings.ParameterServiceAsset = CreateAsset<Services.FMODAudioSystem.FMODParameterServiceAsset>("FMODParameterServiceAsset");
                }
                if (settings.BankServiceAsset == null && GUILayout.Button("Bank Service"))
                {
                    settings.BankServiceAsset = CreateAsset<Services.FMODAudioSystem.FMODBankServiceAsset>("FMODBankServiceAsset");
                }
                if (settings.SnapshotServiceAsset == null && GUILayout.Button("Snapshot Service"))
                {
                    settings.SnapshotServiceAsset = CreateAsset<Services.FMODAudioSystem.FMODSnapshotServiceAsset>("FMODSnapshotServiceAsset");
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (settings.TagServiceAsset == null && GUILayout.Button("Tag Service"))
                {
                    settings.TagServiceAsset = CreateAsset<Services.FMODAudioSystem.FMODTagServiceAsset>("FMODTagServiceAsset");
                }
            }
            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
            }
        }

        private static void CreateAudioManager(Services.FMODAudioSystem.FMODAudioSettingsAsset settingsAsset)
        {
            var go = new GameObject("FMOD AudioManager");
            var mgr = go.AddComponent<Services.FMODAudioSystem.FMODAudioManager>();

            var so = new SerializedObject(mgr);
            so.Update();
            so.FindProperty("settingsAsset").objectReferenceValue = settingsAsset;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = go;
        }

        private static T CreateAsset<T>(string defaultName) where T : ScriptableObject
        {
            var path = EditorUtility.SaveFilePanelInProject("Create " + typeof(T).Name, defaultName, "asset", "Select location");
            if (string.IsNullOrEmpty(path)) return null;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }
    }
}
#endif
