// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;
using XRTK.Editor.Extensions;
using XRTK.SDK.UX.Pointers;

namespace XRTK.SDK.Editor.UX.Pointers
{
    [CustomEditor(typeof(HandSpatialPointer))]
    public class HandSpatialPointerInspector : LinePointerInspector
    {
        private SerializedProperty pointerPoseTransform;
        private SerializedProperty offsetStart;
        private SerializedProperty offsetEnd;
        private static readonly GUIContent foldoutHeader = new GUIContent("Spatial Pointer Settings");

        protected override void OnEnable()
        {
            base.OnEnable();

            pointerPoseTransform = serializedObject.FindProperty(nameof(pointerPoseTransform));
            offsetStart = serializedObject.FindProperty(nameof(offsetStart));
            offsetEnd = serializedObject.FindProperty(nameof(offsetEnd));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            pointerPoseTransform.isExpanded = EditorGUILayoutExtensions.FoldoutWithBoldLabel(pointerPoseTransform.isExpanded, foldoutHeader);
            if (pointerPoseTransform.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(pointerPoseTransform);
                EditorGUILayout.PropertyField(offsetStart);
                EditorGUILayout.PropertyField(offsetEnd);

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}