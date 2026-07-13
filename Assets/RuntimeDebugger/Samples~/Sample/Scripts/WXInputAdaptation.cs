using RuntimeDebugger;
#if UNITY_WEBGL
using WeChatWASM;
#endif

/// <summary>
///-------------------注意-------------------------
/// 导入本类或者示例,必须依赖微信sdk,如果不需要本转换器,可以全部注释掉
/// ----------------------------------------------
///
/// 
/// Debugger窗口的微信小游戏平台下的输入法适配器
/// 在微信小游戏平台下当文本输入框获得焦点时必须使用微信的接口才可以正确拉起输入法
/// 微信小游戏对接示例（参考 WX SDK 官方 WXInputFieldAdapter）:https://developers.weixin.qq.com/minigame/dev/guide/game-engine/unity-webgl-transform/Design/InputAdaptation.html
/// </summary>
public class WXInputAdaptation
{
#if UNITY_WEBGL
    private string _wxText;
    private bool _isShow;

    /// <summary>
    /// 注意,必须添加对WXSDk的依赖才能正常使用
    /// 设置debuggerMgr的输入法为微信输入法,
    /// </summary>
    /// <param name="mgr"></param>
    public void Init(DebuggerMgr mgr)
    {
        mgr.SetCustomInput(() => true,
            onShowKeyboard: OnShowKeyboard, onHideKeyboard: OnHideKeyboard,
            onGetKeyboardText: OnGetKeyboardText, onSetKeyboardText: OnSetKeyboardText);
    }

    private void OnShowKeyboard(string text)
    {
        if (_isShow) return;
        _isShow = true;
        _wxText = text;
        WX.ShowKeyboard(new ShowKeyboardOption { defaultValue = text });
        WX.OnKeyboardInput(OnInput);
        WX.OnKeyboardConfirm(OnConfirm);
        WX.OnKeyboardComplete(OnComplete);
    }

    private void OnHideKeyboard()
    {
        if (!_isShow) return;
        _isShow = false;
        WX.OffKeyboardInput(OnInput);
        WX.OffKeyboardConfirm(OnConfirm);
        WX.OffKeyboardComplete(OnComplete);
        WX.HideKeyboard(new HideKeyboardOption());
        SampleStart.DebugMgr.NotifyKeyboardDismissed();
    }

    private string OnGetKeyboardText()
    {
        return _wxText;
    }

    private void OnSetKeyboardText(string text)
    {
        _wxText = text;
        WX.UpdateKeyboard(new UpdateKeyboardOption { value = text });
    }


    private void OnComplete(OnKeyboardInputListenerResult v)
    {
        _wxText = v.value;
        OnHideKeyboard();
    }

    private void OnConfirm(OnKeyboardInputListenerResult v)
    {
        _wxText = v.value;
        OnHideKeyboard();
    }

    private void OnInput(OnKeyboardInputListenerResult v)
    {
        _wxText = v.value;
    }

#endif
}