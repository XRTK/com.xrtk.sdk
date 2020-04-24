// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEditor;
using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.Inspectors.Extensions;
using XRTK.SDK.Input.Handlers;

namespace XRTK.SDK.Inspectors.Input.Handlers
{
    [CustomEditor(typeof(ControllerPoseSynchronizer))]
    public class ControllerPoseSynchronizerInspector : Editor
    {
        private readonly GUIContent synchronizationSettings = new GUIContent("Synchronization Settings");
        private static readonly string[] HandednessLabels = { "Left", "Right" };

        private SerializedProperty useSourcePoseData;
        private SerializedProperty poseAction;
        private SerializedProperty handedness;

        protected bool DrawHandednessProperty = true;

        protected virtual void OnEnable()
        {
            useSourcePoseData = serializedObject.FindProperty(nameof(useSourcePoseData));
            poseAction = serializedObject.FindProperty(nameof(poseAction));
            handedness = serializedObject.FindProperty(nameof(handedness));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();

            if (useSourcePoseData.FoldoutWithBoldLabelPropertyField(synchronizationSettings))
            {
                EditorGUI.indentLevel++;

                if (!useSourcePoseData.boolValue)
                {
                    EditorGUILayout.PropertyField(poseAction);
                }

                if (DrawHandednessProperty)
                {
                    var currentHandedness = (Handedness)handedness.enumValueIndex;
                    var handIndex = currentHandedness == Handedness.Right ? 1 : 0;

                    EditorGUI.BeginChangeCheck();
                    var newHandednessIndex = EditorGUILayout.Popup(handedness.displayName, handIndex, HandednessLabels);

                    if (EditorGUI.EndChangeCheck())
                    {
                        currentHandedness = newHandednessIndex == 0 ? Handedness.Left : Handedness.Right;
                        handedness.enumValueIndex = (int)currentHandedness;
                    }
                }

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}