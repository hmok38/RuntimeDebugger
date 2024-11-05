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
            SampleStart.DebugMgr.RegisterDebuggerWindow("GM",new GmTestWindow());
            
            //添加相同的cmd会覆盖之前的
            SampleStart.DebugMgr.AddCmd("cmd.test0", "测试指令 无参数", () => { Debug.Log("测试指令覆盖 被触发"); }, "测试指令覆盖");
            
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