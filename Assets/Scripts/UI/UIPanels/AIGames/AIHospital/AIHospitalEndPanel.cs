using AIGame.Base;
using Game.Base;
using Network.Http;
using Network;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Game;
using Newtonsoft.Json.Linq;
using System.Linq;
using Message;
using System;
using Basic.Utils;

public enum AIHospitalEnd
{
    Failed = 0,
    Completed = 1,
}

[Serializable]
public class S9UgcShareRtn
{   
    //todo 转换格式        初始化类
    public int result;
    public string rmsg;
    public string requestId;
    public Data data;
    public bool isFirst;
    public class Data
    {
        public List<Rewards> rewards;
    }

    public class Rewards
    {
        public int rewardType;
        public int amount;
    }
}



public enum EShareType
{
    /// <summary>
    /// 普通ugc地图分享
    /// </summary>
    Normal,
    /// <summary>
    /// 游玩分享
    /// </summary>
    Play,
    /// <summary>
    /// 首通分享
    /// </summary>
    Pass
}


public class AIHospitalEndPanel : BasePanel<AIHospitalEndPanel>
{
    // Start is called before the first frame update
    [SerializeField] private ActivityCenterBgItem _bg;
    [SerializeField] private List<string> _completedImgs;
    [SerializeField] private List<string> _failedImgs;
    [SerializeField] private string _rgbHexValue;

    [SerializeField] private Transform _completedPanel;
    [SerializeField] private Transform _failedPanel;

    [SerializeField] private string[] _completedBtnHexColor;

    [SerializeField] private CButton _exitBtn;
    [SerializeField] private CButton _tryAgainBtn;

    [SerializeField] private CButton _shareBtn;
    [SerializeField] private Image _shareTipsImg;
    [SerializeField] private Text _shareTipsTxt;

    private AIHospitalEnd _gameState;


