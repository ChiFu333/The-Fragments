#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Services.FMODAudioSystem.Editor
{
    [CustomEditor(typeof(FMODTagServiceAsset))]
    public class FMODTagServiceAssetEditor : UnityEditor.Editor
    {
        private ReorderableList _templatesList;

        private void OnEnable()
        {
            var so = serializedObject;
            var templatesProp = so.FindProperty("Templates");
            _templatesList = new ReorderableList(so, templatesProp, true, true, true, true);
            _templatesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Tag Templates");
            _templatesList.elementHeightCallback = index =>
            {
                var el = templatesProp.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(el, true) + 6f;
            };
            _templatesList.drawElementCallback = (rect, index, active, focused) =>
            {
                var el = templatesProp.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUI.GetPropertyHeight(el, true);
                EditorGUI.PropertyField(rect, el, GUIContent.none, true);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _templatesList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
