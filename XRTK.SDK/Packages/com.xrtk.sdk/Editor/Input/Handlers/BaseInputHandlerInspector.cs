// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;

namespace XRTK.SDK.Editor.Input.Handlers
{
    public class BaseInputHandlerInspector : UnityEditor.Editor
    {
        private SerializedProperty isFocusRequired;

        protected virtual void OnEnable()
        {
            isFocusRequired = serializedObject.FindProperty(nameof(isFocusRequired));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(isFocusRequired);
            serializedObject.ApplyModifiedProperties();
        }
    }
}