using System;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using UI.BaseWidgets;
using UI.Preview3D;
using UI.Preview3D.Base;
using UI.Preview3D.Bean;
using UI.Preview3D.Mono;

/// <summary>
/// Author:
/// Desc:
/// Date:23-07-01 23:08:50
/// </summary>
public class TestPanel1 : BasePanel<TestPanel1>
{
    public Button testBtn;
    public Text testText;
    public Button testBtn1;
    public Text testText1;

    public UISegmentView segmentView;
    public CButton backBtn;

    public Preview3DRawImage preview3DRawImage;
    public CButton testPreviewSingleEmoteBtn1;
    public CButton testPreviewSingleEmoteBtn2;
    public CButton testPreviewDoubleEmoteBtn1;
    public CButton testUnPreviewBtn;
    
    public override void OnCreate()
    {
        //  AkSoundEngine.AddBasePath(Application.persistentDataPath + "/Bundles/wwise/");
        //  // AkBankManager.LoadInitBank();
        //  AkBankManager.LoadBank("Bud_Bgm_SoundBank", false, false);
        
        testBtn.onClick.AddListener(()=> {
        
            string musicEventName = "Bgm_Amusement_Park";
            string bgmEvent = "Play_Bgm_Loop";
            string bgmSwitch = "Bgm_Group";
            testText.text = "Bgm_Amusement_Park";
            // AkSoundEngine.SetSwitch(bgmSwitch, musicEventName, this.gameObject);
            // AkSoundEngine.PostEvent(bgmEvent, this.gameObject, (uint)AkCallbackType.AK_EndOfEvent, null,
            //     musicEventName);
        });
        
        testBtn1.onClick.AddListener(() => {
        
            string musicEventName = "Bgm_Final_Race";
            string bgmEvent = "Play_Bgm_Loop";
            string bgmSwitch = "Bgm_Group";
            testText1.text = "播放Bgm_Final_Race";
            // AkSoundEngine.SetSwitch(bgmSwitch, musicEventName, this.gameObject);
            // AkSoundEngine.PostEvent(bgmEvent, this.gameObject, (uint)AkCallbackType.AK_EndOfEvent, null,
            //     musicEventName);
        });
        
        backBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });

        List<string> lists = new List<string>
        {
            "android", "Unity", "iOS"
        };

        segmentView.SetSegementData(lists, defaultIndex:1, i =>
        {
            LoggerUtils.Log("点击 索引： " + i.ToString());
        });
        
        testPreviewSingleEmoteBtn1.onClick.AddListener(() =>
        {
            Preview3DData testData = new Preview3DData()
            {
                ResType = 20,
                PgcIdStr = "10",
                PreviewType = Preview3DType.Emote
            };
            Preview3DManager.Inst.Preview(preview3DRawImage, testData);
        });   
        
        testPreviewSingleEmoteBtn2.onClick.AddListener(() =>
        {
            Preview3DData testData = new Preview3DData()
            {
                ResType = 20,
                PgcIdStr = "42",
                PreviewType = Preview3DType.Emote
            };
            Preview3DManager.Inst.Preview(preview3DRawImage, testData);
        });  
        
        testPreviewDoubleEmoteBtn1.onClick.AddListener(() =>
        {
            Preview3DData testData = new Preview3DData()
            {
                ResType = 20,
                PgcIdStr = "72",
                PreviewType = Preview3DType.Emote
            };
            Preview3DManager.Inst.Preview(preview3DRawImage, testData);
        }); 
        
        testUnPreviewBtn.onClick.AddListener(() =>
        {
            Preview3DManager.Inst.CancelPreview(preview3DRawImage);
        });
    }
    
    public override void OnShow(params object[] args)
    {
        Debug.Log("TestPanel1 show");
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
    }
    
    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}