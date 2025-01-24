using System;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    public class LogNode
    {
        public bool IsAcquire { get; set; }
        private DateTime _mLogTime;
        private int _mLogFrameCount;
        private LogType _mLogType;
        private string _mLogMessage;
        private string _mStackTrack;
        private Color32 _mColor;
        public string MyText;
        public LogNode Next;
        public LogNode Pre;
        public LogNode End;
        public LogNode FilterNext;
        public int Size;

        /// <summary>
        /// 初始化日志记录结点的新实例。
        /// </summary>
        public LogNode()
        {
            _mLogTime = default(DateTime);
            _mLogFrameCount = 0;
            _mLogType = LogType.Error;
            _mLogMessage = null;
            _mStackTrack = null;
        }

        /// <summary>
        /// 获取日志时间。
        /// </summary>
        public DateTime LogTime
        {
            get { return _mLogTime; }
        }

        /// <summary>
        /// 获取日志帧计数。
        /// </summary>
        public int LogFrameCount
        {
            get { return _mLogFrameCount; }
        }

        /// <summary>
        /// 获取日志类型。
        /// </summary>
        public LogType LogType
        {
            get { return _mLogType; }
        }


        /// <summary>
        /// 日志类型对应显示的颜色。
        /// </summary>
        public Color32 Color
        {
            set { _mColor = value; }
            get { return _mColor; }
        }

        /// <summary>
        /// 获取日志内容。
        /// </summary>
        public string LogMessage
        {
            get { return _mLogMessage; }
        }

        /// <summary>
        /// 获取日志堆栈信息。
        /// </summary>
        public string StackTrack
        {
            get { return _mStackTrack; }
        }

        public static MyObjectPool<LogNode> Pool;

        /// <summary>
        /// 创建日志记录结点。
        /// </summary>
        /// <param name="logType">日志类型。</param>
        /// <param name="logMessage">日志内容。</param>
        /// <param name="stackTrack">日志堆栈信息。</param>
        /// <returns>创建的日志记录结点。</returns>
        public static LogNode Create(LogType logType, string logMessage, string stackTrack)
        {
            if (Pool == null)
            {
                Pool = new MyObjectPool<LogNode>(() => new LogNode(), _ => { },
                    obj => { obj.Clear(); }, _ => { });
            }

            LogNode logNode = Pool.Get();
            logNode._mLogTime = DateTime.UtcNow;
            logNode._mLogFrameCount = Time.frameCount;
            logNode._mLogType = logType;
            logNode._mLogMessage = logMessage;
            logNode._mStackTrack = stackTrack;
            return logNode;
        }

        /// <summary>
        /// 清理日志记录结点。
        /// </summary>
        public void Clear()
        {
            _mLogTime = default(DateTime);
            _mLogFrameCount = 0;
            _mLogType = LogType.Error;
            _mLogMessage = null;
            _mStackTrack = null;
            Next = null;
            Pre = null;
            FilterNext = null;
            End = null;
            Size = 0;
        }

        /// <summary>
        /// 用来复用
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="logMessage"></param>
        /// <param name="stackTrack"></param>
        public void Reset(LogType logType, string logMessage, string stackTrack)
        {
            _mLogTime = DateTime.UtcNow;
            _mLogFrameCount = Time.frameCount;
            _mLogType = logType;
            _mLogMessage = logMessage;
            _mStackTrack = stackTrack;
            Next = null;
            Pre = null;
            FilterNext = null;
            End = null;
            Size = 0;
        }
    }
}