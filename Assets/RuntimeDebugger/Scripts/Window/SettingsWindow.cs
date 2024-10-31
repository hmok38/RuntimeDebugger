using System;
using UnityEngine;

namespace RuntimeDebugger
{
    public class SettingsWindow : ScrollableDebuggerWindowBase
    {
        private float m_LastIconX = 0f;
        private float m_LastIconY = 0f;
        private float m_LastWindowX = 0f;
        private float m_LastWindowY = 0f;
        private float m_LastWindowWidth = 0f;
        private float m_LastWindowHeight = 0f;
        private float m_LastWindowScale = 0f;
        private string m_GM = "";

        public override void Initialize(DebuggerMgr mgr, params object[] args)
        {
            base.Initialize(mgr);


            m_LastIconX = PlayerPrefs.GetFloat("Debugger.Icon.X", DebuggerMgr.DefaultIconRect.x);
            m_LastIconY = PlayerPrefs.GetFloat("Debugger.Icon.Y", DebuggerMgr.DefaultIconRect.y);
            m_LastWindowX = PlayerPrefs.GetFloat("Debugger.Window.X", DebuggerMgr.DefaultWindowRect.x);
            m_LastWindowY = PlayerPrefs.GetFloat("Debugger.Window.Y", DebuggerMgr.DefaultWindowRect.y);
            m_LastWindowWidth = PlayerPrefs.GetFloat("Debugger.Window.Width", DebuggerMgr.DefaultWindowRect.width);
            m_LastWindowHeight = PlayerPrefs.GetFloat("Debugger.Window.Height", DebuggerMgr.DefaultWindowRect.height);
            m_DebuggerComponent.WindowScale = m_LastWindowScale =
                PlayerPrefs.GetFloat("Debugger.Window.Scale", DebuggerMgr.DefaultWindowScale);
            m_DebuggerComponent.IconRect =
                new Rect(m_LastIconX, m_LastIconY, DebuggerMgr.DefaultIconRect.width,
                    DebuggerMgr.DefaultIconRect.height);
            m_DebuggerComponent.MWindowWidth = Math.Min(m_LastWindowWidth,
                m_DebuggerComponent.DesignWidth - (int)(DebuggerMgr.DefaultWindowPadding * 2));
            m_DebuggerComponent.MWindowHeight = Math.Min(m_LastWindowHeight,
                m_DebuggerComponent.DesignHeight - (int)(DebuggerMgr.DefaultWindowPadding * 2));
            // m_DebuggerComponent.m_WindowWidth = DefaultWindowRect.width;
            // m_DebuggerComponent.m_WindowHeight = DefaultWindowRect.height;

            m_DebuggerComponent.WindowRect = new Rect(m_LastWindowX, m_LastWindowY, m_DebuggerComponent.MWindowWidth,
                m_DebuggerComponent.MWindowHeight);
            
            m_DebuggerComponent.MWindowContentMaxWidth = m_DebuggerComponent.MWindowWidth -
                                                         m_DebuggerComponent.MSkin.window.padding.left -
                                                         m_DebuggerComponent.MSkin.window.padding.right;
            m_DebuggerComponent.MWindowContentMaxHeight = m_DebuggerComponent.MWindowHeight -
                                                          m_DebuggerComponent.MSkin.window.padding.top -
                                                          m_DebuggerComponent.MSkin.window.padding.bottom;
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (m_LastIconX != m_DebuggerComponent.IconRect.x)
            {
                m_LastIconX = m_DebuggerComponent.IconRect.x;
                PlayerPrefs.SetFloat("Debugger.Icon.X", m_DebuggerComponent.IconRect.x);
            }

            if (m_LastIconY != m_DebuggerComponent.IconRect.y)
            {
                m_LastIconY = m_DebuggerComponent.IconRect.y;
                PlayerPrefs.SetFloat("Debugger.Icon.Y", m_DebuggerComponent.IconRect.y);
            }

            if (m_LastWindowX != m_DebuggerComponent.WindowRect.x)
            {
                m_LastWindowX = m_DebuggerComponent.WindowRect.x;
                PlayerPrefs.SetFloat("Debugger.Window.X", m_DebuggerComponent.WindowRect.x);
            }

            if (m_LastWindowY != m_DebuggerComponent.WindowRect.y)
            {
                m_LastWindowY = m_DebuggerComponent.WindowRect.y;
                PlayerPrefs.SetFloat("Debugger.Window.Y", m_DebuggerComponent.WindowRect.y);
            }

            if (m_LastWindowWidth != m_DebuggerComponent.WindowRect.width)
            {
                m_LastWindowWidth = m_DebuggerComponent.WindowRect.width;
                PlayerPrefs.SetFloat("Debugger.Window.Width", m_DebuggerComponent.WindowRect.width);
            }

            if (m_LastWindowHeight != m_DebuggerComponent.WindowRect.height)
            {
                m_LastWindowHeight = m_DebuggerComponent.WindowRect.height;
                PlayerPrefs.SetFloat("Debugger.Window.Height", m_DebuggerComponent.WindowRect.height);
            }

            if (m_LastWindowScale != m_DebuggerComponent.WindowScale)
            {
                m_LastWindowScale = m_DebuggerComponent.WindowScale;
                PlayerPrefs.SetFloat("Debugger.Window.Scale", m_DebuggerComponent.WindowScale);
            }
        }

