﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEditor;
using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.Editor.Extensions;
using XRTK.SDK.Input.Handlers;

namespace XRTK.SDK.Editor.Input.Handlers
{
    [CustomEditor(typeof(ControllerPoseSynchronizer), true, isFallback = true)]
    public class ControllerPoseSynchronizerInspector : UnityEditor.Editor
    {
        private static readonly GUIContent SynchronizationSettings = new GUIContent("Synchronization Settings");
        private static readonly string[] HandednessLabels = { "Left", "Right" };

        private SerializedProperty useSourcePoseData;
        private SerializedProperty poseAction;
        private SerializedProperty handedness;
        private SerializedProperty destroyOnSourceLost;

        protected bool DrawHandednessProperty = true;

        protected virtual void OnEnable()
        {
            useSourcePoseData = serializedObject.FindProperty(nameof(useSourcePoseData));
            poseAction = serializedObject.FindProperty(nameof(poseAction));
            handedness = serializedObject.FindProperty(nameof(handedness));
            destroyOnSourceLost = serializedObject.FindProperty(nameof(destroyOnSourceLost));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();

            if (useSourcePoseData.FoldoutWithBoldLabelPropertyField(SynchronizationSettings))
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

                EditorGUILayout.PropertyField(destroyOnSourceLost);

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}