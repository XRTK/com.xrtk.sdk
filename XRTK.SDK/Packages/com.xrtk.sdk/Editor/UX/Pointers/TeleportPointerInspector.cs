// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEditor;
using UnityEngine;
using XRTK.Editor.Extensions;
using XRTK.SDK.UX.Pointers;

namespace XRTK.SDK.Editor.UX.Pointers
{
    [CustomEditor(typeof(TeleportPointer))]
    public class TeleportPointerInspector : LinePointerInspector
    {
        private readonly GUIContent teleportFoldoutHeader = new GUIContent("Teleport Pointer Settings");

        private SerializedProperty lineColorHotSpot;

        protected override void OnEnable()
        {
            DrawBasePointerActions = false;
            base.OnEnable();

            lineColorHotSpot = serializedObject.FindProperty(nameof(lineColorHotSpot));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (lineColorHotSpot.FoldoutWithBoldLabelPropertyField(teleportFoldoutHeader))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(lineColorHotSpot);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