    private AIHospitalGame _aiGame;
    private AIGamePassStatus _aiGamePassStatus;
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        _aiGame = AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
        if (_aiGame==null)
        {
            LoggerUtils.LogError("_aigame 获取失败");
        }
        AIHospitalEnd end = (AIHospitalEnd)args[0];
        InitUI(end);
        PopShareGiftPanel();
    }

    private void InitUI(AIHospitalEnd eEnd)
    {
        AddListener();
        if (_bg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        _bg.InitCustomBgItem(_rgbHexValue, atlasPath, eEnd == AIHospitalEnd.Completed ? _completedImgs : _failedImgs);

        _completedPanel.gameObject.SetActive(eEnd == AIHospitalEnd.Completed);
        _failedPanel.gameObject.SetActive(eEnd == AIHospitalEnd.Failed);
        SetBtnHexColor(_tryAgainBtn.image, _completedBtnHexColor[(int)eEnd]);

        _bg.gameObject.SetActive(true);
        _bg.transform.SetAsFirstSibling();

        _gameState = eEnd;
        ReportGameResult(eEnd);
    }

    private void AddListener()
    {
        _exitBtn.onClick.AddListener(OnExitBtnClick);
        _tryAgainBtn.onClick.AddListener(OnTryAgainBtnClick);
        _shareBtn.onClick.AddListener(ShareToThirdParty);
    }

    private void RemoveListener()
    {
        _exitBtn.onClick.RemoveListener(OnExitBtnClick);
        _tryAgainBtn.onClick.RemoveListener(OnTryAgainBtnClick);
        _shareBtn.onClick.RemoveListener(ShareToThirdParty);
    }

    public void SetBtnHexColor(Image image, string hexColor)
    {
        if (!hexColor.StartsWith("#"))
            hexColor = "#" + hexColor;

        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            image.color = color;
        }
    }

    public void OnExitBtnClick()
    {
        GameController.ExitGame(() =>
        {
            //YandereDataManager.Inst.IsTryAgain = false;
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
            UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.BackToLastWindow();
            MessageHelper.Broadcast(MessageName.OnS9UpdateGiftState);
        });
    }

    public void OnTryAgainBtnClick()
    {
        //YandereDataManager.Inst.IsTryAgain = true;
        AIGameController.Inst.Restart();
    }

    private void ReportGameResult(AIHospitalEnd end)
    {
        //var _aiGame = AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
        S9GameReport req = new()
        {
            npcId = string.Empty,
            gameId = (int)PGCGameType.AIHospital,
            duration = (int)(GameUtils.GetTimeStamp() - _aiGame.GetGameStartTime()),
            result = (int)end,
            conversationId = string.Empty,
            mapId = _aiGame.CurMapID
        };
        var paramStr = JsonConvert.SerializeObject(req);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIResult, HttpMethod.POST, paramStr, (content) =>
        {
            LoggerUtils.Log("s9游戏结果上报成功");
        }, (err) => { LoggerUtils.Log("s9游戏结果上报失败", err); }, null, 0, 3);

        string localKey = AccountDataManager.Inst.UserInfo.uid + AIGameHospitalConfig.firstPassMapIDKey;
        if (end == AIHospitalEnd.Completed && string.IsNullOrEmpty(PlayerPrefs.GetString(localKey, string.Empty)))
        {
            PlayerPrefs.SetString(localKey, _aiGame.CurMapID);
        }
    }


    /// <summary>
    /// 分享游戏结果到第三方平台
    /// </summary>
    public void ShareToThirdParty()
    {
#if UNITY_ANDROID&&! UNITY_EDITOR
        TipPanel.ShowToast("功能敬请期待");
        return;
#endif

        UIManager.Inst.OpenPanel(PanelId.AIHospitalSharePanel,_aiGame.CurMapID);
    }


    public void PopShareGiftPanel()
    {
        var _aiGame = AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
        if (_aiGame == null||_aiGame.isPgcEnter)
        {
            _shareBtn.gameObject.SetActive(false);
            _shareTipsImg.gameObject.SetActive(false);
            return;
        }
        _shareBtn.gameObject.SetActive(true);
        _shareTipsImg.gameObject.SetActive(true);
        GetUserImageStatus();
    }


    private void GetUserImageStatus()
    {
        // 构建请求参数
        JObject req = new JObject()
        {
            ["targetUid"] = AccountDataManager.Inst.Uid,  // 假设需要用户ID
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.getUserImage,         // 接口地址
            HttpMethod.GET,                     // 请求方法
            JsonConvert.SerializeObject(req),   // 请求参数
            OnGetUserImageSuccess,              // 成功回调
            OnGetUserImageFail                  // 失败回调
        );
    }

    private void OnGetUserImageSuccess(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            OnGetUserImageFail("Empty Response");
            return;
        }

        try
        {
            // 解析返回数据
            var response = JsonConvert.DeserializeObject<GetImageRes>(content);
            if (response != null && response.aiGameStatus != null)
            {
                // 根据返回数据设置状态
                 _aiGamePassStatus = response.aiGameStatus.FirstOrDefault(x => x.gameId == (int)PGCGameType.AIHospital); // 查找gameId为2的数据
                if (_aiGamePassStatus!=null)
                {
                    int tipsStyle = 0;
                    if (!_aiGamePassStatus.hasPlayedPgc && !_aiGamePassStatus.hasFinUgc)
                    {
                        if (_aiGame.GetGamePassState())
                        {
                            UIManager.Inst.OpenPanel(PanelId.AIHospitalFirstPlayGiftPanel,null, OpenFinishGiftPanel);
                        }
                        else
                            UIManager.Inst.OpenPanel(PanelId.AIHospitalFirstPlayGiftPanel, null, ShareToThirdParty);
                        tipsStyle = 1;
                    }
                    else if (!_aiGamePassStatus.hasFinUgc)
                    {
                        UIManager.Inst.OpenPanel(PanelId.AIHospitalFirstPassGiftPanel,null,ShareToThirdParty);
                        tipsStyle = 1;
                    }
                    //else 
                    if (_aiGamePassStatus.hasDailyShare)
                    {
                        tipsStyle = 2;
                    }
                    else
                        tipsStyle = 0;
                    _shareTipsImg.gameObject.SetActive(tipsStyle>0);
                    _shareTipsTxt.text = tipsStyle == 1 ? "首次分享得优优币" : "每日首次分享有奖";
                }
            }
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError($"Parse user image response failed: {e.Message}");
        }
    }

    private void OnGetUserImageFail(string error)
    {
        LoggerUtils.LogError($"Get user image failed: {error}");
    }

    private void OpenFinishGiftPanel()
    {
        _aiGamePassStatus.hasFinUgc = false;
        if (_aiGamePassStatus!=null&&!_aiGamePassStatus.hasFinUgc)
        {
            UIManager.Inst.OpenPanel(PanelId.AIHospitalFirstPassGiftPanel,null,ShareToThirdParty);
        }
    }

    protected override void OnDestroy()
    {
        RemoveListener();
    }

}
