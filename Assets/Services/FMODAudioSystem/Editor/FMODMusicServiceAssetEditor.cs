#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Services.FMODAudioSystem.Editor
{
    [CustomEditor(typeof(FMODMusicServiceAsset))]
    public class FMODMusicServiceAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var policyProp = serializedObject.FindProperty("ShufflePolicyAsset");

            EditorGUILayout.PropertyField(policyProp, new GUIContent("Shuffle Policy Asset"));

            if (policyProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No shuffle policy assigned. Playlists will use sequential or unweighted shuffle only.", MessageType.Info);
                if (GUILayout.Button("Create Weighted Shuffle Policy"))
                {
                    var asset = ScriptableObject.CreateInstance<FMODWeightedEventShufflePolicyAsset>();
                    var path = EditorUtility.SaveFilePanelInProject("Create Weighted Shuffle Policy", "FMODWeightedEventShufflePolicy", "asset", "Select location");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        policyProp.objectReferenceValue = asset;
                        EditorGUIUtility.PingObject(asset);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
