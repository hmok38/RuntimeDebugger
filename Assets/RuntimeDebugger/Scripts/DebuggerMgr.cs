using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    /**
     *
     *  添加cmd命令: debugger.AddCmd("gm.addCoin", "gm命令加钱 参数1:钱的数量", (coinCount)=> Debug.Log($"加钱{coinCount}"),"加钱命令");
     *  设置cmd密码: debugger.SetCmdPassword("NewPassword");
     *  初始化:Init();会检查Application.persistentDataPath,中是否有 "logsetting.txt"文件,并设置LogEnable=true
     * 会返回一个常驻的Gameobject,并显示gui,否则返回空,即不开启日志窗口,也可以在外部根据条件强制开启传入布尔值即可
     */
    public class DebuggerMgr : MonoBehaviour
    {
        internal static readonly float DefaultWindowPadding = 5f;
        private static readonly float DefaultDesignWidth = 450;
        private static readonly float DefaultDesignHeight = 800;
        internal static readonly float DefaultWindowScale = 1f;
        private static readonly TextEditor STextEditor = new TextEditor();

        /// <summary>
        /// 默认调试器漂浮框大小。
        /// </summary>
        internal static readonly Rect DefaultIconRect = new Rect(DefaultWindowPadding, DefaultWindowPadding, 60, 60);

        /// <summary>
        /// 默认调试器窗口大小。
        /// </summary>
        internal static readonly Rect DefaultWindowRect =
            new Rect(DefaultWindowPadding, DefaultWindowPadding, 240f, 280f);

        [NonSerialized] public int MaxCount = 300;
        private readonly string _skinPath = "DebuggerSkin";
        private float _mWindowScale = DefaultWindowScale;
        [NonSerialized] public float MWindowWidth = DefaultWindowRect.width;
        [NonSerialized] public float MWindowHeight = DefaultWindowRect.height;
        private bool _mShowFullWindow;
        private Rect _mIconRect = DefaultIconRect;
        private Rect _mWindowRect = DefaultWindowRect;
        internal GUISkin MSkin;
        private DebuggerActiveWindowType _mActiveWindow = DebuggerActiveWindowType.AlwaysOpen;
        private Rect _mDragRect = new Rect(0f, 0f, float.MaxValue, 25f);
        internal int MaxToolbarXCount = 8;
        internal float MWindowContentMaxWidth = DefaultWindowRect.width - DefaultWindowPadding * 2;
        internal float MWindowContentMaxHeight = DefaultWindowRect.height - DefaultWindowPadding * 2;
        private ConsoleWindow _mConsoleWindow = new();
        private IDebuggerWindowManager _mDebuggerWindowManager;
        private FpsCounter _mFpsCounter;
        private SettingsWindow _mSettingsWindow = new();
        private CmdWindow _cmdWindow = new();

        internal float WindowScale
        {
            get { return _mWindowScale; }
            set { _mWindowScale = value; }
        }

        internal Rect IconRect
        {
            get { return _mIconRect; }
            set { _mIconRect = value; }
        }

        /// <summary>
        /// 获取或设置调试器窗口大小。
        /// </summary>
        internal Rect WindowRect
        {
            get { return _mWindowRect; }
            set { _mWindowRect = value; }
        }

        internal float DesignWidth
        {
            get
            {
                if (Application.platform == RuntimePlatform.WindowsEditor ||
                    Application.platform == RuntimePlatform.LinuxEditor ||
                    Application.platform == RuntimePlatform.OSXEditor)
                {
                    if (Screen.width > Screen.height) return DefaultDesignHeight;
                    return DefaultDesignWidth;
                }

                if (Screen.orientation == ScreenOrientation.Portrait ||
                    Screen.orientation == ScreenOrientation.PortraitUpsideDown)
                {
                    return DefaultDesignWidth;
                }

                return DefaultDesignHeight;
            }
        }

        internal float DesignHeight
        {
            get
            {
                if (Application.platform == RuntimePlatform.WindowsEditor ||
                    Application.platform == RuntimePlatform.LinuxEditor ||
                    Application.platform == RuntimePlatform.OSXEditor)
                {
                    if (Screen.width > Screen.height) return DefaultDesignWidth;
                    return DefaultDesignHeight;
                }

                if (Screen.orientation == ScreenOrientation.Portrait ||
                    Screen.orientation == ScreenOrientation.PortraitUpsideDown)
                {
                    return DefaultDesignHeight;
                }

                return DefaultDesignWidth;
            }
        }

        /// <summary>
        /// 获取或设置调试器窗口是否激活。
        /// </summary>
        internal bool ActiveWindow
        {
            get { return _mDebuggerWindowManager.ActiveWindow; }
            set
            {
                if (_mDebuggerWindowManager.ActiveWindow == value) return;
                enabled = value;
                _mDebuggerWindowManager.ActiveWindow = value;
                if (value)
                {
                    RegisterDebuggerOtherWindows();
                }
                else
                {
                    UnRegisterDebuggerOtherWindows();
                }
            }
        }

        public static DebuggerMgr Init(bool beForceCreat = false)
        {
            bool beCreat = false;
            int maxCount = 300;
            string filePath = Path.Combine(Application.persistentDataPath, "logsetting.txt");

            if (File.Exists(filePath))
            {
                var allLinesStr = File.ReadAllLines(filePath, Encoding.UTF8);

                for (int i = 0; i < allLinesStr.Length; i++)
                {
                    var lineStr = allLinesStr[i];
                    if (!string.IsNullOrEmpty(lineStr))
                    {
                        var keyValues = lineStr.Split("=");
                        if (keyValues == null || keyValues.Length != 2)
                        {
                            continue;
                        }

                        switch (keyValues[0])
                        {
                            case "LogEnable":
                                bool.TryParse(keyValues[1], out beCreat);

                                break;
                            case "LogMaxCount":
                                int.TryParse(keyValues[1], out maxCount);
                                break;
                        }
                    }
                }
            }

            if (!beForceCreat)
            {
                if (!beCreat) return null;
            }

            var mgr = new GameObject("DebuggerMgr").AddComponent<DebuggerMgr>();
            Object.DontDestroyOnLoad(mgr);
            mgr.MaxCount = maxCount;
            return mgr;
        }

        protected void Awake()
        {
            _mDebuggerWindowManager = new DebuggerWindowWindowManager();
            _mFpsCounter = new FpsCounter(0.5f);
            MSkin = Resources.Load<GUISkin>(_skinPath);
            RegisterDebuggerWindow("Home/Settings", _mSettingsWindow);
            RegisterDebuggerWindow("Console", _mConsoleWindow);
            switch (_mActiveWindow)
            {
                case DebuggerActiveWindowType.AlwaysOpen:
                    ActiveWindow = true;
                    break;

                case DebuggerActiveWindowType.OnlyOpenWhenDevelopment:
                    ActiveWindow = Debug.isDebugBuild;
                    break;

                case DebuggerActiveWindowType.OnlyOpenInEditor:
                    ActiveWindow = Application.isEditor;
                    break;

                case DebuggerActiveWindowType.CustomOpen:
                    ActiveWindow = false;
                    break;

                default:
                    ActiveWindow = false;
                    break;
            }

            _mIconRect = LimitWindowPos(_mIconRect);
        }

        private void Start()
        {
            _mConsoleWindow.MaxLine = this.MaxCount;
        }

        private void Update()
        {
            _mDebuggerWindowManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
            if (_mFpsCounter != null) _mFpsCounter.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            GUISkin cachedGuiSkin = GUI.skin;
            Matrix4x4 cachedMatrix = GUI.matrix;

            float scaleW = Screen.width / DesignWidth;
            float scaleH = Screen.height / DesignHeight;
            float minScale = Math.Min(scaleW, scaleH);
            GUI.skin = MSkin;
            // GUI.matrix = Matrix4x4.Scale(new Vector3(m_WindowScale, m_WindowScale, 1f));
            GUI.matrix = Matrix4x4.Scale(new Vector3(minScale, minScale, 1f));

            if (_mShowFullWindow)
            {
                string title = $"<b>DEBUGGER</b>　　{DateTime.Now}";
                _mWindowRect = GUILayout.Window(0, _mWindowRect, DrawWindow, title);
            }
            else
            {
                _mIconRect = GUILayout.Window(0, _mIconRect, DrawDebuggerWindowIcon, "<b>DEBUGGER</b>");
            }

            GUI.matrix = cachedMatrix;
            GUI.skin = cachedGuiSkin;
        }

        private void DrawWindow(int windowId)
        {
            GUI.DragWindow(_mDragRect);
            // m_WindowRect = LimitWindowPosTop(m_WindowRect);
            DrawDebuggerWindowGroup(_mDebuggerWindowManager.DebuggerWindowRoot, MaxToolbarXCount);
        }

        private void DrawDebuggerWindowGroup(IDebuggerWindowGroup debuggerWindowGroup, int toolbarXCount)
        {
            if (debuggerWindowGroup == null)
            {
                return;
            }

            GUILayout.Space(20);
            List<string> names = new List<string>();
            string[] debuggerWindowNames = debuggerWindowGroup.GetDebuggerWindowNames();
            for (int i = 0; i < debuggerWindowNames.Length; i++)
            {
                names.Add($"<b>{debuggerWindowNames[i]}</b>");
            }

            if (debuggerWindowGroup == _mDebuggerWindowManager.DebuggerWindowRoot)
            {
                names.Add("<b>Close</b>");
            }

            int toolbarIndex;
            GUILayoutOption op =
                GUILayout.Width(MWindowContentMaxWidth - MSkin.button.margin.left - MSkin.button.margin.right);
            toolbarIndex = GUILayout.SelectionGrid(debuggerWindowGroup.SelectedIndex, names.ToArray(),
                Math.Min(MaxToolbarXCount, toolbarXCount), op);

            if (toolbarIndex >= debuggerWindowGroup.DebuggerWindowCount)
            {
                _mShowFullWindow = false;
                return;
            }

            if (debuggerWindowGroup.SelectedWindow == null)
            {
                return;
            }

            if (debuggerWindowGroup.SelectedIndex != toolbarIndex)
            {
                debuggerWindowGroup.SelectedWindow.OnLeave();
                debuggerWindowGroup.SelectedIndex = toolbarIndex;
                debuggerWindowGroup.SelectedWindow.OnEnter();
            }

            IDebuggerWindowGroup subDebuggerWindowGroup = debuggerWindowGroup.SelectedWindow as IDebuggerWindowGroup;
            if (subDebuggerWindowGroup != null)
            {
                DrawDebuggerWindowGroup(subDebuggerWindowGroup,
                    Math.Min(MaxToolbarXCount, subDebuggerWindowGroup.DebuggerWindowCount));
            }

            debuggerWindowGroup.SelectedWindow.OnDraw();
        }

        private void DrawDebuggerWindowIcon(int windowId)
        {
            GUI.DragWindow(_mDragRect);
            _mIconRect = LimitWindowPosTop(_mIconRect);
            GUILayout.Space(5);
            Color32 color = Color.white;
            _mConsoleWindow.RefreshCount();
            if (_mConsoleWindow.FatalCount > 0)
            {
                color = _mConsoleWindow.GetLogStringColor(LogType.Exception);
            }
            else if (_mConsoleWindow.ErrorCount > 0)
            {
                color = _mConsoleWindow.GetLogStringColor(LogType.Error);
            }
            else if (_mConsoleWindow.WarningCount > 0)
            {
                color = _mConsoleWindow.GetLogStringColor(LogType.Warning);
            }
            else
            {
                color = _mConsoleWindow.GetLogStringColor(LogType.Log);
            }

            string title = string.Format("<color=#{0:x2}{1:x2}{2:x2}{3:x2}><b>FPS: {4:F2}</b></color>", color.r,
                color.g, color.b, color.a, _mFpsCounter.CurrentFps);
            if (GUILayout.Button(title, GUILayout.Width(100f), GUILayout.Height(40f)))
            {
                _mShowFullWindow = true;
            }
        }

        private Rect LimitWindowPosTop(Rect windowRect)
        {
            Vector2 p = windowRect.position;
            if (p.y < 0)
            {
                p.y = 0;
                windowRect.position = p;
            }

            return windowRect;
        }

        internal static void CopyToClipboard(string content)
        {
            STextEditor.text = content;
            STextEditor.OnFocus();
            STextEditor.Copy();
            STextEditor.text = string.Empty;
        }

        /// <summary>
        /// 注册调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <param name="debuggerWindow">要注册的调试器窗口。</param>
        /// <param name="args">初始化调试器窗口参数。</param>
        public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow, params object[] args)
        {
            _mDebuggerWindowManager.RegisterDebuggerWindow(this, path, debuggerWindow, args);
        }

        /// <summary>
        /// 解除注册调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <returns>是否解除注册调试器窗口成功。</returns>
        public bool UnregisterDebuggerWindow(string path)
        {
            return _mDebuggerWindowManager.UnregisterDebuggerWindow(path);
        }

        /// <summary>
        /// 还原调试器窗口布局。
        /// </summary>
        internal void ResetLayout()
        {
            IconRect = DefaultIconRect;
            WindowRect = DefaultWindowRect;
            WindowScale = DefaultWindowScale;
            MWindowWidth = DefaultWindowRect.width;
            MWindowHeight = DefaultWindowRect.height;
            MWindowContentMaxWidth = MWindowWidth - MSkin.window.padding.left - MSkin.window.padding.right;
            MWindowContentMaxHeight = MWindowHeight - MSkin.window.padding.top - MSkin.window.padding.bottom;
        }

        /** 全限制 **/
        private Rect LimitWindowPos(Rect windowRect)
        {
            Resolution r = Screen.currentResolution;
            Vector2 p = windowRect.position;
            p.x = Mathf.Clamp(windowRect.position.x, 0f, r.width / _mWindowScale - windowRect.width);
            p.y = Mathf.Clamp(windowRect.position.y, 0f, r.height / _mWindowScale - windowRect.height);
            return windowRect;
        }

        private void RegisterDebuggerOtherWindows()
        {
            if (!_mDebuggerWindowManager.ContainsDebuggerWindow("Console"))
                RegisterDebuggerWindow("Console", _mConsoleWindow);
            RegisterDebuggerWindow("Cmd", _cmdWindow);
            RegisterDebuggerWindow("Information/System", new SystemInformationWindow());
            RegisterDebuggerWindow("Information/Environment", new EnvironmentInformationWindow());
            RegisterDebuggerWindow("Information/Screen", new ScreenInformationWindow());
            RegisterDebuggerWindow("Information/Graphics", new GraphicsInformationWindow());
            RegisterDebuggerWindow("Information/Input/Summary", new InputSummaryInformationWindow());
            RegisterDebuggerWindow("Information/Input/Touch", new InputTouchInformationWindow());
            RegisterDebuggerWindow("Information/Input/Location", new InputLocationInformationWindow());
            RegisterDebuggerWindow("Information/Input/Acceleration", new InputAccelerationInformationWindow());
            RegisterDebuggerWindow("Information/Input/Gyroscope", new InputGyroscopeInformationWindow());
            RegisterDebuggerWindow("Information/Input/Compass", new InputCompassInformationWindow());
            RegisterDebuggerWindow("Information/Other/Scene", new SceneInformationWindow());
            RegisterDebuggerWindow("Information/Other/Path", new PathInformationWindow());
            RegisterDebuggerWindow("Information/Other/Time", new TimeInformationWindow());
            RegisterDebuggerWindow("Information/Other/Quality", new QualityInformationWindow());
            RegisterDebuggerWindow("Information/Other/Web Player", new WebPlayerInformationWindow());
            RegisterDebuggerWindow("Profiler/Summary", new ProfilerInformationWindow());
            RegisterDebuggerWindow("Profiler/Memory/Summary", new RuntimeMemorySummaryWindow());
            RegisterDebuggerWindow("Profiler/Memory/All", new RuntimeMemoryInformationWindow<Object>());
            RegisterDebuggerWindow("Profiler/Memory/Texture", new RuntimeMemoryInformationWindow<Texture>());
            RegisterDebuggerWindow("Profiler/Memory/Mesh", new RuntimeMemoryInformationWindow<Mesh>());
            RegisterDebuggerWindow("Profiler/Memory/Material", new RuntimeMemoryInformationWindow<Material>());
            RegisterDebuggerWindow("Profiler/Memory/Shader", new RuntimeMemoryInformationWindow<Shader>());
            RegisterDebuggerWindow("Profiler/Memory/AnimationClip",
                new RuntimeMemoryInformationWindow<AnimationClip>());
            RegisterDebuggerWindow("Profiler/Memory/AudioClip", new RuntimeMemoryInformationWindow<AudioClip>());
            RegisterDebuggerWindow("Profiler/Memory/Font", new RuntimeMemoryInformationWindow<Font>());
            RegisterDebuggerWindow("Profiler/Memory/TextAsset", new RuntimeMemoryInformationWindow<TextAsset>());
            RegisterDebuggerWindow("Profiler/Memory/ScriptableObject",
                new RuntimeMemoryInformationWindow<ScriptableObject>());
        }

        private void UnRegisterDebuggerOtherWindows()
        {
            UnregisterDebuggerWindow("Console");
            UnregisterDebuggerWindow("Information/System");
            UnregisterDebuggerWindow("Information/Environment");
            UnregisterDebuggerWindow("Information/Screen");
            UnregisterDebuggerWindow("Information/Graphics");
            UnregisterDebuggerWindow("Information/Input/Summary");
            UnregisterDebuggerWindow("Information/Input/Touch");
            UnregisterDebuggerWindow("Information/Input/Location");
            UnregisterDebuggerWindow("Information/Input/Acceleration");
            UnregisterDebuggerWindow("Information/Input/Gyroscope");
            UnregisterDebuggerWindow("Information/Input/Compass");
            UnregisterDebuggerWindow("Information/Other/Scene");
            UnregisterDebuggerWindow("Information/Other/Path");
            UnregisterDebuggerWindow("Information/Other/Time");
            UnregisterDebuggerWindow("Information/Other/Quality");
            UnregisterDebuggerWindow("Information/Other/Web Player");
            UnregisterDebuggerWindow("Profiler/Summary");
            UnregisterDebuggerWindow("Profiler/Memory/Summary");
            UnregisterDebuggerWindow("Profiler/Memory/All");
            UnregisterDebuggerWindow("Profiler/Memory/Texture");
            UnregisterDebuggerWindow("Profiler/Memory/Mesh");
            UnregisterDebuggerWindow("Profiler/Memory/Material");
            UnregisterDebuggerWindow("Profiler/Memory/Shader");
            UnregisterDebuggerWindow("Profiler/Memory/AnimationClip");
            UnregisterDebuggerWindow("Profiler/Memory/AudioClip");
            UnregisterDebuggerWindow("Profiler/Memory/Font");
            UnregisterDebuggerWindow("Profiler/Memory/TextAsset");
            UnregisterDebuggerWindow("Profiler/Memory/ScriptableObject");
            //OnUnRegisterDebuggerWindows?.Invoke();
        }

        /// <summary>
        /// 设置cmd界面的password
        /// </summary>
        /// <param name="password"></param>
        public void SetCmdPassword(string password)
        {
            _cmdWindow.Password = password;
        }

        /// <summary>
        /// 增加命令
        /// </summary>
        /// <param name="cmd">命令行</param>
        /// <param name="msg">显示在代码提示的提示文字,一般为命令用途和参数意义</param>
        /// <param name="action">当被执行时的回调</param>
        /// <param name="button">设置的话会生成一个按钮,方便快捷输入,按钮显示的值</param>
        public void AddCmd(string cmd, string msg, UnityAction action, string button = "")
        {
            _cmdWindow.AddCmd(cmd, msg, action, button);
        }

        /// <summary>
        /// 增加命令
        /// </summary>
        /// <param name="cmd">命令行</param>
        /// <param name="msg">显示在代码提示的提示文字,一般为命令用途和参数意义</param>
        /// <param name="action">当被执行时的回调</param>
        /// <param name="button">设置的话会生成一个按钮,方便快捷输入,按钮显示的值</param>
        public void AddCmd(string cmd, string msg, UnityAction<string> action, string button = "")
        {
            _cmdWindow.AddCmd(cmd, msg, action, button);
        }

        /// <summary>
        /// 增加命令
        /// </summary>
        /// <param name="cmd">命令行</param>
        /// <param name="msg">显示在代码提示的提示文字,一般为命令用途和参数意义</param>
        /// <param name="action">当被执行时的回调</param>
        /// <param name="button">设置的话会生成一个按钮,方便快捷输入,按钮显示的值</param>
        public void AddCmd(string cmd, string msg, UnityAction<string, string> action, string button = "")
        {
            _cmdWindow.AddCmd(cmd, msg, action, button);
        }

        /// <summary>
        /// 增加命令
        /// </summary>
        /// <param name="cmd">命令行</param>
        /// <param name="msg">显示在代码提示的提示文字,一般为命令用途和参数意义</param>
        /// <param name="action">当被执行时的回调</param>
        /// <param name="button">设置的话会生成一个按钮,方便快捷输入,按钮显示的值</param>
        public void AddCmd(string cmd, string msg, UnityAction<string, string, string> action, string button = "")
        {
            _cmdWindow.AddCmd(cmd, msg, action, button);
        }

        /// <summary>
        /// 增加命令
        /// </summary>
        /// <param name="cmd">命令行</param>
        /// <param name="msg">显示在代码提示的提示文字,一般为命令用途和参数意义</param>
        /// <param name="action">当被执行时的回调</param>
        /// <param name="button">设置的话会生成一个按钮,方便快捷输入,按钮显示的值</param>
        public void AddCmd(string cmd, string msg, UnityAction<string, string, string, string> action,
            string button = "")
        {
            _cmdWindow.AddCmd(cmd, msg, action, button);
        }

        /// <summary>
        /// 设置日志最大行数
        /// </summary>
        /// <param name="maxLine"></param>
        public void SetConsoleMaxLine(int maxLine)
        {
            _mConsoleWindow.MaxLine = maxLine;
        }
    }
}