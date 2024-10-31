using UnityEngine;

// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    public class ScreenInformationWindow : ScrollableDebuggerWindowBase
    {
        protected override void OnDrawScrollableWindow()
        {
            GUILayout.Label("<b>Screen Information</b>");
            GUILayout.BeginVertical("box");
            {
                DrawItem("Current Resolution", GetResolutionString(Screen.currentResolution));
                DrawItem("Screen Width",
                    $"{Screen.width} px / {Screen.width / Screen.dpi:F2} in / {2.54f * Screen.width / Screen.dpi:F2} cm");
                DrawItem("Screen Height",
                    $"{Screen.height} px / {(Screen.height / Screen.dpi):F2} in / {2.54f * (Screen.height) / Screen.dpi:F2} cm");
                DrawItem("Screen DPI", Screen.dpi.ToString("F2"));
                DrawItem("Screen Orientation", Screen.orientation.ToString());
                DrawItem("Is Full Screen", Screen.fullScreen.ToString());
#if UNITY_2018_1_OR_NEWER
                DrawItem("Full Screen Mode", Screen.fullScreenMode.ToString());
#endif
                DrawItem("Sleep Timeout", GetSleepTimeoutDescription(Screen.sleepTimeout));
#if UNITY_2019_2_OR_NEWER
                DrawItem("Brightness", Screen.brightness.ToString("F2"));
#endif
                DrawItem("Cursor Visible", Cursor.visible.ToString());
                DrawItem("Cursor Lock State", Cursor.lockState.ToString());
                DrawItem("Auto Landscape Left", Screen.autorotateToLandscapeLeft.ToString());
                DrawItem("Auto Landscape Right", Screen.autorotateToLandscapeRight.ToString());
                DrawItem("Auto Portrait", Screen.autorotateToPortrait.ToString());
                DrawItem("Auto Portrait Upside Down", Screen.autorotateToPortraitUpsideDown.ToString());
#if UNITY_2017_2_OR_NEWER && !UNITY_2017_2_0
                DrawItem("Safe Area", Screen.safeArea.ToString());
#endif
#if UNITY_2019_2_OR_NEWER
                DrawItem("Cutouts", GetCutoutsString(Screen.cutouts));
#endif
                DrawItem("Support Resolutions", GetResolutionsString(Screen.resolutions));
            }
            GUILayout.EndVertical();
        }

        private string GetSleepTimeoutDescription(int sleepTimeout)
        {
            if (sleepTimeout == SleepTimeout.NeverSleep)
            {
                return "Never Sleep";
            }

            if (sleepTimeout == SleepTimeout.SystemSetting)
            {
                return "System Setting";
            }

            return sleepTimeout.ToString();
        }

        private string GetResolutionString(Resolution resolution)
        {
            return $"{resolution.width} x {resolution.height} @ {resolution.refreshRate}Hz";
        }

        private string GetCutoutsString(Rect[] cutouts)
        {
            string[] cutoutStrings = new string[cutouts.Length];
            for (int i = 0; i < cutouts.Length; i++)
            {
                cutoutStrings[i] = cutouts[i].ToString();
            }

            return string.Join("; ", cutoutStrings);
        }

        private string GetResolutionsString(Resolution[] resolutions)
        {
            string[] resolutionStrings = new string[resolutions.Length];
            for (int i = 0; i < resolutions.Length; i++)
            {
                resolutionStrings[i] = GetResolutionString(resolutions[i]);
            }

            return string.Join("; ", resolutionStrings);
        }
    }
}