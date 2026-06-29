using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ASR 接入测试脚本，仅用于开发验证，不用于正式业务。
/// 挂到场景任意 GameObject，在 Inspector 中指定 Config 后运行。
///
/// 操作方式：
///   Editor / PC：按住空格开始录音，松开停止
///   移动端：触摸屏幕开始录音，松开停止
/// </summary>
public class ASRmono : MonoBehaviour
{
    public ASRTester aSRTester;
    public Button beginBtn;
    public Button endBtn;
     private void Awake() {

        Screen.orientation = ScreenOrientation.Portrait ;
        beginBtn.onClick.AddListener(() =>
        {
            aSRTester.BeginRecord();
        })  ;  

        endBtn.onClick.AddListener(() =>
        {
            aSRTester.EndRecord();
        }) ;
    }
}
