using System.Collections;
using System.Collections.Generic;
using GameSync.Manager;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;

public class DebugGameView : MonoBehaviour
{
    [SerializeField] private CButton testBtn1;
    [SerializeField] private CButton testBtn2;
    [SerializeField] private TextInputView inputView1;
    [SerializeField] private TextInputView inputView2;
    void Start()
    {
        testBtn1.onClick.AddListener(OnTestBtn1Click);
        testBtn2.onClick.AddListener(OnTestBtn2Click);
    }


    private void OnTestBtn1Click()
    {
        TestLinkEmoteBind();
    }

    private void OnTestBtn2Click()
    {
        // TestLinkEmoteUnBind();
        ClientManager.Inst.TestReconnect();
    }

    #region 测试双人牵手
    private void TestLinkEmoteBind()
    {
        // string playerIdA = inputView1.Input;
        // string playerIdB = inputView2.Input;
        
        // string playerIdA = "1815310838542921728";
        // string playerIdB = "1816452138537984000";
        
        string playerIdA = "1816452138537984000";
        string playerIdB = "1815310838542921728";
        
        LinkEmoteManager.Inst.StartBind(playerIdA,playerIdB,"40900001");
    }

    private void TestLinkEmoteUnBind()
    {
        // string playerIdA = inputView1.Input;
        // string playerIdB = inputView2.Input;
        // string playerIdA = "1815310838542921728";
        // string playerIdB = "1816452138537984000";
        string playerIdA = "1816452138537984000";
        string playerIdB = "1815310838542921728";
        LinkEmoteManager.Inst.StopBind(playerIdA,playerIdB);
    }
    #endregion
}
