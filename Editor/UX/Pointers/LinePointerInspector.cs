// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEditor;
using UnityEngine;
using XRTK.Editor.Extensions;
using XRTK.SDK.UX.Pointers;

namespace XRTK.SDK.Editor.UX.Pointers
{
    [CustomEditor(typeof(LinePointer))]
    public class LinePointerInspector : BaseControllerPointerInspector
    {
        private const int MAX_RECOMMENDED_LINECAST_RESOLUTION = 20;
        private readonly GUIContent foldoutContent = new GUIContent("Line Pointer Settings");

        private SerializedProperty lineColorSelected;
        private SerializedProperty lineColorValid;
        private SerializedProperty lineColorInvalid;
        private SerializedProperty lineColorNoTarget;
        private SerializedProperty lineColorLockFocus;
        private SerializedProperty lineCastResolution;
        private SerializedProperty lineRenderers;

        protected override void OnEnable()
        {
            base.OnEnable();

            lineColorSelected = serializedObject.FindProperty(nameof(lineColorSelected));
            lineColorValid = serializedObject.FindProperty(nameof(lineColorValid));
            lineColorInvalid = serializedObject.FindProperty(nameof(lineColorInvalid));
            lineColorNoTarget = serializedObject.FindProperty(nameof(lineColorNoTarget));
            lineColorLockFocus = serializedObject.FindProperty(nameof(lineColorLockFocus));
            lineCastResolution = serializedObject.FindProperty(nameof(lineCastResolution));
            lineRenderers = serializedObject.FindProperty(nameof(lineRenderers));
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            lineCastResolution.isExpanded = EditorGUILayoutExtensions.FoldoutWithBoldLabel(lineCastResolution.isExpanded, foldoutContent);

            if (lineCastResolution.isExpanded)
            {
                EditorGUI.indentLevel++;

                int lineCastResolutionValue = lineCastResolution.intValue;

                if (lineCastResolutionValue > MAX_RECOMMENDED_LINECAST_RESOLUTION)
                {
                    EditorGUILayout.LabelField($"Note: values above {MAX_RECOMMENDED_LINECAST_RESOLUTION} should only be used when your line is expected to be highly non-uniform.", EditorStyles.miniLabel);
                }

                EditorGUILayout.PropertyField(lineCastResolution);
                EditorGUILayout.PropertyField(lineColorSelected);
                EditorGUILayout.PropertyField(lineColorValid);
                EditorGUILayout.PropertyField(lineColorInvalid);
                EditorGUILayout.PropertyField(lineColorNoTarget);
                EditorGUILayout.PropertyField(lineColorLockFocus);
                EditorGUILayout.PropertyField(lineRenderers, true);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}