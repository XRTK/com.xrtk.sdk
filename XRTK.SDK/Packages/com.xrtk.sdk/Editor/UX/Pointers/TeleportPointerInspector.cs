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

        private SerializedProperty lineColorAnchor;

        protected override void OnEnable()
        {
            DrawBasePointerActions = false;
            base.OnEnable();

            lineColorAnchor = serializedObject.FindProperty(nameof(lineColorAnchor));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (lineColorAnchor.FoldoutWithBoldLabelPropertyField(teleportFoldoutHeader))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(lineColorAnchor);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
