﻿using UnityEngine;

namespace RuntimeDebugger
{
    public class InputAccelerationInformationWindow : ScrollableDebuggerWindowBase
    {
        protected override void OnDrawScrollableWindow()
        {
            GUILayout.Label("<b>Input Acceleration Information</b>");
            GUILayout.BeginVertical("box");
            {
                DrawItem("Acceleration", Input.acceleration.ToString());
                DrawItem("Acceleration Event Count", Input.accelerationEventCount.ToString());
                DrawItem("Acceleration Events", GetAccelerationEventsString(Input.accelerationEvents));
            }
            GUILayout.EndVertical();
        }

        private string GetAccelerationEventString(AccelerationEvent accelerationEvent)
        {
            return $"{accelerationEvent.acceleration}, {accelerationEvent.deltaTime}";
        }

        private string GetAccelerationEventsString(AccelerationEvent[] accelerationEvents)
        {
            string[] accelerationEventStrings = new string[accelerationEvents.Length];
            for (int i = 0; i < accelerationEvents.Length; i++)
            {
                accelerationEventStrings[i] = GetAccelerationEventString(accelerationEvents[i]);
            }

            return string.Join("; ", accelerationEventStrings);
        }
    }
}