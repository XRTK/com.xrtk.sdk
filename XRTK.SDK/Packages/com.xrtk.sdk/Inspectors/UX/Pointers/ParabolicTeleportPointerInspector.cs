// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;
using XRTK.Inspectors.Extensions;
using XRTK.SDK.UX.Pointers;

namespace XRTK.SDK.Inspectors.UX.Pointers
{
    [CustomEditor(typeof(ParabolicTeleportPointer))]
    public class ParabolicTeleportPointerInspector : TeleportPointerInspector
    {
        private readonly GUIContent parabolicFoldoutHeaderContent = new GUIContent("Parabolic Pointer Options");

        private SerializedProperty minParabolaVelocity;
        private SerializedProperty maxParabolaVelocity;
        private SerializedProperty minDistanceModifier;
        private SerializedProperty maxDistanceModifier;

        protected override void OnEnable()
        {
            base.OnEnable();

            minParabolaVelocity = serializedObject.FindProperty(nameof(minParabolaVelocity));
            maxParabolaVelocity = serializedObject.FindProperty(nameof(maxParabolaVelocity));
            minDistanceModifier = serializedObject.FindProperty(nameof(minDistanceModifier));
            maxDistanceModifier = serializedObject.FindProperty(nameof(maxDistanceModifier));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (minParabolaVelocity.FoldoutWithBoldLabelPropertyField(parabolicFoldoutHeaderContent))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(maxParabolaVelocity);
                EditorGUILayout.PropertyField(minDistanceModifier);
                EditorGUILayout.PropertyField(maxDistanceModifier);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}