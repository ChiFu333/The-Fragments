#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Services.FMODAudioSystem.Editor
{
    public static class FMODAudioTools
    {
        [MenuItem("Tools/Audio/FMOD/Create Settings Asset (Resources\\FMOD)")]
        public static void CreateSettingsAsset()
        {
            const string folder = "Assets/Resources/FMOD";
            const string assetPath = folder + "/FMODAudioSettings.asset";

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "FMOD");
            }

            var settings = ScriptableObject.CreateInstance<FMODAudioSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
            Debug.Log("Created FMODAudioSettings at " + assetPath);
        }
    }
}
#endif
