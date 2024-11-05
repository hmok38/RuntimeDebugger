using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace RuntimeDebugger
{
    public class CmdWindow : ScrollableDebuggerWindowBase
    {
        private readonly List<CmdInfo> _cmdInfos = new List<CmdInfo>();
        internal string Password = "";
        private bool _beOpenCmd;
        private string _inputPassword = "";
        private string _inputCmd = "";
        private string _resultMsg;
        private List<CmdInfo> _checkCmdInfos = new List<CmdInfo>();
        private List<CmdInfo> _buttons = new List<CmdInfo>();

        internal void AddCmd(string cmd, string cmdMsg, Delegate cb, string button = "")
        {
            CmdInfo newInfo = new CmdInfo()
            {
                Cmd = cmd,
                Msg = cmdMsg,
                Delegate = cb,
                Button = button
            };
            _cmdInfos.RemoveAll(x => x.Cmd.Equals(cmd));
            _cmdInfos.Add(newInfo);
            if (!string.IsNullOrEmpty(button))
            {
                _buttons.RemoveAll(x => x.Cmd.Equals(cmd));
                _buttons.Add(newInfo);
            }
        }

        private bool _beForceFindCmd;
        private string _passwordMsg;

        protected override void OnDrawScrollableWindow()
        {
            if (!Application.isEditor && !_beOpenCmd)
            {
                _inputPassword = GUILayout.TextField(_inputPassword);
                if (GUILayout.Button("输入密码解锁Cmd"))
                {
                    if (_inputPassword.Equals(Password))
                    {
                        _beOpenCmd = true;
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
                            //te.MoveCursorToPosition(new Vector2(_inputCmd.Length, 0)); // 将光标移动到文本末尾
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
                    _resultMsg = CmdExecute(cmd);
                }


                GUILayout.EndHorizontal();
                if (_checkCmdInfos.Count >= 1)
                {
                    for (int i = 0; i < _checkCmdInfos.Count; i++)
                    {
                        GUILayout.Label($"{_checkCmdInfos[i].Cmd} {_checkCmdInfos[i].Msg}");
                        if (i == 9) break; //最多显示10条命令提示
                    }
                }
                else if (!string.IsNullOrEmpty(cmd))
                {
                    GUILayout.Label($"<color=#ff0000>没有这个指令: {cmd}</color>");
                }

                GUILayout.Space(10);
                GUILayout.Label(_resultMsg);

                GUILayout.BeginHorizontal();

                for (int i = 0; i < _buttons.Count; i++)
                {
                    if (i > 0 && i % 4 == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                    }

                    if (GUILayout.Button(_buttons[i].Button))
                    {
                        GUI.FocusControl("InputCmd");
                        _inputCmd = _buttons[i].Cmd + " ";
                        _beForceFindCmd = true;
                    }
                }

                GUILayout.EndHorizontal();
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


            for (int i = 0; i < _cmdInfos.Count; i++)
            {
                var cmdInfo = _cmdInfos[i];
                if (needFull)
                {
                    if (cmdInfo.Cmd.Equals(cmdArray[0]))
                    {
                        infos.Add(cmdInfo);
                    }
                }
                else
                {
                    if (cmdInfo.Cmd.StartsWith(cmdArray[0]))
                    {
                        infos.Add(cmdInfo);
                    }
                }
            }
        }

        public string CmdExecute(string cmd)
        {
            var cmdArray = cmd.Split(' ');
            if (cmdArray.Length <= 0)
            {
                return $"<color=#ff0000>{DateTime.Now:MM.dd HH:mm:ss:fff} 无效的cmd:{cmd}</color>";
            }

            var info = this._cmdInfos.Find(x => x.Cmd.Equals(cmdArray[0]));
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

            return $"{DateTime.Now:MM.dd HH:mm:ss:fff} 执行完成 {cmd}";
        }
    }
}