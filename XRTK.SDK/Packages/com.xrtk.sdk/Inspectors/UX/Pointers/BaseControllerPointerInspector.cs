// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEditor;
using XRTK.SDK.Inspectors.Input.Handlers;
using XRTK.SDK.UX.Pointers;

namespace XRTK.SDK.Inspectors.UX.Pointers
{
    [CustomEditor(typeof(BaseControllerPointer))]
    public class BaseControllerPointerInspector : ControllerPoseSynchronizerInspector
    {
        private SerializedProperty cursorPrefab;
        private SerializedProperty disableCursorOnStart;
        private SerializedProperty setCursorVisibilityOnSourceDetected;
        private SerializedProperty raycastOrigin;
        private SerializedProperty pointerExtent;
        private SerializedProperty selectActions;
        private SerializedProperty selectThreshold;
        private SerializedProperty holdActions;
        private SerializedProperty holdThreshold;
        private SerializedProperty pointerOrientation;
        private SerializedProperty requiresHoldAction;
        private SerializedProperty enablePointerOnStart;
        private SerializedProperty isTargetPositionLockedOnFocusLock;

        private bool basePointerFoldout = true;

        protected bool DrawBasePointerActions = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            cursorPrefab = serializedObject.FindProperty("cursorPrefab");
            disableCursorOnStart = serializedObject.FindProperty("disableCursorOnStart");
            setCursorVisibilityOnSourceDetected = serializedObject.FindProperty("setCursorVisibilityOnSourceDetected");
            raycastOrigin = serializedObject.FindProperty("raycastOrigin");
            pointerExtent = serializedObject.FindProperty("defaultPointerExtent");
            selectActions = serializedObject.FindProperty("selectActions");
            selectThreshold = serializedObject.FindProperty("selectThreshold");
            holdActions = serializedObject.FindProperty("holdActions");
            holdThreshold = serializedObject.FindProperty("holdThreshold");
            pointerOrientation = serializedObject.FindProperty("pointerOrientation");
            requiresHoldAction = serializedObject.FindProperty("requiresHoldAction");
            enablePointerOnStart = serializedObject.FindProperty("enablePointerOnStart");
            isTargetPositionLockedOnFocusLock = serializedObject.FindProperty("isTargetPositionLockedOnFocusLock");

            DrawHandednessProperty = false;
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            basePointerFoldout = EditorGUILayout.Foldout(basePointerFoldout, "Base Pointer Settings", true);

            if (basePointerFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(cursorPrefab);
                EditorGUILayout.PropertyField(disableCursorOnStart);
                EditorGUILayout.PropertyField(setCursorVisibilityOnSourceDetected);
                EditorGUILayout.PropertyField(enablePointerOnStart);
                EditorGUILayout.PropertyField(isTargetPositionLockedOnFocusLock);
                EditorGUILayout.PropertyField(raycastOrigin);
                EditorGUILayout.PropertyField(pointerExtent);
                EditorGUILayout.PropertyField(pointerOrientation);
                EditorGUILayout.PropertyField(selectActions, true);
                EditorGUILayout.PropertyField(selectThreshold);

                if (DrawBasePointerActions)
                {
                    EditorGUILayout.PropertyField(requiresHoldAction);

                    if (requiresHoldAction.boolValue)
                    {
                        EditorGUILayout.PropertyField(holdActions, true);
                        EditorGUILayout.PropertyField(holdThreshold);
                    }
                }

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}