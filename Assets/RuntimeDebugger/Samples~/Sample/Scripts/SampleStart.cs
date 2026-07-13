using System;
using RuntimeDebugger;
using UnityEngine;
#if UNITY_WEBGL
using WeChatWASM;
#endif

public class SampleStart : MonoBehaviour
{
    public static RuntimeDebugger.DebuggerMgr DebugMgr;

    void Start()
    {
        var font = Resources.Load<Font>("Font37"); //因为在某些平台会丢失中文,所以可以额外加载中文字体传入debug窗口

        //编辑器中或者development build强制打开
        //如果不是强制打开,会检查Application.persistentDataPath,中是否有 "logsetting.txt"文件,并设置LogEnable=true
        DebugMgr = RuntimeDebugger.DebuggerMgr.Init(Application.isEditor || UnityEngine.Debug.isDebugBuild, font, 12);
        if (SampleStart.DebugMgr != null)
        {
#if UNITY_WEBGL
            DebugMgr.SetCustomInput(() => { return true; },
                onShowKeyboard: WXSample.OnShowKeyboard, onHideKeyboard: WXSample.OnHideKeyboard,
                onGetKeyboardText: WXSample.OnGetKeyboardText, onSetKeyboardText: WXSample.OnSetKeyboardText
            );
#endif
            //设置cmd密码,密码正确才能打开cmd窗口
            SampleStart.DebugMgr.SetCmdPassword("xxxxxx");
            //添加cmd指令
            SampleStart.DebugMgr.AddCmd("cmd.test0", "测试指令 无参数", () => { Debug.Log("测试指令 被触发"); }, "测试指令");
            SampleStart.DebugMgr.AddCmd("cmd.test1", "测试指令1 int:参数1 string:参数2",
                (arg0, arg1) => { Debug.Log($"测试指令 被触发,参数1:{arg0} 参数2:{arg1}"); }, "测试指令1");
            //设置日志最大行数
            SampleStart.DebugMgr.SetConsoleMaxLine(200);
            SampleStart.DebugMgr.RegisterDebuggerWindow("GM", new GmTestWindow());

            //添加相同的cmd会覆盖之前的
            SampleStart.DebugMgr.AddCmd("cmd.test0", "测试指令 无参数", () => { Debug.Log("测试指令覆盖 被触发"); }, "测试指令覆盖");

            SampleStart.DebugMgr.AddCmd("cmd.removeTest0", "移除test0的按钮",
                () => { SampleStart.DebugMgr.RemoveCmd("cmd.test0"); }, "移除test0的按钮");
            SampleStart.DebugMgr.AddCmd("cmd.ReAddTest0", "重新添加test0的按钮",
                () =>
                {
                    SampleStart.DebugMgr.AddCmd("cmd.test0", "测试指令 无参数", () => { Debug.Log("测试指令覆盖 被触发"); }, "测试指令覆盖");
                }, "重新添加test0的按钮");


            SampleStart.DebugMgr.AddCmd($"cmd.testGroup11", "测试分组指令 无参数", () => { Debug.Log("测试分组指令 被触发1"); },
                "测试分组指令11", $"TestGroup1");
            SampleStart.DebugMgr.AddCmd($"cmd.testGroup22", "测试分组指令 int:数量",
                (string arg0) => { Debug.Log($"测试分组指令 被触发22  {arg0}"); }, "测试分组指令22", $"TestGroup1");

            //添加窗口位置和大小改变的回调,返回的是屏幕坐标
            SampleStart.DebugMgr.WindowChangeAction = (rect) => { Debug.Log("窗口位置和大小:" + rect); };
            SampleStart.DebugMgr.AddCmd($"cmd.testConsole", "测试控制台指令  int:输出次数", (count) =>
            {
                if (!int.TryParse(count, out int UPPER))
                {
                    Debug.Log($"测试控制台指令 被触发 参数错误:{count}");
                    return;
                }

                for (int i = 0; i < UPPER; i++)
                {
                    Debug.Log($"测试控制台指令 被触发 输出:{i}");
                }
            }, "测试控制台指令", null);
        }
    }
}
#if UNITY_WEBGL
public static class WXSample
{
    private static string wxText;
    private static bool _isShow;

    public static void OnShowKeyboard(string text)
    {
        if (_isShow) return;
        _isShow = true;
        wxText = text;

        WX.ShowKeyboard(new ShowKeyboardOption { defaultValue = text });
        WX.OnKeyboardInput(OnInput);
        WX.OnKeyboardConfirm(OnConfirm);
        WX.OnKeyboardComplete(OnComplete);
    }

    public static void OnHideKeyboard()
    {
        if (!_isShow) return;
        _isShow = false;
        WX.OffKeyboardInput(OnInput);
        WX.OffKeyboardConfirm(OnConfirm);
        WX.OffKeyboardComplete(OnComplete);
        WX.HideKeyboard(new HideKeyboardOption());
        SampleStart.DebugMgr.NotifyKeyboardDismissed();
    }

    public static string OnGetKeyboardText()
    {
        return wxText;
    }

    public static void OnSetKeyboardText(string text)
    {
        wxText = text;
        WX.UpdateKeyboard(new UpdateKeyboardOption { value = text });
    }


    private static void OnComplete(OnKeyboardInputListenerResult v)
    {
        wxText = v.value;
        OnHideKeyboard();
    }

    private static void OnConfirm(OnKeyboardInputListenerResult v)
    {
        wxText = v.value;
        OnHideKeyboard();
    }

    private static void OnInput(OnKeyboardInputListenerResult v)
    {
        wxText = v.value;
    }
}
#endif

public class GmTestWindow : RuntimeDebugger.ScrollableDebuggerWindowBase
{
    protected override void OnDrawScrollableWindow()
    {
        if (GUILayout.Button("测试页面的测试按钮"))
        {
            Debug.Log("测试页面的测试按钮被触发");
        }
    }
}