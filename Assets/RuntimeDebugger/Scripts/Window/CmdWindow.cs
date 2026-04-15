using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    public class CmdWindow : ScrollableDebuggerWindowBase
    {
        private const string DefaultGroupName = "默认";
        private const float GroupListWidth = 120f;
        private const float MinGroupListWidth = 72f;
        private const float GroupPanelSpacing = 8f;
        private const float ButtonMinWidth = 120f;
        private const float GroupScrollbarWidthScale = 0.5f;
        /// <summary>
        /// 命令提示区固定高度（内部滚动），避免行数变化时推动下方分栏起始位置。
        /// </summary>
        private const float CmdHintBlockHeight = 32f;
        /// <summary>
        /// 执行结果区固定高度（换行裁剪），避免点击按钮后分栏上下跳动。
        /// </summary>
        private const float CmdResultBlockHeight = 56f;
        /// <summary>
        /// 分栏以上除提示区外固定占用高度（标题、输入行、间距、结果区、分栏前间距等），与 <see cref="CmdHintBlockHeight"/> 相加用于计算分栏可用高度。
        /// </summary>
        private const float CmdSplitFixedAboveHintBlock = 142f;
        /// <summary>分栏前与结果区之间的间距（与 <see cref="DrawGroupList"/> 内首行 <c>GUILayout.Space</c> 一致）。</summary>
        private const float SplitListTopSpace = 6f;
        /// <summary>分栏底部与窗口内容区底边的安全留白，避免贴边或裁切。</summary>
        private const float SplitPanelsBottomSafety = 8f;
        private static readonly Color ScrollPanelColor = new Color(0f, 0f, 0f, 0.35f);
        /// <summary>
        /// group所在的cmd列表
        /// </summary>
        private readonly Dictionary<string, List<CmdInfo>> _cmdGroupDict = new Dictionary<string, List<CmdInfo>>();
        /// <summary>
        /// cmd所在的groupName映射表
        /// </summary>
        private readonly Dictionary<string, string> _cmdGroupNameDict = new Dictionary<string, string>();
        internal string Password = "";
        private bool _beOpenCmd;
        private string _inputPassword = "";
        private string _inputCmd = "";
        private string _resultMsg;
        private List<CmdInfo> _checkCmdInfos = new List<CmdInfo>();
        private string _selectedGroupName = DefaultGroupName;
        private Vector2 _groupListScrollPosition = Vector2.zero;
        private Vector2 _btnListScrollPosition = Vector2.zero;
        private Vector2 _hintScrollPosition = Vector2.zero;
        private static GUIStyle _clipWrapLabelStyle;
        private static GUIStyle _hintLabelStyle;

        internal void AddCmd(string cmd, string cmdMsg, Delegate cb, string button = "", string groupName = null)
        {
            EnsureDefaultGroup();
            foreach (var cmdLs in _cmdGroupDict.Values)
            {
                cmdLs.RemoveAll(x => x.Cmd.Equals(cmd));
            }

            CmdInfo newInfo = new CmdInfo()
            {
                Cmd = cmd,
                Msg = cmdMsg,
                Delegate = cb,
                Button = button
            };
            groupName = string.IsNullOrEmpty(groupName) ? DefaultGroupName : groupName;
            if (!_cmdGroupDict.TryGetValue(groupName, out var cmdInfoLs))
            {
                cmdInfoLs = new List<CmdInfo>();
                _cmdGroupDict[groupName] = cmdInfoLs;
            }

            cmdInfoLs.Add(newInfo);
            _cmdGroupNameDict[cmd] = groupName;
        }

        private bool _beForceFindCmd;
        private string _passwordMsg;
        private bool _hasFocus;
        private TouchScreenKeyboard _keyboard;
        protected override bool UseOuterScrollView => false;

        private void SetTouchScreenKeyBoard(string focusedControlName)
        {
            if (TouchScreenKeyboard.isSupported && Event.current.type == EventType.Repaint)
            {
                bool currentFocus = GUI.GetNameOfFocusedControl() == focusedControlName; //InputCmd

                if (currentFocus && !_hasFocus) //获得焦点就创建一个新的
                {
                    if (_keyboard == null || !_keyboard.active)
                    {
                        _keyboard = TouchScreenKeyboard.Open(_inputCmd, TouchScreenKeyboardType.Default);
                    }
                }
                else if (!currentFocus && _hasFocus)
                {
                    if (_keyboard != null && _keyboard.active)
                    {
                        _keyboard.active = false;
                    }
                }

                _hasFocus = currentFocus;
            }
        }

        protected override void OnDrawScrollableWindow()
        {
            EnsureDefaultGroup();
            GUI.SetNextControlName("emptyLabel");
            GUILayout.Label("CMD命令");
            if (!Application.isEditor && !_beOpenCmd)
            {
                GUI.SetNextControlName("PasswordTextField");
                _inputPassword = GUILayout.TextField(_inputPassword);

                SetTouchScreenKeyBoard("PasswordTextField");

                if (TouchScreenKeyboard.isSupported && _keyboard != null)
                {
                    //根据软键盘赋值
                    _inputPassword = _keyboard.text;
                }

                if (GUILayout.Button("输入密码解锁Cmd"))
                {
                    if (_inputPassword.Equals(Password))
                    {
                        _beOpenCmd = true;
                        GUI.FocusControl("emptyLabel"); //移除焦点
                        if (_keyboard != null)
                        {
                            _keyboard.active = false;
                            _keyboard.text = "";
                            _hasFocus = false;
                        }
                    }
                    else
                    {
                        _passwordMsg = "<color=#ff0000>密码错误</color>";
                    }
                }

                GUILayout.Label(_passwordMsg);
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUI.SetNextControlName("InputCmd");
                var cmd = GUILayout.TextField(_inputCmd);
                SetTouchScreenKeyBoard("InputCmd");
                if (TouchScreenKeyboard.isSupported && _keyboard != null)
                {
                    cmd = _keyboard.text;
                }

                if (!string.IsNullOrEmpty(cmd))
                {
                    if (cmd != _inputCmd || _beForceFindCmd)
                    {
                        if (_beForceFindCmd)
                        {
                            // 设置光标
                            TextEditor te =
                                (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                            te.MoveTextEnd();
                        }

                        _resultMsg = "";
                        FindCmdInfo(cmd, ref _checkCmdInfos);
                        _beForceFindCmd = false;
                    }
                }
                else
                {
                    _checkCmdInfos.Clear();
                }

                _inputCmd = cmd;

                if (GUILayout.Button("执行Cmd"))
                {
                    GUI.FocusControl("emptyLabel"); //移除焦点到非输入框
                    _resultMsg = CmdExecute(cmd, out var beSuc);
                    if (beSuc)
                    {
                        _inputCmd = "";
                        if (_keyboard != null)
                        {
                            _keyboard.text = "";
                        }
                    }
                }

                GUILayout.EndHorizontal();
                _hintScrollPosition = GUILayout.BeginScrollView(_hintScrollPosition, GUILayout.Height(CmdHintBlockHeight));
                GUILayout.BeginVertical();
                if (_checkCmdInfos.Count >= 1)
                {
                    for (int i = 0; i < _checkCmdInfos.Count; i++)
                    {
                        GUILayout.Label($"{_checkCmdInfos[i].Cmd} {_checkCmdInfos[i].Msg}", GetHintLabelStyle());
                        if (i == 9) break; //最多显示10条命令提示
                    }
                }
                else if (!string.IsNullOrEmpty(cmd))
                {
                    GUILayout.Label($"<color=#ff0000>没有这个指令: {cmd}</color>", GetHintLabelStyle());
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndScrollView();

                DrawGroupList();
            }
        }

        private void DrawGroupList()
        {
            EnsureDefaultGroup();
            var groupNameLs = _cmdGroupDict.Keys.ToList();
            if (groupNameLs.Remove(DefaultGroupName))
            {
                groupNameLs.Insert(0, DefaultGroupName);
            }

            if (!groupNameLs.Contains(_selectedGroupName))
            {
                _selectedGroupName = DefaultGroupName;
            }

            var contentHeight = ComputeSplitPanelsHeight() - 130;
            var totalContentWidth = Mathf.Max(180f, m_DebuggerComponent.MWindowContentMaxWidth - 4f);
            var groupWidth = Mathf.Clamp(totalContentWidth * 0.28f, MinGroupListWidth, GroupListWidth);
            var rightContentWidth = Mathf.Max(100f, totalContentWidth - groupWidth - GroupPanelSpacing - GroupPanelSpacing);
            GUILayout.Space(SplitListTopSpace);
            GUILayout.BeginHorizontal(GUILayout.Width(totalContentWidth), GUILayout.ExpandWidth(false));
            {
                BeginTransparentPanel(groupWidth, contentHeight);
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("分组");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    PushGroupScrollbarNarrow(GUI.skin, GroupScrollbarWidthScale, out var scrollbarRestore);
                    _groupListScrollPosition = GUILayout.BeginScrollView(_groupListScrollPosition, GUILayout.ExpandHeight(true));
                    for (int i = 0; i < groupNameLs.Count; i++)
                    {
                        var groupName = groupNameLs[i];
                        bool isSelected = groupName.Equals(_selectedGroupName, StringComparison.Ordinal);
                        if (GUILayout.Toggle(isSelected, groupName, "Button") && !isSelected)
                        {
                            _selectedGroupName = groupName;
                        }
                    }

                    GUILayout.EndScrollView();
                    PopGroupScrollbarNarrow(scrollbarRestore);
                }
                EndTransparentPanel();

                GUILayout.Space(GroupPanelSpacing);
                BeginTransparentPanel(rightContentWidth, contentHeight);
                {
                    _btnListScrollPosition = GUILayout.BeginScrollView(_btnListScrollPosition, GUILayout.ExpandHeight(true));
                    DrawGroupCmdButtons(rightContentWidth);
                    GUILayout.EndScrollView();
                }
                EndTransparentPanel();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 按当前布局已占用高度计算分栏可用高度，避免固定估算偏小导致分栏底部超出窗口/屏幕。
        /// </summary>
        private float ComputeSplitPanelsHeight()
        {
            var maxClientH = m_DebuggerComponent.MWindowContentMaxHeight;
            var splitTopEstimate = CmdHintBlockHeight + CmdSplitFixedAboveHintBlock;
            var fallback = maxClientH - splitTopEstimate - SplitListTopSpace - SplitPanelsBottomSafety;

            var last = GUILayoutUtility.GetLastRect();
            if (last.height <= Mathf.Epsilon && last.y <= Mathf.Epsilon)
            {
                return Mathf.Max(60f, fallback);
            }

            var measured = maxClientH - last.yMax - SplitListTopSpace - SplitPanelsBottomSafety;
            if (measured < 20f || measured > maxClientH + 1f)
            {
                return Mathf.Max(60f, fallback);
            }

            return Mathf.Max(60f, measured);
        }

        private static void BeginTransparentPanel(float width, float height)
        {
            var oldColor = GUI.color;
            GUI.color = ScrollPanelColor;
            GUILayout.BeginVertical("box", GUILayout.Width(width), GUILayout.Height(height));
            GUI.color = oldColor;
        }

        private static void EndTransparentPanel()
        {
            GUILayout.EndVertical();
        }

        private static GUIStyle GetClipWrapLabelStyle()
        {
            if (_clipWrapLabelStyle == null)
            {
                _clipWrapLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    clipping = TextClipping.Clip
                };
            }

            return _clipWrapLabelStyle;
        }

        private static GUIStyle GetHintLabelStyle()
        {
            if (_hintLabelStyle == null)
            {
                _hintLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                    richText = true
                };
            }

            return _hintLabelStyle;
        }

        private struct ScrollbarStyleRestore
        {
            public float TrackFixedWidth;
            public float ThumbFixedWidth;
            public float UpFixedWidth;
            public float DownFixedWidth;
        }

        /// <summary>
        /// 缩小左侧滚动条时同时缩放轨道与滑块（thumb），否则仅改轨道宽度时滑块视觉宽度不会跟随。
        /// </summary>
        private static void PushGroupScrollbarNarrow(GUISkin skin, float scale, out ScrollbarStyleRestore restore)
        {
            var track = skin.verticalScrollbar;
            var thumb = skin.verticalScrollbarThumb;
            var up = skin.verticalScrollbarUpButton;
            var down = skin.verticalScrollbarDownButton;

            restore.TrackFixedWidth = track.fixedWidth;
            restore.ThumbFixedWidth = thumb.fixedWidth;
            restore.UpFixedWidth = up.fixedWidth;
            restore.DownFixedWidth = down.fixedWidth;

            var newTrackW = Mathf.Max(6f, restore.TrackFixedWidth * scale);
            track.fixedWidth = newTrackW;

            if (restore.ThumbFixedWidth > 0.01f)
            {
                thumb.fixedWidth = Mathf.Max(4f, restore.ThumbFixedWidth * scale);
            }
            else
            {
                thumb.fixedWidth = Mathf.Max(4f, newTrackW * 0.88f);
            }

            if (restore.UpFixedWidth > 0.01f)
            {
                up.fixedWidth = Mathf.Max(4f, restore.UpFixedWidth * scale);
            }

            if (restore.DownFixedWidth > 0.01f)
            {
                down.fixedWidth = Mathf.Max(4f, restore.DownFixedWidth * scale);
            }
        }

        private static void PopGroupScrollbarNarrow(ScrollbarStyleRestore restore)
        {
            var skin = GUI.skin;
            skin.verticalScrollbar.fixedWidth = restore.TrackFixedWidth;
            skin.verticalScrollbarThumb.fixedWidth = restore.ThumbFixedWidth;
            skin.verticalScrollbarUpButton.fixedWidth = restore.UpFixedWidth;
            skin.verticalScrollbarDownButton.fixedWidth = restore.DownFixedWidth;
        }

        private void DrawGroupCmdButtons(float viewWidth)
        {
            if (!_cmdGroupDict.TryGetValue(_selectedGroupName, out var cmdInfos) || cmdInfos.Count == 0)
            {
                GUILayout.Label("<color=#808080>当前分组暂无命令</color>");
                return;
            }

            bool hasButton = false;
            var buttonInfos = cmdInfos.Where(x => !string.IsNullOrEmpty(x.Button)).ToList();
            int columns = Mathf.Max(1, Mathf.FloorToInt((viewWidth - 20f) / ButtonMinWidth));
            columns = Mathf.Min(columns, 4);
            for (int i = 0; i < buttonInfos.Count; i += columns)
            {
                GUILayout.BeginHorizontal();
                int end = Mathf.Min(i + columns, buttonInfos.Count);
                for (int j = i; j < end; j++)
                {
                    var cmdInfo = buttonInfos[j];
                    hasButton = true;
                    if (!GUILayout.Button(cmdInfo.Button, GUILayout.ExpandWidth(true)))
                    {
                        continue;
                    }

                    if (cmdInfo.Delegate is UnityAction)
                    {
                        GUI.FocusControl("emptyLabel"); //移除焦点到非输入框
                        _resultMsg = CmdExecute(cmdInfo.Cmd, out var beSuc);
                        if (beSuc)
                        {
                            _inputCmd = "";
                            if (_keyboard != null)
                            {
                                _keyboard.text = "";
                            }
                        }
                    }
                    else
                    {
                        GUI.FocusControl("InputCmd");
                        _inputCmd = cmdInfo.Cmd + " ";
                        _beForceFindCmd = true;
                        if (TouchScreenKeyboard.isSupported && _keyboard != null)
                        {
                            _keyboard.text = _inputCmd;
                        }
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }

            if (!hasButton)
            {
                GUILayout.Label("<color=#808080>当前分组暂无快捷按钮</color>");
            }
        }

        private void EnsureDefaultGroup()
        {
            if (!_cmdGroupDict.ContainsKey(DefaultGroupName))
            {
                _cmdGroupDict[DefaultGroupName] = new List<CmdInfo>();
            }

            if (string.IsNullOrEmpty(_selectedGroupName) || !_cmdGroupDict.ContainsKey(_selectedGroupName))
            {
                _selectedGroupName = DefaultGroupName;
            }
        }

        private void FindCmdInfo(string cmd, ref List<CmdInfo> infos)
        {
            infos.Clear();
            var cmdArray = cmd.Split(' ');
            if (cmdArray.Length <= 0)
            {
                return;
            }

            bool needFull = cmdArray.Length > 1;
            var query = cmdArray[0].ToLower();
            foreach (var cmdInfo in _cmdGroupDict.Values.SelectMany(x => x))
            {
                if (needFull)
                {
                    if (cmdInfo.Cmd.ToLower().Equals(query))
                    {
                        infos.Add(cmdInfo);
                    }
                }
                else
                {
                    if (cmdInfo.Cmd.ToLower().StartsWith(query))
                    {
                        infos.Add(cmdInfo);
                    }
                }
            }
        }

        public string CmdExecute(string cmd, out bool beSuc)
        {
            beSuc = false;
            var cmdArray = cmd.Split(' ');
            if (cmdArray.Length <= 0)
            {
                return $"<color=#ff0000>{DateTime.Now:MM.dd HH:mm:ss:fff} 无效的cmd:{cmd}</color>";
            }

            var info = _cmdGroupDict.Values
                .SelectMany(x => x)
                .FirstOrDefault(x => x.Cmd.ToLower().Equals(cmdArray[0].ToLower()));
            if (info == null)
            {
                return $"<color=#ff0000>{DateTime.Now:MM.dd HH:mm:ss:fff} 无效的cmd:{cmd}</color>";
            }

            var cmdList = cmdArray.ToList();
            cmdList.RemoveAll(string.IsNullOrEmpty);
            cmdList.RemoveAt(0);

            object[] args = new object[cmdList.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = cmdList[i];
            }

            try
            {
                info.Delegate.DynamicInvoke(args);
            }
            catch (TargetParameterCountException)
            {
                var count = info.Delegate.Method.GetParameters().Length;
                return $"<color=#ff0000>{DateTime.Now:MM.dd HH:mm:ss:fff} 参数数量不正确 应该为{count} 但获得{args.Length}</color>";
            }
            catch (Exception e)
            {
                return e.Message;
            }

            beSuc = true;
            return $"{DateTime.Now:MM.dd HH:mm:ss:fff} 执行完成 {cmd} - {info.Button}";
        }

        /// <summary>
        /// 移除某个cmd的指令
        /// </summary>
        /// <param name="cmd"></param>
        internal void RemoveCmd(string cmd)
        {
            if (_cmdGroupNameDict.TryGetValue(cmd, out var groupName) && _cmdGroupDict.TryGetValue(groupName, out var cmdLs))
            {
                cmdLs.RemoveAll(x => x.Cmd.Equals(cmd));
                _cmdGroupNameDict.Remove(cmd);
            }
        }
    }
}