// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using XRTK.SDK.Input.Handlers;

namespace XRTK.SDK.Inspectors.Input.Handlers
{
    [CustomEditor(typeof(PointerClickHandler))]
    public class PointerClickHandlerInspector : BaseInputHandlerInspector
    {
        private SerializedProperty onPointerUpActionEvent;
        private SerializedProperty onPointerDownActionEvent;
        private SerializedProperty onPointerClickedActionEvent;

        protected override void OnEnable()
        {
            base.OnEnable();

            onPointerUpActionEvent = serializedObject.FindProperty(nameof(onPointerUpActionEvent));
            onPointerDownActionEvent = serializedObject.FindProperty(nameof(onPointerDownActionEvent));
            onPointerClickedActionEvent = serializedObject.FindProperty(nameof(onPointerClickedActionEvent));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.PropertyField(onPointerUpActionEvent, true);
            EditorGUILayout.PropertyField(onPointerDownActionEvent, true);
            EditorGUILayout.PropertyField(onPointerClickedActionEvent, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}