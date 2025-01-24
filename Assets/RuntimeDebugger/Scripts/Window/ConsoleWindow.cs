using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    public class ConsoleWindow : IDebuggerWindow
    {
        //直接使用可反向循环的双链表， 避免许多地方foreach就没有GC 消耗
        private readonly LogNode _mLogNodes = new LogNode();
        private readonly List<LogNode> _mFilterLogNodesList = new List<LogNode>(1000);
        private readonly LogNode _mFilterLogNodes = new LogNode();
        private LogNode _mFilterCurLogNodes;


        private Vector2 _mLogScrollPosition, _mStackScrollPosition = Vector2.zero;
        private int _mInfoCount, _mWarningCount, _mErrorCount, _mFatalCount;
        private LogNode _mSelectedNode;

        private bool _mLastLockScroll,
            _mLastInfoFilter,
            _mLastWarningFilter,
            _mLastErrorFilter,
            _mLastFatalFilter,
            _mInfoFilter,
            _mWarningFilter,
            _mErrorFilter,
            _mFatalFilter = true;

        private string _searchString = "";
        private string _oldString = "";
        private Regex _reg = new Regex("");
        private readonly StringBuilder _sb = new StringBuilder();
        protected DebuggerMgr MDebuggerComponent;

        private bool _mLockScroll = true;

        private int _mMaxLine = 100;


        private Color32 _mInfoColor = Color.white;

        private Color32 _mWarningColor = Color.yellow;

        private Color32 _mErrorColor = Color.red;

        private Color32 _mFatalColor = new Color(0.7f, 0.2f, 0.2f);

        private Vector2 _touchFirst = Vector2.zero; //手指开始按下的位置
        private Vector2 _touchSecond = Vector2.zero; //手指拖动的位置


        private const float ElementHeight = 20.0f;
        private float _mScrollPosition;
        private float _mMaxScrollPosition;
        private bool _mSelected;

        private readonly (bool, string)[] _mToggleInfo = new[]
        {
            (true, "Lock Scroll"), (true, "Info ({0})"), (true, "Warning ({0})"), (true, "Error ({0})"),
            (true, "Fatal ({0})")
        };

        private float _mTopHeight = 154;
        private static readonly float MinLogViewHeight = 150f;

        public bool LockScroll
        {
            get => _mLockScroll;
            set => _mLockScroll = value;
        }

        public int MaxLine
        {
            get => _mMaxLine;
            set => _mMaxLine = value;
        }

        public bool InfoFilter
        {
            get => _mInfoFilter;
            set => _mInfoFilter = value;
        }

        public bool WarningFilter
        {
            get => _mWarningFilter;
            set => _mWarningFilter = value;
        }

        public bool ErrorFilter
        {
            get => _mErrorFilter;
            set => _mErrorFilter = value;
        }

        public bool FatalFilter
        {
            get => _mFatalFilter;
            set => _mFatalFilter = value;
        }

        public int InfoCount => _mInfoCount;

        public int WarningCount => _mWarningCount;

        public int ErrorCount => _mErrorCount;

        public int FatalCount => _mFatalCount;

        public Color32 InfoColor
        {
            get => _mInfoColor;
            set => _mInfoColor = value;
        }

        public Color32 WarningColor
        {
            get => _mWarningColor;
            set => _mWarningColor = value;
        }

        public Color32 ErrorColor
        {
            get => _mErrorColor;
            set => _mErrorColor = value;
        }

        public Color32 FatalColor
        {
            get => _mFatalColor;
            set => _mFatalColor = value;
        }

        private bool _debuggerConsoleLockScroll = true,
            _debuggerConsoleInfoFilter = true,
            _debuggerConsoleWarningFilter = true,
            _debuggerConsoleErrorFilter = true,
            _debuggerConsoleFatalFilter = true;

        public void Initialize(DebuggerMgr debuggerMgr, params object[] args)
        {
            MDebuggerComponent = debuggerMgr;
            if (MDebuggerComponent == null)
            {
                Debug.LogError("Debugger component is invalid.");
                return;
            }


            Application.logMessageReceived += OnLogMessageReceived;

            _mLockScroll = _mLastLockScroll = this._debuggerConsoleLockScroll;
            _mInfoFilter = _mLastInfoFilter = _debuggerConsoleInfoFilter;
            _mWarningFilter = _mLastWarningFilter = _debuggerConsoleWarningFilter;
            _mErrorFilter = _mLastErrorFilter = _debuggerConsoleErrorFilter;
            _mFatalFilter = _mLastFatalFilter = _debuggerConsoleFatalFilter;
        }

        public void Shutdown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            Clear();
        }

        public void OnEnter()
        {
        }

        public void OnLeave()
        {
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (_mLastLockScroll != _mLockScroll)
            {
                _mLastLockScroll = _mLockScroll;
                _debuggerConsoleLockScroll = _mLockScroll;
            }

            if (_mLastInfoFilter != _mInfoFilter)
            {
                _mLastInfoFilter = _mInfoFilter;
                _debuggerConsoleInfoFilter = _mInfoFilter;

                OnSearchStringChange();
            }

            if (_mLastWarningFilter != _mWarningFilter)
            {
                _mLastWarningFilter = _mWarningFilter;
                _debuggerConsoleWarningFilter = _mWarningFilter;

                OnSearchStringChange();
            }

            if (_mLastErrorFilter != _mErrorFilter)
            {
                _mLastErrorFilter = _mErrorFilter;
                _debuggerConsoleErrorFilter = _mErrorFilter;
                OnSearchStringChange();
            }

            if (_mLastFatalFilter != _mFatalFilter)
            {
                _mLastFatalFilter = _mFatalFilter;
                _debuggerConsoleFatalFilter = _mFatalFilter;

                OnSearchStringChange();
            }
        }

        public void OnDraw()
        {
            RefreshCount();

            // m_LockScroll = GUILayout.Toggle(m_LockScroll, "Lock Scroll", GUILayout.Width(90f));
            // GUILayout.FlexibleSpace();
            // m_InfoFilter = GUILayout.Toggle(m_InfoFilter, Utility.Text.Format("Info ({0})", m_InfoCount), GUILayout.Width(90f));
            // m_WarningFilter = GUILayout.Toggle(m_WarningFilter, Utility.Text.Format("Warning ({0})", m_WarningCount), GUILayout.Width(90f));
            // m_ErrorFilter = GUILayout.Toggle(m_ErrorFilter, Utility.Text.Format("Error ({0})", m_ErrorCount), GUILayout.Width(90f));
            // m_FatalFilter = GUILayout.Toggle(m_FatalFilter, Utility.Text.Format("Fatal ({0})", m_FatalCount), GUILayout.Width(90f));


            _mToggleInfo[0].Item1 = _mLockScroll;
            _mToggleInfo[1].Item1 = _mInfoFilter;
            _mToggleInfo[2].Item1 = _mWarningFilter;
            _mToggleInfo[3].Item1 = _mErrorFilter;
            _mToggleInfo[4].Item1 = _mFatalFilter;
            _mToggleInfo[1].Item2 = $"Info ({_mInfoCount})";
            _mToggleInfo[2].Item2 = $"Warning ({_mWarningCount})";
            _mToggleInfo[3].Item2 = $"Error ({_mErrorCount})";
            _mToggleInfo[4].Item2 = $"Fatal ({_mFatalCount})";
            float maxW = MDebuggerComponent.MWindowContentMaxWidth - MDebuggerComponent.MSkin.toggle.margin.left -
                         MDebuggerComponent.MSkin.toggle.margin.right;
            float maxH = MDebuggerComponent.MWindowContentMaxHeight - 60f;
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                int useWidth = 0;
                for (int i = 0; i < _mToggleInfo.Length; i++)
                {
                    useWidth += 90;
                    if (useWidth > maxW)
                    {
                        if (useWidth > 90) GUILayout.EndHorizontal(); // 如果是行的第一个 Toggle，结束上一行的布局
                        GUILayout.BeginHorizontal(); // 开始新的一行布局
                    }

                    _mToggleInfo[i].Item1 = GUILayout.Toggle(_mToggleInfo[i].Item1, _mToggleInfo[i].Item2);
                }

                _mLockScroll = _mToggleInfo[0].Item1;
                _mInfoFilter = _mToggleInfo[1].Item1;
                _mWarningFilter = _mToggleInfo[2].Item1;
                _mErrorFilter = _mToggleInfo[3].Item1;
                _mFatalFilter = _mToggleInfo[4].Item1;

                GUILayout.EndHorizontal(); // 结束最后一行的布局
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            {
                _searchString = GUILayout.TextField(_searchString);
                if (!_oldString.Equals(_searchString))
                {
                    OnSearchStringChange();
                }

                if (GUILayout.Button("Clear All", GUILayout.Width(90f)))
                {
                    Clear();
                }

                if (GUILayout.Button("Export", GUILayout.Width(90f)))
                {
                    ExportLog();
                }
            }
            GUILayout.EndHorizontal();

            Rect lastRect = GUILayoutUtility.GetLastRect();
            if (lastRect.y + lastRect.height > _mTopHeight) _mTopHeight = lastRect.y + lastRect.height;
            GUILayout.BeginVertical("box");
            {
                //旧版的实现方式，有多少条就显示多少日志，太卡
                //DrawLogView();

                //新版的实现方式，只显示可视区域内的
                //var boxRect = EditorUILayout.GetControlRect(GUILayout.Height(MinLogViewHeight));
                float viewH = maxH - _mTopHeight - 140f;
                if (viewH < MinLogViewHeight) viewH = MinLogViewHeight;
                Rect boxRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(viewH));
                DrawLogViewEx(boxRect);
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal("box");
            {
                _mStackScrollPosition = GUILayout.BeginScrollView(_mStackScrollPosition, GUILayout.Height(140f));
                {
                    if (_mSelectedNode != null)
                    {
                        Color32 color = GetLogStringColor(_mSelectedNode.LogType);
                        if (GUILayout.Button(
                                string.Format("<color=#{0:x2}{1:x2}{2:x2}{3:x2}><b>{4}</b></color>{6}{6}{5}",
                                    color.r, color.g, color.b, color.a, _mSelectedNode.LogMessage,
                                    _mSelectedNode.StackTrack, Environment.NewLine), "label"))
                        {
                            DebuggerMgr.CopyToClipboard(string.Format("{0}{2}{2}{1}", _mSelectedNode.LogMessage,
                                _mSelectedNode.StackTrack, Environment.NewLine));
                        }
                        // if (GUILayout.Toggle(false, Utility.Text.Format("<color=#{0:x2}{1:x2}{2:x2}{3:x2}><b>{4}</b></color>{6}{6}{5}", color.r, color.g, color.b, color.a, m_SelectedNode.LogMessage, m_SelectedNode.StackTrack, Environment.NewLine)))
                        // {
                        //     CopyToClipboard(Utility.Text.Format("{0}{2}{2}{1}", m_SelectedNode.LogMessage, m_SelectedNode.StackTrack, Environment.NewLine));
                        // }
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.Box("", GUILayout.Width(100f), GUILayout.Height(140f));
            }
            GUILayout.EndHorizontal();


            if (Event.current.type != EventType.Layout)
            {
                // Debug.Log("--------" + Event.current.type.ToString());
            }

            if (Event.current.type == EventType.MouseDown) //判断当前手指是按下事件
            {
                // Debug.Log("--------touchFirst " + Event.current.type.ToString());
                _touchFirst = Event.current.mousePosition; //记录开始按下的位置
            }

            if (Event.current.type == EventType.MouseDrag)
                //判断当前手指是拖动事件
            {
                _touchSecond = Event.current.mousePosition;

                if (_touchSecond.y > _touchFirst.y)
                    //拖动的位置比按下的位置y大  (向下滑动)
                {
                    if (_mSelectedNode != null)
                    {
                        _mStackScrollPosition.y += (_touchFirst.y - _touchSecond.y);
                    }
                    else if (!_mLockScroll)
                    {
                        _mScrollPosition = Mathf.Min(_mMaxScrollPosition,
                            _mScrollPosition + _touchFirst.y - _touchSecond.y);
                        _mLogScrollPosition.y += (_touchFirst.y - _touchSecond.y);
                    }
                }
                else if (_touchSecond.y < _touchFirst.y)
                    //拖动的位置比按下的位置y小  (向上滑动)
                {
                    if (_mSelectedNode != null)
                    {
                        _mStackScrollPosition.y += (_touchFirst.y - _touchSecond.y);
                    }
                    else if (!_mLockScroll)
                    {
                        _mScrollPosition = Mathf.Min(_mMaxScrollPosition,
                            _mScrollPosition + _touchFirst.y - _touchSecond.y);
                        _mLogScrollPosition.y += (_touchFirst.y - _touchSecond.y);
                    }
                }

                if (_touchSecond.x > _touchFirst.x)
                    //拖动的位置比按下的位置x大  (向右滑动)
                {
                    if (_mSelectedNode != null)
                    {
                        _mStackScrollPosition.x += (_touchFirst.x - _touchSecond.x);
                    }
                    else if (!_mLockScroll)
                    {
                        _mLogScrollPosition.x += (_touchFirst.x - _touchSecond.x);
                    }
                }
                else if (_touchSecond.x < _touchFirst.x)
                    //拖动的位置比按下的位置x小  (向左滑动)
                {
                    if (_mSelectedNode != null)
                    {
                        _mStackScrollPosition.x += (_touchFirst.x - _touchSecond.x);
                    }
                    else if (!_mLockScroll)
                    {
                        _mLogScrollPosition.x += (_touchFirst.x - _touchSecond.x);
                    }
                }

                _touchFirst = _touchSecond; //初始化位置
            }
        }

        private void DrawLogView()
        {
            if (_mLockScroll)
            {
                _mLogScrollPosition.y = float.MaxValue;
            }

            _mLogScrollPosition = GUILayout.BeginScrollView(_mLogScrollPosition);
            {
                bool selected = false;
                LogNode logNode = _mFilterLogNodes.FilterNext;
                while (logNode != null)
                {
                    GUI.color = logNode.Color;
                    if (GUILayout.Toggle(_mSelectedNode == logNode, logNode.MyText, GUILayout.Height(20)))
                    {
                        selected = true;
                        if (_mSelectedNode != logNode)
                        {
                            _mSelectedNode = logNode;
                            _mStackScrollPosition = Vector2.zero;
                        }
                    }

                    GUI.color = Color.white;
                    logNode = logNode.FilterNext;
                }

                if (!selected)
                {
                    _mSelectedNode = null;
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawLogViewEx(Rect boxRect)
        {
            // only use half space of this shit window
            int gap = 5;
            Rect viewportRect = new Rect(boxRect.x, boxRect.y, boxRect.width, boxRect.height);

            float scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth;
            Rect scrollbarRect = new Rect(viewportRect.x + viewportRect.width - scrollbarWidth, viewportRect.y,
                scrollbarWidth, viewportRect.height);
            Rect currentRect = new Rect(boxRect.x + gap, boxRect.y, viewportRect.width - scrollbarWidth,
                viewportRect.height - gap);
            float viewportHeight = viewportRect.height;
            int elementCount = _mFilterLogNodesList.Count;

            GUI.BeginClip(currentRect); // to clip the overflow stuff
            int indexOffset = Mathf.FloorToInt(_mScrollPosition / ElementHeight);
            int showCount = Mathf.CeilToInt(currentRect.height / ElementHeight);
            showCount = showCount > elementCount ? elementCount : showCount;
            float startPosY = (indexOffset * ElementHeight) - _mScrollPosition;

            for (int i = 0; i < showCount; i++)
            {
                Rect elementRect = new Rect(0, 0 + startPosY + i * ElementHeight, currentRect.width, ElementHeight);
                DrawTempElement(elementRect, indexOffset + i);
            }

            GUI.EndClip();

            // do stuff for scroller
            float fullElementHeight = elementCount * ElementHeight;
            if (Math.Abs(scrollbarRect.height - 1) > 0.001f)
            {
                _mMaxScrollPosition = fullElementHeight - scrollbarRect.height;
                if (_mLockScroll)
                {
                    _mScrollPosition = _mMaxScrollPosition;
                }
            }

            _mScrollPosition = Mathf.Max(0,
                GUI.VerticalScrollbar(scrollbarRect, _mScrollPosition, currentRect.height, 0,
                    Mathf.Max(fullElementHeight, currentRect.height)));

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            float scrollSensitivity = ElementHeight;
            float maxScrollPos =
                (fullElementHeight > currentRect.height) ? (fullElementHeight - currentRect.height) : 0;

            if (EventType.ScrollWheel == Event.current.GetTypeForControl(controlId))
            {
                _mScrollPosition = Mathf.Clamp(_mScrollPosition + Event.current.delta.y * scrollSensitivity, 0,
                    maxScrollPos);
                Event.current.Use();
            }
        }

        private void DrawTempElement(Rect elementRect, int dataIndex)
        {
            // Rect iconRect = new Rect(elementRect.x, elementRect.y, ELEMENT_ICON_SIZE, elementRect.height);
            // if (m_data[dataIndex].IconTag == 0)
            //     GUI.Label(iconRect, m_infoIconSmall);
            // else
            //     GUI.Label(iconRect, m_warningIconSmall);

            LogNode logNode = _mFilterLogNodesList[dataIndex];
            Rect labelRect = new Rect(elementRect.x, elementRect.y, elementRect.width, elementRect.height);
            // GUI.Label(labelRect, m_FilterLogNodes[dataIndex].text);
            GUI.color = logNode.Color;

            // if (GUILayout.Toggle(m_SelectedNode == logNode, logNode.text, GUILayout.Height(elementRect.height)))
            if (GUI.Toggle(labelRect, (_mSelectedNode == logNode), logNode.MyText))
            {
                _mSelected = true;
                if (_mSelectedNode != logNode)
                {
                    _mSelectedNode = logNode;
                    _mStackScrollPosition = Vector2.zero;
                }
            }
            else
            {
                if (_mSelectedNode == logNode)
                {
                    _mSelected = false;
                    _mSelectedNode = null;
                }
            }

            GUI.color = Color.white;
        }

        private void OnSearchStringChange()
        {
            try
            {
                _reg = new Regex(_searchString, RegexOptions.IgnoreCase);
                _oldString = _searchString;
                _mFilterLogNodesList.Clear();
                _mFilterLogNodes.Clear();
                _mScrollPosition = 0;

                LogNode filterNode = _mFilterLogNodes;
                LogNode node = _mLogNodes.Next;
                while (node != null)
                {
                    LogNode retNode = CheckFilterNode(filterNode, node);
                    if (retNode != null)
                    {
                        filterNode = retNode;
                    }

                    node = node.Next;
                }
            }
            catch
            {
                // reg = new Regex(oldString, RegexOptions.IgnoreCase);
            }
        }

        private LogNode CheckFilterNode(LogNode filterNode, LogNode checkTargetNode)
        {
            bool beFilter = false;
            switch (checkTargetNode.LogType)
            {
                case LogType.Log:
                    if (!_mInfoFilter)
                    {
                        beFilter = true;
                    }

                    break;

                case LogType.Warning:
                    if (!_mWarningFilter)
                    {
                        beFilter = true;
                    }

                    break;

                case LogType.Error:
                    if (!_mErrorFilter)
                    {
                        beFilter = true;
                    }

                    break;

                case LogType.Exception:
                    if (!_mFatalFilter)
                    {
                        beFilter = true;
                    }

                    break;
            }

            if (!beFilter && _reg.IsMatch(checkTargetNode.LogMessage))
            {
                _mFilterLogNodesList.Add(checkTargetNode);
                filterNode.FilterNext = checkTargetNode;
                checkTargetNode.FilterNext = null;
                _mFilterCurLogNodes = checkTargetNode;
                ++_mFilterLogNodes.Size;
                return checkTargetNode;
            }

            return null;
        }

        public void ExportLog()
        {
            var str = "";
            LogNode logNode = _mLogNodes.Next;
            while (logNode != null)
            {
                str += $"UTC {logNode.LogTime}\n{logNode.LogMessage}\n{logNode.StackTrack}\n\n";
                logNode = logNode.Next;
            }

            var path = Application.persistentDataPath + "/logs.txt";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var file = File.Create(path);
            var ar = Encoding.UTF8.GetBytes(str);
            file.Write(ar, 0, ar.Length);
            file.Close();
            GUIUtility.systemCopyBuffer = str;
        }

        private void Clear()
        {
            LogNode node = _mLogNodes.Next;
            while (node != null)
            {
                node.Clear();
                LogNode.Pool.Release(node);
                node = node.Next;
            }

            _mLogNodes.Clear();
            _mFilterLogNodesList.Clear();
            _mFilterLogNodes.Clear();
            _mFilterCurLogNodes = null;
        }

        public void RefreshCount()
        {
            _mInfoCount = 0;
            _mWarningCount = 0;
            _mErrorCount = 0;
            _mFatalCount = 0;
            LogNode logNode = _mLogNodes.Next;
            while (logNode != null)
            {
                switch (logNode.LogType)
                {
                    case LogType.Log:
                        _mInfoCount++;
                        break;

                    case LogType.Warning:
                        _mWarningCount++;
                        break;

                    case LogType.Error:
                        _mErrorCount++;
                        break;

                    case LogType.Exception:
                        _mFatalCount++;
                        break;
                }

                logNode = logNode.Next;
            }
        }

        public void GetRecentLogs(List<LogNode> results)
        {
            if (results == null)
            {
                Debug.LogError("Results is invalid.");
                return;
            }

            results.Clear();
            LogNode node = _mLogNodes.Next;
            while (node != null)
            {
                results.Add(node);
                node = node.Next;
            }
        }

        public void GetRecentLogs(List<LogNode> results, int count)
        {
            if (results == null)
            {
                Debug.LogError("Results is invalid.");
                return;
            }

            if (count <= 0)
            {
                Debug.LogError("Count is invalid.");
                return;
            }

            int size = 0;
            results.Clear();
            LogNode node = _mLogNodes.End;
            while (node != null)
            {
                results.Add(node);
                node = node.Pre;
                ++size;
                if (size >= count) break;
            }
        }

        private void OnLogMessageReceived(string logMessage, string stackTrace, LogType logType)
        {
            if (logType == LogType.Assert)
            {
                logType = LogType.Error;
            }

            //如果没有超过上限，则直接添加，如果超过上限则移动箭头，让箭头前一个作为新的
            LogNode node = null;
            if (_mLogNodes.Size >= _mMaxLine)
            {
                node = _mLogNodes.Next;
                _mLogNodes.Next = node.Next;
                node.Next.Pre = _mLogNodes;
                node.Reset(logType, logMessage, stackTrace);
                _mLogNodes.Size--;
            }

            if (node == null) node = LogNode.Create(logType, logMessage, stackTrace);
            node.Color = GetLogStringColor(logType);
            _sb.Clear();
            _sb.Append("UTC ");
            _sb.Append(node.LogTime);
            _sb.Append(" ");
            _sb.Append(node.LogFrameCount);
            _sb.Append(" ");
            _sb.Append(node.LogMessage);
            node.MyText = _sb.ToString();

            LogNode curNode = _mLogNodes.End == null ? _mLogNodes : _mLogNodes.End;
            curNode.Next = node;
            node.Pre = curNode;
            _mLogNodes.End = node;
            ++_mLogNodes.Size;

            if (_reg.IsMatch(logMessage))
            {
                LogNode curFilterNode = _mFilterCurLogNodes == null ? _mFilterLogNodes : _mFilterCurLogNodes;
                CheckFilterNode(curFilterNode, node);
            }
        }


        internal Color32 GetLogStringColor(LogType logType)
        {
            Color32 color = Color.white;
            switch (logType)
            {
                case LogType.Log:
                    color = _mInfoColor;
                    break;

                case LogType.Warning:
                    color = _mWarningColor;
                    break;

                case LogType.Error:
                    color = _mErrorColor;
                    break;

                case LogType.Exception:
                    color = _mFatalColor;
                    break;
            }

            return color;
        }
    }
}