        protected override void OnDrawScrollableWindow()
        {
            GUILayout.Label("<b>Window Settings</b>");

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Position:", GUILayout.Width(60f));
                    GUILayout.Label("Drag window caption to move position.");
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    float width = m_DebuggerComponent.WindowRect.width;
                    GUILayout.Label("Width:", GUILayout.Width(60f));
                    if (GUILayout.RepeatButton("-", GUILayout.Width(30f)))
                    {
                        width--;
                    }

                    width = GUILayout.HorizontalSlider(width, DebuggerMgr.DefaultWindowRect.width,
                        m_DebuggerComponent.DesignWidth - (int)(DebuggerMgr.DefaultWindowPadding * 2));
                    if (GUILayout.RepeatButton("+", GUILayout.Width(30f)))
                    {
                        width++;
                    }

                    width = Mathf.Clamp(width, DebuggerMgr.DefaultWindowRect.width,
                        m_DebuggerComponent.DesignWidth - (int)(DebuggerMgr.DefaultWindowPadding * 2));
                    if (width != m_DebuggerComponent.WindowRect.width)
                    {
                        m_DebuggerComponent.MWindowWidth = width;
                        m_DebuggerComponent.MWindowContentMaxWidth = width -
                                                                     m_DebuggerComponent.MSkin.window.padding.left -
                                                                     m_DebuggerComponent.MSkin.window.padding.right;
                        m_DebuggerComponent.WindowRect = new Rect(m_DebuggerComponent.WindowRect.x,
                            m_DebuggerComponent.WindowRect.y, width, m_DebuggerComponent.WindowRect.height);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    float height = m_DebuggerComponent.WindowRect.height;
                    GUILayout.Label("Height:", GUILayout.Width(60f));
                    if (GUILayout.RepeatButton("-", GUILayout.Width(30f)))
                    {
                        height--;
                    }

                    height = GUILayout.HorizontalSlider(height, DebuggerMgr.DefaultWindowRect.height,
                        m_DebuggerComponent.DesignHeight - (int)(DebuggerMgr.DefaultWindowPadding * 2));
                    if (GUILayout.RepeatButton("+", GUILayout.Width(30f)))
                    {
                        height++;
                    }

                    height = Mathf.Clamp(height, DebuggerMgr.DefaultWindowRect.height,
                        m_DebuggerComponent.DesignHeight - (int)(DebuggerMgr.DefaultWindowPadding * 2));
                    if (height != m_DebuggerComponent.WindowRect.height)
                    {
                        m_DebuggerComponent.MWindowHeight = height;
                        m_DebuggerComponent.MWindowContentMaxHeight = height -
                                                                      m_DebuggerComponent.MSkin.window.padding.top -
                                                                      m_DebuggerComponent.MSkin.window.padding.bottom;
                        m_DebuggerComponent.WindowRect = new Rect(m_DebuggerComponent.WindowRect.x,
                            m_DebuggerComponent.WindowRect.y, m_DebuggerComponent.WindowRect.width, height);
                    }
                }
                GUILayout.EndHorizontal();

                //弃用
                // GUILayout.BeginHorizontal();
                // {
                //     float scale = m_DebuggerComponent.WindowScale;
                //     GUILayout.Label("Scale:", GUILayout.Width(60f));
                //     if (GUILayout.RepeatButton("-", GUILayout.Width(60f)))
                //     {
                //         scale -= 0.01f;
                //     }
                //
                //     scale = GUILayout.HorizontalSlider(scale, 0.5f, 4f);
                //     if (GUILayout.RepeatButton("+", GUILayout.Width(30f)))
                //     {
                //         scale += 0.01f;
                //     }
                //
                //     scale = Mathf.Clamp(scale, 0.5f, 4f);
                //     if (scale != m_DebuggerComponent.WindowScale)
                //     {
                //         m_DebuggerComponent.WindowScale = scale;
                //     }
                // }
                // GUILayout.EndHorizontal();

                //弃用
                // GUILayout.BeginHorizontal();
                // {
                //     if (GUILayout.Button("0.5x", GUILayout.Height(60f)))
                //     {
                //         m_DebuggerComponent.WindowScale = 0.5f;
                //     }
                //
                //     if (GUILayout.Button("1.0x", GUILayout.Height(60f)))
                //     {
                //         m_DebuggerComponent.WindowScale = 1f;
                //     }
                //
                //     if (GUILayout.Button("1.5x", GUILayout.Height(60f)))
                //     {
                //         m_DebuggerComponent.WindowScale = 1.5f;
                //     }
                //
                //     if (GUILayout.Button("2.0x", GUILayout.Height(60f)))
                //     {
                //         m_DebuggerComponent.WindowScale = 2f;
                //     }
                //
                //     if (GUILayout.Button("2.5x", GUILayout.Height(60f)))
                //     {
                //         m_DebuggerComponent.WindowScale = 2.5f;
                //     }
                //
                //     if (GUILayout.Button("3.0x", GUILayout.Height(60f)))
                //     {
                //         m_DebuggerComponent.WindowScale = 3f;
                //     }
                //
                //     if (GUILayout.Button("3.5x", GUILayout.Height(60f)))
                //     {
                //         m_DebuggerComponent.WindowScale = 3.5f;
                //     }
                //
                //     if (GUILayout.Button("4.0x", GUILayout.Height(60f)))
                //     {
                //         m_DebuggerComponent.WindowScale = 4f;
                //     }
                // }
                // GUILayout.EndHorizontal();

                

                if (GUILayout.Button("Auto MaxSize Layout", GUILayout.Height(30f)))
                {
                    m_DebuggerComponent.ResetLayout();
                    float windowMaxW = m_DebuggerComponent.DesignWidth - (int)(DebuggerMgr.DefaultWindowPadding * 2);
                    float windowMaxH = m_DebuggerComponent.DesignHeight - (int)(DebuggerMgr.DefaultWindowPadding * 2);
                    m_DebuggerComponent.MWindowContentMaxWidth = windowMaxW -
                                                                 m_DebuggerComponent.MSkin.window.padding.left -
                                                                 m_DebuggerComponent.MSkin.window.padding.right;
                    m_DebuggerComponent.MWindowContentMaxHeight = windowMaxH -
                                                                  m_DebuggerComponent.MSkin.window.padding.top -
                                                                  m_DebuggerComponent.MSkin.window.padding.bottom;
                    m_DebuggerComponent.WindowRect = new Rect(m_DebuggerComponent.WindowRect.x,
                        m_DebuggerComponent.WindowRect.y, windowMaxW, windowMaxH);
                }
                if (GUILayout.Button("Reset Layout", GUILayout.Height(30f)))
                {
                    m_DebuggerComponent.ResetLayout();
                }
            }
            GUILayout.EndVertical();
        }
    }
}