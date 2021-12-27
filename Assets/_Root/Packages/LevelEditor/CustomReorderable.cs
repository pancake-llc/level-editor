using System;

namespace Snorlax.Editor
{
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    public class CustomReorderable
    {
        private static class Style
        {
            public static readonly GUIContent AddContent;
            public static readonly GUIStyle AddStyle;
            public static readonly GUIContent SubContent;
            public static readonly GUIStyle SubStyle;

            static Style()
            {
                AddContent = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to list");
                AddStyle = "RL FooterButton";
                SubContent = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove selection from list");
                SubStyle = "RL FooterButton";
            }
        }

        private readonly ReorderableList _reorderableList;

        public CustomReorderable(SerializedProperty property, Action<int> actionUpdateNumberElement = null, CreateButtonSearchPathDelegate actionCreateButtonSearchPath = null)
        {
            _reorderableList = CreateInstance(property, actionUpdateNumberElement, actionCreateButtonSearchPath);
        }

        public void Draw() { _reorderableList.DoLayoutList(); }

        private ReorderableList CreateInstance(SerializedProperty property, Action<int> actionAddElement, CreateButtonSearchPathDelegate actionCreateButtonSearchPath)
        {
            return new ReorderableList(property.serializedObject,
                property,
                true,
                true,
                false,
                false)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, $"{property.displayName}: {property.arraySize}", EditorStyles.boldLabel);
                    var position = new Rect(rect.width - Math.Max(EditorGUI.indentLevel - property.depth, 1) * 15f, rect.y, 20f, 13f);
                    if (GUI.Button(position, Style.AddContent, Style.AddStyle))
                    {
                        property.serializedObject.UpdateIfRequiredOrScript();
                        property.InsertArrayElementAtIndex(property.arraySize);
                        property.serializedObject.ApplyModifiedProperties();
                        actionAddElement?.Invoke(property.arraySize);
                        // save again
                    }
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    if (property.arraySize <= index)
                        return;

                    DrawElement(property, rect, index);

                    rect.xMin = rect.width - (EditorGUI.indentLevel - property.depth) * 15f;
                    rect.width = 20f;
                    if (GUI.Button(rect, Style.SubContent, Style.SubStyle))
                    {
                        property.serializedObject.UpdateIfRequiredOrScript();
                        property.DeleteArrayElementAtIndex(index);
                        property.serializedObject.ApplyModifiedProperties();
                        actionAddElement?.Invoke(property.arraySize);
                        // save again
                    }

                    // todo
                    rect.xMin -= 20;
                    rect.width = 20f;
                    actionCreateButtonSearchPath?.Invoke(rect, ref property, index);
                },
                drawFooterCallback = _ => { },
                footerHeight = 0f,
                elementHeightCallback = index =>
                {
                    if (property.arraySize <= index)
                        return 0;

                    return EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(index));
                }
            };
        }

        private void DrawElement(SerializedProperty property, Rect rect, int index)
        {
            var indexName = index.ToString();

            rect.x += 5f;
            rect.width -= 50f;
            var elementProperty = property.GetArrayElementAtIndex(index);
            if (elementProperty.propertyType != SerializedPropertyType.Generic)
            {
                EditorGUI.PropertyField(rect, elementProperty, new GUIContent(indexName));
                return;
            }

            rect.x += 10f;
            rect.width -= 20f;
            rect.height = EditorGUIUtility.singleLineHeight;

            elementProperty.isExpanded = EditorGUI.Foldout(rect, elementProperty.isExpanded, new GUIContent(indexName));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (!elementProperty.isExpanded)
                return;

            var depth = -1;
            while (elementProperty.NextVisible(true) || depth == -1)
            {
                if (depth != -1 && elementProperty.depth != depth)
                    break;
                depth = elementProperty.depth;
                rect.height = EditorGUI.GetPropertyHeight(elementProperty);
                EditorGUI.PropertyField(rect, elementProperty, true);
                rect.y += rect.height;
            }
        }
    }
}