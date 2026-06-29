using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Basic.Utils;
using DG.Tweening;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.Utils;
using GameData.BaseInfo;
using GameData.Manager;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AIYandereGameOverPanel : BasePanel<AIYandereGameOverPanel>
{
    public class ResultData
    {
        public string endSummary;
    }
    
    public Text Txt_Desc;
    public Image Img_Bg;
    public Sprite[] TextBGSprites;

    public Sprite[] DialogSprites;
    public Image DialogImage;

    public Sprite[] DialogStarSprites;
    public Image DialogStarImage;
    public Text NpcName;
    
    [Header("结算部分")]
    public GameObject Go_SettleNode;
    public Image Img_SettleBg;
    public Image Panel_Desc;
    
    public Text Txt_SettleDesc;
    public GameObject SuccessText;
    public GameObject FailText;
    public CButton Btn_ExitGame;
    public CButton Btn_TryAgain;
    public Button Btn_Next;
    public GameRemoteImageBehaviour imageBehaviour1;

    private bool isSuccess = false;
    private NpcVoicePlayer voicePlayer;
    private YandereStep _curYandereStep;
    // private string _endDesc;
    private string[] colors =  {"#FF63C8","#ACB3BC"};
    private string curColor = "#FF63C8";
    private string curIconName = "ai_success";
    private BudTimer blackAnimTimer;
    private bool isPlayFinalTextAnim = true;
    private bool isPgcEnter = true;
    private string endContent = string.Empty;
    private bool isGetEndResponse = false;
    private string[] endContents =
    {
        "时间到！逃脱失败，你隐约听到你的朋友们嘲笑你家庭弟位的笑声！", 
        "逃脱失败！不仅出不了门还被揍得鼻青脸肿，你隐约听到朋友们嘲笑你家庭弟位的笑声！",
        "逃脱失败！虽然打开了门，但你被女友揍得鼻青脸肿，实在没脸出门见人...", 
        "逃脱成功！不仅打开了门，还躲过了女友的拖鞋，但你出门后开始担心今晚进不了家门...",
        "逃脱成功！你兴高采烈地出门了，殊不知在你今晚回家后将会面临怎样的恐怖结局..."
    };
    
    private string[] endUgcContents =
    {
        "时间到！逃脱失败，你将和{0}永远呆在这座公寓里！", 
        "逃脱失败！屈服于{0}的愤怒，你将和{0}永远呆在这座公寓里！",
        "逃脱失败！虽然打开了门，但最终还是没能躲过{0}的怒火，只得和{0}永远呆在这座公寓里！", 
        "逃脱成功！不仅打开了门，还成功逃离{0}，却发现外面的世界比公寓里更加恐怖...",
        "逃脱成功！你兴高采烈地出门了，却发现外面的世界比公寓里更加恐怖..."
    };
    
    public override void OnCreate()
    {
        base.OnCreate();
        Image bg = Img_Bg.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0);
        bg.DOFade(1, 0.3f);
        voicePlayer = NpcVoicePlayer.Create("AIYandereGameOverPanel");
        voicePlayer.transform.SetParent(this.transform);
        AddListener();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        int duration = 5;
        Go_SettleNode.SetActive(false);
        long gameTime = 0;
        string conversationId = string.Empty;
        if(args != null && args.Length >= 3)
        {
            _curYandereStep = (YandereStep)args[0];
            gameTime = (long)args[1];
            conversationId = (string) args[2];
            SendResultToServer(gameTime,_curYandereStep,conversationId);
        }

       
        AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Typing_Loop);
        Txt_Desc.text = string.Empty;
        string content = GetResultContent();
        Txt_Desc.DOText(content, duration).OnComplete(() =>
        {
            isPlayFinalTextAnim = false;
            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
        });
        
        var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
        NpcName.text = npcInfo.name;
        
        if (!string.IsNullOrEmpty(npcInfo.npcPortraitUrl))
        {
            imageBehaviour1.Load(npcInfo.npcPortraitUrl);
        }

        if (_curYandereStep == YandereStep.BadEnd_2 || _curYandereStep == YandereStep.BadEnd_3)
        {
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.SOUND_DIE);
        }

        SetSettleData(_curYandereStep);
        InitBG();
        blackAnimTimer = TimerManager.Inst.RunOnce("GameOver", duration + 5f, () =>
        {
            if (_curYandereStep == YandereStep.BadEnd_2 || _curYandereStep == YandereStep.BadEnd_3)
            {
                AIGameSoundUtils.Inst.StopSound(YandereConfig.SOUND_DIE);
            }
            EndBlackEffect();
        });
    }

    private string GetResultContent()
    {
        int result = YandereDataManager.Inst.FinalResult;
        var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
        isPgcEnter = npcInfo.id.Equals("0");
        var content = isPgcEnter ? endContents[result] : endUgcContents[result];
        content = string.Format(content, npcInfo.name);
        return content;
    }


    private void EndBlackEffect()
    {
        AIGameSoundUtils.Inst.StopCurrentBgm();
        var endSound = isSuccess ? YandereConfig.SOUND_GAME_WIN : YandereConfig.SOUND_GAME_OVER;
        AIGameSoundUtils.Inst.PlaySound(endSound);
        Go_SettleNode.gameObject.SetActive(true);
        if (isGetEndResponse)
        {
            PlayEndTextAnim();
        }
    }

    private void PlayEndTextAnim()
    {
        Txt_SettleDesc.DOKill();
        Txt_SettleDesc.text = "";
        Txt_SettleDesc.DOText(endContent, 2);
    }

    public void SendResultToServer(float gameTime, YandereStep curStep,string conversation)
    {
        var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
        AIYandereGame.GameResultReq req = new()
        {
            npcId = npcInfo.id,
            gameId = (int) PGCGameType.AIYandere,
            duration = (int) gameTime,
            result = YandereDataManager.Inst.GetResult(curStep),
            conversationId = conversation
        };
        var paramStr = JsonConvert.SerializeObject(req);
        Txt_SettleDesc.text = string.Empty;
        Txt_SettleDesc.DOText("......", 2).SetLoops(-1, LoopType.Restart);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIResult, HttpMethod.POST, paramStr,
            (content) =>
            {
                if (Txt_SettleDesc == null)
                {
                    return;
                }
                var data = JsonConvert.DeserializeObject<ResultData>(content);
                if (data != null)
                {
                    isGetEndResponse = true;
                    endContent = data.endSummary;
                    if (!isPlayFinalTextAnim)
                    {
                        PlayEndTextAnim();
                    }
                }
                LoggerUtils.Log("病娇消息发送成功");
            }, (err) => { LoggerUtils.Log("病娇消息发送失败", err); }, null, 0, 3);
    }

    private void InitBG()
    {
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(Img_SettleBg.transform);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem(curColor, atlasPath, new List<string>()
        {
            curIconName
        });
        item.gameObject.SetActive(true);
    }
    
    private void AddListener()
    {
        Btn_ExitGame.onClick.AddListener(OnBtnExitGameClick);
        Btn_TryAgain.onClick.AddListener(OnBtnTryAgainClick);
        Btn_Next.onClick.AddListener(OnNextClick);
    }

    private void OnNextClick()
    {
        if (isPlayFinalTextAnim)
        {
            isPlayFinalTextAnim = false;
            var content = GetResultContent();
            Txt_Desc.DOKill();
            Txt_Desc.text = content;
            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
        }
        else
        {
            TimerManager.Inst.Stop(blackAnimTimer);
            EndBlackEffect();
        }
    }

    private void SetSettleData(YandereStep yandereStep)
    {
        switch (yandereStep)
        {
            case YandereStep.BadEnd_1:
            case YandereStep.BadEnd_2:
            case YandereStep.BadEnd_3:
                isSuccess = false;
                curIconName = "ai_fail";
                curColor = colors[1];
                Panel_Desc.sprite = TextBGSprites[1];
                DialogImage.sprite = DialogSprites[1];
                DialogStarImage.sprite = DialogStarSprites[1];
                SuccessText.SetActive(false);
                FailText.SetActive(true);
                break;
            case YandereStep.GoodEnd_1:
            case YandereStep.GoodEnd_2:
                isSuccess = true;
                curIconName = "ai_success";
                curColor = colors[0];
                Panel_Desc.sprite = TextBGSprites[0];
                DialogImage.sprite = DialogSprites[0];
                DialogStarImage.sprite = DialogStarSprites[0];
                SuccessText.SetActive(true);
                FailText.SetActive(false);
                break;
        }
    }

    private void OnBtnExitGameClick()
    {
        GameController.ExitGame(() =>
        {
            YandereDataManager.Inst.IsTryAgain = false;
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
            UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.BackToLastWindow();
        });
    }

    private void OnBtnTryAgainClick()
    {
        YandereDataManager.Inst.IsTryAgain = true;
        if (!isPgcEnter)
        {
            var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
            UIManager.Inst.OpenPanel(PanelId.AIGameLoadingPanel,npcInfo.id);
        }
        AIGameController.Inst.Restart();
    }
    
}
