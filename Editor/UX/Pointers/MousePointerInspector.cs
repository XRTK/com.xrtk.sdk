// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEditor;
using UnityEngine;
using XRTK.Editor.Extensions;
using XRTK.SDK.UX.Pointers;

namespace XRTK.SDK.Editor.UX.Pointers
{
    [CustomEditor(typeof(MousePointer))]
    public class MousePointerInspector : BaseControllerPointerInspector
    {
        private readonly GUIContent mousePointerFoldoutContent = new GUIContent("Mouse Pointer Settings");

        private SerializedProperty hideCursorWhenInactive;
        private SerializedProperty hideTimeout;
        private SerializedProperty movementThresholdToUnHide;
        private SerializedProperty speed;

        protected override void OnEnable()
        {
            DrawBasePointerActions = false;
            base.OnEnable();

            hideCursorWhenInactive = serializedObject.FindProperty(nameof(hideCursorWhenInactive));
            movementThresholdToUnHide = serializedObject.FindProperty(nameof(movementThresholdToUnHide));
            hideTimeout = serializedObject.FindProperty(nameof(hideTimeout));
            speed = serializedObject.FindProperty(nameof(speed));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (hideCursorWhenInactive.FoldoutWithBoldLabelPropertyField(mousePointerFoldoutContent))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hideCursorWhenInactive);

                if (hideCursorWhenInactive.boolValue)
                {
                    EditorGUILayout.PropertyField(hideTimeout);
                    EditorGUILayout.PropertyField(movementThresholdToUnHide);
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.PropertyField(speed);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}