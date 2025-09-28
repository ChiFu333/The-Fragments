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

            var settings = ScriptableObject.CreateInstance<FMODAudioSettingsAsset>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
            Debug.Log("Created FMODAudioSettings at " + assetPath);
        }

        [MenuItem("Tools/Audio/FMOD/Create Event Sequence Asset")]
        public static void CreateEventSequenceAsset()
        {
            var folder = GetSelectedFolder();
            var asset = ScriptableObject.CreateInstance<FMODEventSequenceAsset>();
            var path = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(folder, "NewFMODEventSequence.asset"));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("Created FMODEventSequence at " + path);
        }

        [MenuItem("Tools/Audio/FMOD/Create Event Container Asset")]
        public static void CreateEventContainerAsset()
        {
            var folder = GetSelectedFolder();
            var asset = ScriptableObject.CreateInstance<FMODEventContainerAsset>();
            var path = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(folder, "NewFMODEventContainer.asset"));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("Created FMODEventContainerAsset at " + path);
        }

        [MenuItem("Tools/Audio/FMOD/Create Event Weights Registry Asset")]
        public static void CreateWeightsRegistryAsset()
        {
            var folder = GetSelectedFolder();
            var asset = ScriptableObject.CreateInstance<FMODEventWeightsRegistryAsset>();
            var path = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(folder, "FMODEventWeightsRegistry.asset"));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("Created FMODEventWeightsRegistryAsset at " + path);
        }

        [MenuItem("Tools/Audio/FMOD/Create Weighted Shuffle Policy Asset")]
        public static void CreateWeightedShufflePolicyAsset()
        {
            var folder = GetSelectedFolder();
            var asset = ScriptableObject.CreateInstance<FMODWeightedEventShufflePolicyAsset>();
            var path = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(folder, "FMODWeightedEventShufflePolicy.asset"));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("Created FMODWeightedEventShufflePolicyAsset at " + path);
        }

        [MenuItem("Tools/Audio/FMOD/Create Tag Service Asset")]
        public static void CreateTagServiceAsset()
        {
            var folder = GetSelectedFolder();
            var asset = ScriptableObject.CreateInstance<FMODTagServiceAsset>();
            var path = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(folder, "FMODTagServiceAsset.asset"));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("Created FMODTagServiceAsset at " + path);
        }

        private static string GetSelectedFolder()
        {
            var obj = Selection.activeObject;
            var path = obj != null ? AssetDatabase.GetAssetPath(obj) : "Assets";
            if (string.IsNullOrEmpty(path)) return "Assets";
            if (System.IO.Directory.Exists(path)) return path;
            var dir = System.IO.Path.GetDirectoryName(path);
            return string.IsNullOrEmpty(dir) ? "Assets" : dir.Replace("\\", "/");
        }
    }
}
#endif
