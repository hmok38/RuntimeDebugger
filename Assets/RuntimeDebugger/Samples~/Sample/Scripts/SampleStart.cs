using RuntimeDebugger;
using UnityEngine;

public class SampleStart : MonoBehaviour
{
    public static RuntimeDebugger.DebuggerMgr DebugMgr;

    void Start()
    {
        //编辑器中或者development build强制打开
        //如果不是强制打开,会检查Application.persistentDataPath,中是否有 "logsetting.txt"文件,并设置LogEnable=true
        DebugMgr = RuntimeDebugger.DebuggerMgr.Init(Application.isEditor || UnityEngine.Debug.isDebugBuild);
        if (SampleStart.DebugMgr != null)
        {
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


            SampleStart.DebugMgr.AddCmd($"cmd.testGroup11", "测试分组指令 无参数", () => { Debug.Log("测试分组指令 被触发1"); }, "测试分组指令11", $"TestGroup1");
            SampleStart.DebugMgr.AddCmd($"cmd.testGroup22", "测试分组指令 int:数量", (string arg0) => { Debug.Log($"测试分组指令 被触发22  {arg0}"); }, "测试分组指令22", $"TestGroup1");

            //添加窗口位置和大小改变的回调,返回的是屏幕坐标
            SampleStart.DebugMgr.WindowChangeAction = (rect) => { Debug.Log("窗口位置和大小:" + rect); };
            SampleStart.DebugMgr.AddCmd($"cmd.testConsole", "测试控制台指令  int:输出次数", (count) =>
            {
                if(!int.TryParse(count,out int UPPER))
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