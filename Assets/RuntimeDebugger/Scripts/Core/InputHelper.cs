using UnityEngine;

// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    /// <summary>
    /// 多平台文本输入抽象层（仅程序集内可见）。
    ///
    /// 标准移动平台（Android/iOS）使用 Unity TouchScreenKeyboard；
    /// 特殊平台（如微信小游戏）通过 DebuggerMgr.SetCustomInput() 注入回调。
    ///
    /// 业务层对接示例（微信小游戏，参考 WX SDK 官方 WXInputFieldAdapter）：
    /// <code>
    /// string _wxText = "";
    /// bool _wxKeyboardClosed = false;
    ///
    /// void OnWxInput(OnKeyboardInputListenerResult v)
    /// {
    ///     _wxText = v.value;
    ///     _wxKeyboardClosed = false;   // 正在输入，键盘未关闭
    /// }
    /// void OnWxConfirm(OnKeyboardInputListenerResult v)
    /// {
    ///     _wxText = v.value;
    ///     _wxKeyboardClosed = true;    // 用户点"完成"，标记已关闭
    /// }
    /// void OnWxComplete(OnKeyboardInputListenerResult v)
    /// {
    ///     _wxText = v.value;
    ///     _wxKeyboardClosed = true;    // 键盘收起，标记已关闭
    /// }
    ///
    /// debugger.SetCustomInput(
    ///     isSupported:          () => IsWeChatMiniGame(),
    ///     onShowKeyboard:       (text) => {
    ///         _wxKeyboardClosed = false;  // 重置标记
    ///         ...
    ///     },
    ///     onHideKeyboard:       () => { ... },
    ///     onGetKeyboardText:    () => _wxText,
    ///     onSetKeyboardText:    (text) => { ... },
    ///     onGetKeyboardClosed:  () => _wxKeyboardClosed
    /// );
    /// </code>
    /// </summary>
    internal static class InputHelper
    {
        #region External Hooks — 由业务层注入

        /// <summary>
        /// 判断是否使用外部键盘系统（如微信小游戏）。
        /// 如果返回 true，将优先使用 OnShowKeyboard/OnHideKeyboard 等回调。
        /// </summary>
        public static System.Func<bool> IsExternalKeyboardSupported;

        /// <summary>
        /// 显示外部键盘，参数为初始文本。
        /// </summary>
        public static System.Action<string> OnShowKeyboard;

        /// <summary>
        /// 隐藏外部键盘。
        /// </summary>
        public static System.Action OnHideKeyboard;

        /// <summary>
        /// 获取外部键盘当前文本。
        /// </summary>
        public static System.Func<string> OnGetKeyboardText;

        /// <summary>
        /// 程序化设置外部键盘文本（如预填命令）。
        /// </summary>
        public static System.Action<string> OnSetKeyboardText;

        /// <summary>
        /// 轮询外部键盘是否已被用户关闭。
        /// 业务层在微信 OnKeyboardConfirm / OnKeyboardComplete 中设置标记为 true，
        /// 在 OnKeyboardInput 和 onShowKeyboard 中重置为 false。
        /// InputHelper 每帧 OnGUI 轮询此值，一旦为 true 则重置焦点状态。
        /// </summary>
        public static System.Func<bool> OnGetKeyboardClosed;

        #endregion

        #region Private State

        private static TouchScreenKeyboard _fallbackKeyboard;
        private static bool _hasFocus;
        private static bool _externalActive;
        private static string _lastControlName;

        #endregion

        #region Public Properties

        /// <summary>
        /// 当前平台是否支持键盘输入。
        /// 外部注入优先 → 标准移动平台 TouchScreenKeyboard → false（桌面/普通 WebGL）。
        /// </summary>
        public static bool IsSupported
        {
            get
            {
                if (IsExternalKeyboardSupported != null && IsExternalKeyboardSupported())
                    return true;

#if UNITY_ANDROID || UNITY_IOS
                return TouchScreenKeyboard.isSupported;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 键盘当前是否激活。
        /// 外部键盘跟踪 _externalActive 标志；
        /// TouchScreenKeyboard 直接读取 active 状态。
        /// </summary>
        public static bool IsKeyboardActive
        {
            get
            {
                if (UsingExternalKeyboard)
                    return _externalActive;

                if (_fallbackKeyboard != null)
                    return _fallbackKeyboard.active;

                return false;
            }
        }

        /// <summary>
        /// 获取键盘当前文本。
        /// 如果外部键盘激活，从 OnGetKeyboardText 获取；
        /// 如果 TouchScreenKeyboard 激活，从 fallback keyboard 获取；
        /// 否则返回空字符串。
        /// </summary>
        public static string Text
        {
            get
            {
                if (UsingExternalKeyboard && OnGetKeyboardText != null)
                    return OnGetKeyboardText() ?? string.Empty;

                if (_fallbackKeyboard != null)
                    return _fallbackKeyboard.text ?? string.Empty;

                return string.Empty;
            }
        }

        private static bool UsingExternalKeyboard
        {
            get { return IsExternalKeyboardSupported != null && IsExternalKeyboardSupported(); }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 在 OnGUI 中每帧调用，自动跟踪 IMGUI 命名控件焦点变化并管理键盘生命周期。
        /// 返回有效文本：键盘激活时返回键盘文本，否则返回传入的 textFieldText。
        ///
        /// 使用方式：
        /// <code>
        /// GUI.SetNextControlName("MyInput");
        /// var text = GUILayout.TextField(myText);
        /// myText = InputHelper.SyncFromFocusChange("MyInput", text);
        /// </code>
        /// </summary>
        /// <param name="controlName">IMGUI 控件名（与 GUI.SetNextControlName 一致）</param>
        /// <param name="textFieldText">GUILayout.TextField 返回的当前文本</param>
        /// <returns>有效文本（键盘文本或 TextField 文本）</returns>
        public static string SyncFromFocusChange(string controlName, string textFieldText)
        {
            if (!IsSupported)
                return textFieldText;

            // 焦点变化仅在 Repaint 事件中可靠检测
            if (Event.current.type != EventType.Repaint)
                return GetEffectiveText(textFieldText);

            // 轮询外部键盘是否已被用户关闭（微信 OnConfirm/OnComplete 中置 true）
            // 重置焦点并清除 IMGUI keyboardControl，让下次点击能触发真正的焦点变化
            if (_hasFocus && UsingExternalKeyboard && OnGetKeyboardClosed != null && OnGetKeyboardClosed())
            {
                _externalActive = false;
                _hasFocus = false;
                GUIUtility.keyboardControl = 0;
            }

            // TouchScreenKeyboard 被用户手动关闭（非外部回调），自动检测并重置
            if (_hasFocus && !IsKeyboardActive)
            {
                _hasFocus = false;
            }

            bool currentFocus = GUI.GetNameOfFocusedControl() == controlName;

            if (currentFocus && !_hasFocus)
            {
                // 获得焦点 — 显示键盘
                ShowKeyboardInternal(textFieldText);
            }
            else if (!currentFocus && _hasFocus)
            {
                // 失去焦点 — 隐藏键盘
                HideKeyboardInternal();
            }

            _hasFocus = currentFocus;
            _lastControlName = controlName;

            string effectiveText = GetEffectiveText(textFieldText);

            // 微信等外部键盘平台：将 IMGUI 光标设为透明
            if (UsingExternalKeyboard)
            {
                GUI.skin.settings.cursorColor = Color.clear;
            }

            return effectiveText;
        }

        /// <summary>
        /// 程序化设置键盘文本（如按钮点击预填命令）。
        /// </summary>
        public static void SetText(string text)
        {
            if (UsingExternalKeyboard && OnSetKeyboardText != null)
            {
                OnSetKeyboardText(text);
                return;
            }

            if (_fallbackKeyboard != null)
            {
                _fallbackKeyboard.text = text ?? string.Empty;
            }
        }

        /// <summary>
        /// 强制隐藏键盘并重置焦点状态。
        /// 用于密码解锁后、命令执行后等需要主动关闭键盘的场景。
        /// </summary>
        public static void HideKeyboard()
        {
            HideKeyboardInternal();
            _hasFocus = false;
        }

        #endregion

        #region Private Methods

        private static void ShowKeyboardInternal(string initialText)
        {
            if (UsingExternalKeyboard)
            {
                _externalActive = true;
                OnShowKeyboard?.Invoke(initialText ?? string.Empty);
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            if (TouchScreenKeyboard.isSupported)
            {
                if (_fallbackKeyboard == null || !_fallbackKeyboard.active)
                {
                    _fallbackKeyboard = TouchScreenKeyboard.Open(
                        initialText ?? string.Empty,
                        TouchScreenKeyboardType.Default);
                }
            }
#endif
        }

        private static void HideKeyboardInternal()
        {
            if (UsingExternalKeyboard)
            {
                _externalActive = false;
                OnHideKeyboard?.Invoke();
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            if (_fallbackKeyboard != null && _fallbackKeyboard.active)
            {
                _fallbackKeyboard.active = false;
            }
#endif
        }

        private static string GetEffectiveText(string textFieldText)
        {
            if (UsingExternalKeyboard)
            {
                // 键盘激活时，以外部键盘文本为准
                if (_hasFocus && OnGetKeyboardText != null)
                {
                    return OnGetKeyboardText() ?? textFieldText;
                }

                return textFieldText;
            }

            if (_fallbackKeyboard != null && _fallbackKeyboard.active)
            {
                return _fallbackKeyboard.text ?? textFieldText;
            }

            return textFieldText;
        }

        #endregion
    }
}