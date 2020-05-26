// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers.Hands;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;

namespace XRTK.SDK.Input
{
    /// <summary>
    /// Utilitiy component to record a hand controller's data into a file.
    /// </summary>
    public class HandDataRecorder : InputSystemGlobalListener, IMixedRealityInputHandler<HandData>
    {
        private RecordedHandJoints currentRecording;

        [SerializeField]
        [Tooltip("The handedness of the hand to record data for.")]
        private Handedness targetHandedness = Handedness.Right;

        [SerializeField]
        [Tooltip("Keycode to trigger saving of the currently recorded data.")]
        private KeyCode saveRecordingKey = KeyCode.Return;

        private void Update()
        {
            if (currentRecording != null && UnityEngine.Input.GetKeyDown(saveRecordingKey))
            {
                Debug.Log(JsonUtility.ToJson(currentRecording));
            }
        }

        public void OnInputChanged(InputEventData<HandData> eventData)
        {
#if UNITY_EDITOR

            if (targetHandedness != eventData.Handedness)
            {
                return;
            }

            var handData = eventData.InputData;
            var recordedHandJoints = new RecordedHandJoints();
            var jointPoses = new RecordedHandJoint[HandData.JointCount];

            for (int i = 0; i < HandData.JointCount; i++)
            {
                var jointPose = handData.Joints[i];
                jointPoses[i] = new RecordedHandJoint((TrackedHandJoint)i, jointPose);
            }

            recordedHandJoints.items = jointPoses;
            currentRecording = recordedHandJoints;

#endif
        }
    }
}
