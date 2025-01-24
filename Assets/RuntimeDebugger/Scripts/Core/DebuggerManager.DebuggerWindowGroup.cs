﻿using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    internal sealed partial class DebuggerWindowWindowManager
    {
        /// <summary>
        /// 调试器窗口组。
        /// </summary>
        private sealed class DebuggerWindowGroup : IDebuggerWindowGroup
        {
            private readonly List<KeyValuePair<string, IDebuggerWindow>> _debuggerWindows = new List<KeyValuePair<string, IDebuggerWindow>>();
            private int _selectedIndex;
            private string[] _debuggerWindowNames;

            /// <summary>
            /// 获取调试器窗口数量。
            /// </summary>
            public int DebuggerWindowCount => _debuggerWindows.Count;

            /// <summary>
            /// 获取或设置当前选中的调试器窗口索引。
            /// </summary>
            public int SelectedIndex
            {
                get => _selectedIndex;
                set => _selectedIndex = value;
            }

            /// <summary>
            /// 获取当前选中的调试器窗口。
            /// </summary>
            public IDebuggerWindow SelectedWindow
            {
                get
                {
                    if (_selectedIndex >= _debuggerWindows.Count)
                    {
                        return null;
                    }

                    return _debuggerWindows[_selectedIndex].Value;
                }
            }

            /// <summary>
            /// 初始化调试组。
            /// </summary>
            /// <param name="mgr"></param>
            /// <param name="args">初始化调试组参数。</param>
            public void Initialize(DebuggerMgr mgr, params object[] args)
            {
            }


            /// <summary>
            /// 关闭调试组。
            /// </summary>
            public void Shutdown()
            {
                foreach (KeyValuePair<string, IDebuggerWindow> debuggerWindow in _debuggerWindows)
                {
                    debuggerWindow.Value.Shutdown();
                }

                _debuggerWindows.Clear();
            }

            /// <summary>
            /// 进入调试器窗口。
            /// </summary>
            public void OnEnter()
            {
                SelectedWindow.OnEnter();
            }

            /// <summary>
            /// 离开调试器窗口。
            /// </summary>
            public void OnLeave()
            {
                SelectedWindow.OnLeave();
            }

            /// <summary>
            /// 调试组轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                SelectedWindow.OnUpdate(elapseSeconds, realElapseSeconds);
            }

            /// <summary>
            /// 调试器窗口绘制。
            /// </summary>
            public void OnDraw()
            {
            }

            private void RefreshDebuggerWindowNames()
            {
                int index = 0;
                _debuggerWindowNames = new string[_debuggerWindows.Count];
                foreach (KeyValuePair<string, IDebuggerWindow> debuggerWindow in _debuggerWindows)
                {
                    _debuggerWindowNames[index++] = debuggerWindow.Key;
                }
            }

            /// <summary>
            /// 获取调试组的调试器窗口名称集合。
            /// </summary>
            public string[] GetDebuggerWindowNames()
            {
                return _debuggerWindowNames;
            }

            /// <summary>
            /// 获取调试器窗口。
            /// </summary>
            /// <param name="path">调试器窗口路径。</param>
            /// <returns>要获取的调试器窗口。</returns>
            public IDebuggerWindow GetDebuggerWindow(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                int pos = path.IndexOf('/');
                if (pos < 0 || pos >= path.Length - 1)
                {
                    return InternalGetDebuggerWindow(path);
                }

                string debuggerWindowGroupName = path.Substring(0, pos);
                string leftPath = path.Substring(pos + 1);
                DebuggerWindowGroup debuggerWindowGroup =
                    (DebuggerWindowGroup)InternalGetDebuggerWindow(debuggerWindowGroupName);
                if (debuggerWindowGroup == null)
                {
                    return null;
                }

                return debuggerWindowGroup.GetDebuggerWindow(leftPath);
            }

            /// <summary>
            /// 选中调试器窗口。
            /// </summary>
            /// <param name="path">调试器窗口路径。</param>
            /// <returns>是否成功选中调试器窗口。</returns>
            public bool SelectDebuggerWindow(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                int pos = path.IndexOf('/');
                if (pos < 0 || pos >= path.Length - 1)
                {
                    return InternalSelectDebuggerWindow(path);
                }

                string debuggerWindowGroupName = path.Substring(0, pos);
                string leftPath = path.Substring(pos + 1);
                DebuggerWindowGroup debuggerWindowGroup =
                    (DebuggerWindowGroup)InternalGetDebuggerWindow(debuggerWindowGroupName);
                if (debuggerWindowGroup == null || !InternalSelectDebuggerWindow(debuggerWindowGroupName))
                {
                    return false;
                }

                return debuggerWindowGroup.SelectDebuggerWindow(leftPath);
            }

            /// <summary>
            /// 是否已注册调试器窗口。
            /// </summary>
            /// <param name="path">调试器窗口路径。</param>
            public bool ContainsDebuggerWindow(string path)
            {
                int pos = path.IndexOf('/');
                if (pos < 0 || pos >= path.Length - 1)
                {
                    if (InternalGetDebuggerWindow(path) != null)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// 注册调试器窗口。
            /// </summary>
            /// <param name="path">调试器窗口路径。</param>
            /// <param name="debuggerWindow">要注册的调试器窗口。</param>
            public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow)
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new Exception("Path is invalid.");
                }

                int pos = path.IndexOf('/');
                if (pos < 0 || pos >= path.Length - 1)
                {
                    if (InternalGetDebuggerWindow(path) != null)
                    {
                        throw new Exception("Debugger window has been registered.");
                    }

                    _debuggerWindows.Add(new KeyValuePair<string, IDebuggerWindow>(path, debuggerWindow));
                    RefreshDebuggerWindowNames();
                }
                else
                {
                    string debuggerWindowGroupName = path.Substring(0, pos);
                    string leftPath = path.Substring(pos + 1);
                    DebuggerWindowGroup debuggerWindowGroup =
                        (DebuggerWindowGroup)InternalGetDebuggerWindow(debuggerWindowGroupName);
                    if (debuggerWindowGroup == null)
                    {
                        if (InternalGetDebuggerWindow(debuggerWindowGroupName) != null)
                        {
                            throw new Exception(
                                "Debugger window has been registered, can not create debugger window group.");
                        }

                        debuggerWindowGroup = new DebuggerWindowGroup();
                        _debuggerWindows.Add(
                            new KeyValuePair<string, IDebuggerWindow>(debuggerWindowGroupName, debuggerWindowGroup));
                        RefreshDebuggerWindowNames();
                    }

                    debuggerWindowGroup.RegisterDebuggerWindow(leftPath, debuggerWindow);
                }
            }

            /// <summary>
            /// 解除注册调试器窗口。
            /// </summary>
            /// <param name="path">调试器窗口路径。</param>
            /// <returns>是否解除注册调试器窗口成功。</returns>
            public bool UnregisterDebuggerWindow(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                int pos = path.IndexOf('/');
                if (pos < 0 || pos >= path.Length - 1)
                {
                    IDebuggerWindow debuggerWindow = InternalGetDebuggerWindow(path);
                    if (debuggerWindow != null)
                    {
                        bool result =
                            _debuggerWindows.Remove(new KeyValuePair<string, IDebuggerWindow>(path, debuggerWindow));
                        debuggerWindow.Shutdown();
                        RefreshDebuggerWindowNames();
                        return result;
                    }

                    return false;
                }

                string debuggerWindowGroupName = path.Substring(0, pos);
                string leftPath = path.Substring(pos + 1);
                DebuggerWindowGroup debuggerWindowGroup =
                    (DebuggerWindowGroup)InternalGetDebuggerWindow(debuggerWindowGroupName);
                if (debuggerWindowGroup == null)
                {
                    return false;
                }

                return debuggerWindowGroup.UnregisterDebuggerWindow(leftPath);
            }

            private IDebuggerWindow InternalGetDebuggerWindow(string name)
            {
                foreach (KeyValuePair<string, IDebuggerWindow> debuggerWindow in _debuggerWindows)
                {
                    if (debuggerWindow.Key == name)
                    {
                        return debuggerWindow.Value;
                    }
                }

                return null;
            }

            private bool InternalSelectDebuggerWindow(string name)
            {
                for (int i = 0; i < _debuggerWindows.Count; i++)
                {
                    if (_debuggerWindows[i].Key == name)
                    {
                        _selectedIndex = i;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}