// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XRTK.Definitions.InputSystem;
using XRTK.Editor.Extensions;
using XRTK.SDK.Input.Handlers;

namespace XRTK.SDK.Editor.Input.Handlers
{
    [CustomEditor(typeof(SpeechInputHandler))]
    public class SpeechInputHandlerInspector : BaseInputHandlerInspector
    {
        private static readonly GUIContent RemoveButtonContent = new GUIContent("-", "Remove keyword");
        private static readonly GUIContent AddButtonContent = new GUIContent("+", "Add keyword");
        private static readonly GUILayoutOption MiniButtonWidth = GUILayout.Width(20.0f);

        private string[] registeredKeywords;

        private SerializedProperty keywords;
        private SerializedProperty persistentKeywords;

        protected override void OnEnable()
        {
            base.OnEnable();

            keywords = serializedObject.FindProperty(nameof(keywords));
            persistentKeywords = serializedObject.FindProperty(nameof(persistentKeywords));

            var profiles = ScriptableObjectExtensions.GetAllInstances<MixedRealitySpeechCommandsProfile>();
            registeredKeywords = RegisteredKeywords(profiles).Distinct().ToArray();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (registeredKeywords == null || registeredKeywords.Length == 0)
            {
                EditorGUILayout.HelpBox("No speech commands found.\n\nKeywords can be registered via Speech Commands Profile on the Mixed Reality Toolkit's Configuration Profile.", MessageType.Error);
                return;
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(persistentKeywords);

            ShowList(keywords);
            serializedObject.ApplyModifiedProperties();

            // error and warning messages
            if (keywords.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No keywords have been assigned!", MessageType.Warning);
            }
            else
            {
                var handler = (SpeechInputHandler)target;
                var duplicateKeyword = handler.Keywords
                    .GroupBy(keyword => keyword.Keyword.ToLower())
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key).FirstOrDefault();

                if (duplicateKeyword != null)
                {
                    EditorGUILayout.HelpBox($"Keyword \'{duplicateKeyword}\' is assigned more than once!", MessageType.Warning);
                }
            }
        }

        private void ShowList(SerializedProperty list)
        {
            EditorGUI.indentLevel++;

            // remove the keywords already assigned from the registered list
            var handler = (SpeechInputHandler)target;
            var availableKeywords = new string[0];

            if (handler.Keywords != null)
            {
                availableKeywords = registeredKeywords.Except(handler.Keywords.Select(keywordAndResponse => keywordAndResponse.Keyword)).ToArray();
            }

            // keyword rows
            for (int index = 0; index < list.arraySize; index++)
            {
                // the element
                var speechCommandProperty = list.GetArrayElementAtIndex(index);
                EditorGUILayout.BeginHorizontal();
                var elementExpanded = EditorGUILayout.PropertyField(speechCommandProperty);
                GUILayout.FlexibleSpace();
                // the remove element button
                var elementRemoved = GUILayout.Button(RemoveButtonContent, EditorStyles.miniButton, MiniButtonWidth);

                EditorGUILayout.EndHorizontal();

                if (elementRemoved)
                {
                    list.DeleteArrayElementAtIndex(index);
                    return;
                }

                var keywordProperty = speechCommandProperty.FindPropertyRelative("keyword");

                var invalidKeyword = registeredKeywords.All(keyword => keyword != keywordProperty.stringValue);

                if (invalidKeyword)
                {
                    EditorGUILayout.HelpBox("Registered keyword is not recognized in the speech command profile!", MessageType.Error);
                }

                if (elementExpanded)
                {
                    var orderedKeywords = availableKeywords.Concat(new[] { keywordProperty.stringValue }).OrderBy(keyword => keyword).ToArray();
                    var previousSelection = ArrayUtility.IndexOf(orderedKeywords, keywordProperty.stringValue);
                    var currentSelection = EditorGUILayout.Popup("Keyword", previousSelection, orderedKeywords);

                    if (currentSelection != previousSelection)
                    {
                        keywordProperty.stringValue = orderedKeywords[currentSelection];
                    }

                    var responseProperty = speechCommandProperty.FindPropertyRelative("response");
                    EditorGUILayout.PropertyField(responseProperty, true);
                }
            }

            // add button row
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // the add element button
            if (GUILayout.Button(AddButtonContent, EditorStyles.miniButton, MiniButtonWidth))
            {
                var index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                var elementProperty = list.GetArrayElementAtIndex(index);
                var keywordProperty = elementProperty.FindPropertyRelative("keyword");
                keywordProperty.stringValue = string.Empty;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private static IEnumerable<string> RegisteredKeywords(MixedRealitySpeechCommandsProfile[] profiles)
        {
            return from profile in profiles
                   from command in profile.SpeechCommands
                   select command.Keyword;
        }
    }
}