using System;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    /// <summary>
    /// 调试器管理器。
    /// </summary>
    [Preserve]
    internal sealed partial class DebuggerWindowWindowManager : IDebuggerWindowManager
    {
        private readonly DebuggerWindowGroup _debuggerWindowRoot;
        private bool _beActiveWindow;

        /// <summary>
        /// 初始化调试器管理器的新实例。
        /// </summary>
        public DebuggerWindowWindowManager()
        {
            _debuggerWindowRoot = new DebuggerWindowGroup();
            _beActiveWindow = false;
        }


        /// <summary>
        /// 获取或设置调试器窗口是否激活。
        /// </summary>
        public bool ActiveWindow
        {
            get { return _beActiveWindow; }
            set { _beActiveWindow = value; }
        }

        /// <summary>
        /// 调试器窗口根结点。
        /// </summary>
        public IDebuggerWindowGroup DebuggerWindowRoot
        {
            get { return _debuggerWindowRoot; }
        }

        /// <summary>
        /// 调试器管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        void IDebuggerWindowManager.Update(float elapseSeconds, float realElapseSeconds)
        {
            Update(elapseSeconds, realElapseSeconds);
        }

        private void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (!_beActiveWindow)
            {
                return;
            }

            _debuggerWindowRoot.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 关闭并清理调试器管理器。
        /// </summary>
        internal void Shutdown()
        {
            _beActiveWindow = false;
            _debuggerWindowRoot.Shutdown();
        }

        /// <summary>
        /// 是否已注册调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        public bool ContainsDebuggerWindow(string path)
        {
            return _debuggerWindowRoot.ContainsDebuggerWindow(path);
        }

        /// <summary>
        /// 注册调试器窗口。
        /// </summary>
        /// <param name="mgr"></param>
        /// <param name="path">调试器窗口路径。</param>
        /// <param name="debuggerWindow">要注册的调试器窗口。</param>
        /// <param name="args">初始化调试器窗口参数。</param>
        public void RegisterDebuggerWindow(DebuggerMgr mgr, string path, IDebuggerWindow debuggerWindow,
            params object[] args)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception("Path is invalid.");
            }

            if (debuggerWindow == null)
            {
                throw new Exception("Debugger window is invalid.");
            }

            _debuggerWindowRoot.RegisterDebuggerWindow(path, debuggerWindow);
            debuggerWindow.Initialize(mgr, args);
        }

        /// <summary>
        /// 解除注册调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <returns>是否解除注册调试器窗口成功。</returns>
        public bool UnregisterDebuggerWindow(string path)
        {
            return _debuggerWindowRoot.UnregisterDebuggerWindow(path);
        }

        /// <summary>
        /// 获取调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <returns>要获取的调试器窗口。</returns>
        public IDebuggerWindow GetDebuggerWindow(string path)
        {
            return _debuggerWindowRoot.GetDebuggerWindow(path);
        }

        /// <summary>
        /// 选中调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <returns>是否成功选中调试器窗口。</returns>
        public bool SelectDebuggerWindow(string path)
        {
            return _debuggerWindowRoot.SelectDebuggerWindow(path);
        }
    }
}