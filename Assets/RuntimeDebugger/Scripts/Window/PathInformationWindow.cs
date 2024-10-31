using System;
using UnityEngine;

namespace RuntimeDebugger
{
    public class PathInformationWindow : ScrollableDebuggerWindowBase
    {
        protected override void OnDrawScrollableWindow()
        {
            GUILayout.Label("<b>Path Information</b>");
            GUILayout.BeginVertical("box");
            {
                DrawItem("Current Directory", Environment.CurrentDirectory);
                DrawItem("Data Path", (Application.dataPath));
                DrawItem("Persistent Data Path", (Application.persistentDataPath));
                DrawItem("Streaming Assets Path", (Application.streamingAssetsPath));
                DrawItem("Temporary Cache Path", (Application.temporaryCachePath));
#if UNITY_2018_3_OR_NEWER
                DrawItem("Console Log Path", (Application.consoleLogPath));
#endif
            }
            GUILayout.EndVertical();
        }
    }
}