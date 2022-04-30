using UnityEditor;
using UnityEngine;

namespace ModSDK.editor.gui
{
    [CustomPropertyDrawer(typeof(ScriptableObject), true)]
    public class ScriptableObjectDrawer : PropertyDrawer
    {
        // Cached scriptable object editor
        private Editor editor = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);

            if (property.objectReferenceValue != null)
            {
                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
            }

            // Draw foldout properties
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                if (!editor)
                    Editor.CreateCachedEditor(property.objectReferenceValue, null, ref editor);

                // Draw object properties
                if (editor) // catches empty property
                    editor.OnInspectorGUI();

                EditorGUI.indentLevel--;
            }
        }
    }
}