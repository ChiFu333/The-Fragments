#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Services.FMODAudioSystem.Editor
{
    [CustomEditor(typeof(FMODAudioSystem.FMODAudioTester))]
    public class FMODAudioTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var t = (FMODAudioSystem.FMODAudioTester)target;

            GUILayout.Space(8);
            EditorGUILayout.LabelField("One-Shot", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play OneShot")) t.PlayOneShot();
            if (GUILayout.Button("Play OneShot Attached")) t.PlayOneShotAttached();
            if (GUILayout.Button("OneShot Cooldown")) t.PlayOneShotWithCooldown();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Looping", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Loop")) t.PlayLoop();
            if (GUILayout.Button("Stop Loop")) t.StopLoop();
            if (GUILayout.Button("Play If Under Limit")) t.PlayIfUnderLimit();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Dynamic Load", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preload")) t.Preload();
            if (GUILayout.Button("EnsureLoaded")) t.EnsureLoaded();
            if (GUILayout.Button("Unload")) t.Unload();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Music", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Music")) t.PlayMusic();
            if (GUILayout.Button("Start Playlist")) t.StartPlaylist();
            if (GUILayout.Button("Stop Playlist")) t.StopPlaylist();
            if (GUILayout.Button("Next")) t.NextTrack();
            if (GUILayout.Button("Prev")) t.PreviousTrack();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(t.PlaylistAsset == null);
            if (GUILayout.Button("Start Playlist Asset")) t.StartPlaylistAsset();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Snapshots", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Snapshot")) t.StartSnapshot();
            if (GUILayout.Button("Stop Snapshot")) t.StopSnapshot();
            if (GUILayout.Button("Push Snapshot")) _ = t.PushSnapshotAsync();
            if (GUILayout.Button("Pop Snapshot")) _ = t.PopSnapshotAsync();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ramp Global")) _ = t.RampGlobalParameter();
            if (GUILayout.Button("Ramp Event")) _ = t.RampEventParameter();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Buses", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Volume")) t.SetBusVolume();
            if (GUILayout.Button("Fade Volume")) _ = t.FadeBus();
            if (GUILayout.Button("Duck")) _ = t.DuckBus();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop All")) t.StopAll();
            if (GUILayout.Button("Pause All")) t.PauseAll();
            if (GUILayout.Button("Resume All")) t.ResumeAll();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Banks", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Banks")) _ = t.LoadBanksAsync();
            if (GUILayout.Button("Unload Bank")) t.UnloadBank();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Register Tag")) t.RegisterLoopTag();
            if (GUILayout.Button("Unregister Tag")) t.UnregisterLoopTag();
            if (GUILayout.Button("Stop By Tag")) t.StopByTag();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
