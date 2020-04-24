// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEditor;
using UnityEngine;
using XRTK.Inspectors.Extensions;
using XRTK.SDK.UX.Pointers;

namespace XRTK.SDK.Inspectors.UX.Pointers
{
    [CustomEditor(typeof(TeleportPointer))]
    public class TeleportPointerInspector : LinePointerInspector
    {
        private readonly GUIContent teleportFoldoutHeader = new GUIContent("Teleport Pointer Settings");

        private SerializedProperty teleportAction;
        private SerializedProperty inputThreshold;
        private SerializedProperty angleOffset;
        private SerializedProperty teleportActivationAngle;
        private SerializedProperty rotateActivationAngle;
        private SerializedProperty rotationAmount;
        private SerializedProperty backStrafeActivationAngle;
        private SerializedProperty strafeAmount;
        private SerializedProperty upDirectionThreshold;
        private SerializedProperty lineColorHotSpot;
        private SerializedProperty validLayers;
        private SerializedProperty invalidLayers;

        protected override void OnEnable()
        {
            DrawBasePointerActions = false;
            base.OnEnable();

            teleportAction = serializedObject.FindProperty(nameof(teleportAction));
            inputThreshold = serializedObject.FindProperty(nameof(inputThreshold));
            angleOffset = serializedObject.FindProperty(nameof(angleOffset));
            teleportActivationAngle = serializedObject.FindProperty(nameof(teleportActivationAngle));
            rotateActivationAngle = serializedObject.FindProperty(nameof(rotateActivationAngle));
            rotationAmount = serializedObject.FindProperty(nameof(rotationAmount));
            backStrafeActivationAngle = serializedObject.FindProperty(nameof(backStrafeActivationAngle));
            strafeAmount = serializedObject.FindProperty(nameof(strafeAmount));
            upDirectionThreshold = serializedObject.FindProperty(nameof(upDirectionThreshold));
            lineColorHotSpot = serializedObject.FindProperty(nameof(lineColorHotSpot));
            validLayers = serializedObject.FindProperty(nameof(validLayers));
            invalidLayers = serializedObject.FindProperty(nameof(invalidLayers));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (teleportAction.FoldoutWithBoldLabelPropertyField(teleportFoldoutHeader))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(inputThreshold);
                EditorGUILayout.PropertyField(angleOffset);
                EditorGUILayout.PropertyField(teleportActivationAngle);
                EditorGUILayout.PropertyField(rotateActivationAngle);
                EditorGUILayout.PropertyField(rotationAmount);
                EditorGUILayout.PropertyField(backStrafeActivationAngle);
                EditorGUILayout.PropertyField(strafeAmount);
                EditorGUILayout.PropertyField(upDirectionThreshold);
                EditorGUILayout.PropertyField(lineColorHotSpot);
                EditorGUILayout.PropertyField(validLayers);
                EditorGUILayout.PropertyField(invalidLayers);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}