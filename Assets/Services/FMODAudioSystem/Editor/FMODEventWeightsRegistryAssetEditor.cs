#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Services.FMODAudioSystem.Editor
{
    [CustomEditor(typeof(FMODEventWeightsRegistryAsset))]
    public class FMODEventWeightsRegistryAssetEditor : UnityEditor.Editor
    {
        private ReorderableList _entriesList;

        private void OnEnable()
        {
            var so = serializedObject;
            var entriesProp = so.FindProperty("Entries");
            _entriesList = new ReorderableList(so, entriesProp, true, true, true, true);
            _entriesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Event Weights");
            _entriesList.elementHeightCallback = index =>
            {
                var el = entriesProp.GetArrayElementAtIndex(index);
                var evProp = el.FindPropertyRelative("Event");
                var weightProp = el.FindPropertyRelative("Weight");
                float h = 2f;
                h += EditorGUI.GetPropertyHeight(evProp, true) + 2f;
                h += EditorGUI.GetPropertyHeight(weightProp, true) + 2f;
                return h;
            };
            _entriesList.drawElementCallback = (rect, index, active, focused) =>
            {
                var el = entriesProp.GetArrayElementAtIndex(index);
                var evProp = el.FindPropertyRelative("Event");
                var weightProp = el.FindPropertyRelative("Weight");
                var r = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUI.GetPropertyHeight(evProp, true));
                EditorGUI.PropertyField(r, evProp);
                r.y += r.height + 2f;
                r.height = EditorGUI.GetPropertyHeight(weightProp, true);
                EditorGUI.PropertyField(r, weightProp);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _entriesList.DoLayoutList();
            EditorGUILayout.HelpBox("Events not listed are treated as weight = 0.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
