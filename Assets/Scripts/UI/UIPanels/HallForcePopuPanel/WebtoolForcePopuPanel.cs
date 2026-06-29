using Com.TheFallenGames.OSA.Util.IO;
using Game.Audio;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using View.UI.PopupPanelSystem.Base;
using View.UI.PopupPanelSystem.Data;

public class WebtoolForcePopuPanel : BasePopupPanel<WebtoolForcePopuPanel>
{
    private Image GoBg;
    private Button BtnBg;
    private Toggle DismissToday;
    private Text TextAccept;
    public RemoteImageBehaviour remoteBev;

    private WebtoolNewsData curPopupData;
    private string _curCoverUrl;
    private bool isTodayOn;
    

    public override void OnCreate()
    {   
        base.OnCreate();

        CloseBtn = GameObjectEx.FindChildByName(this.transform, "CloseBtn").GetComponent<Button>();
        GoBtn = GameObjectEx.FindChildByName(this.transform, "GoBtn").GetComponent<Button>();
        BgMask = GameObjectEx.FindChildByName(transform, "BgMask")?.GetComponent<Button>();
        GoBg = GameObjectEx.FindChildByName(this.transform, "GoBtn").GetComponent<Image>();
        DismissToday = GameObjectEx.FindChildByName(this.transform, "DismissToday").GetComponent<Toggle>();
        TextAccept = GameObjectEx.FindChildByName(this.transform, "txtAccept").GetComponent<Text>();
        BtnBg = remoteBev.transform.GetComponent<Button>();
        BtnBg.onClick.RemoveAllListeners();
        BtnBg.onClick.AddListener(OnGoBtnClick);
        CloseBtn.onClick.RemoveAllListeners();
        CloseBtn.onClick.AddListener(OnCloseBtnClick);
        GoBtn.onClick.RemoveAllListeners();
        GoBtn.onClick.AddListener(OnGoBtnClick);
        if (BgMask != null)
        {
            BgMask.onClick.RemoveAllListeners();
            BgMask.onClick.AddListener(OnCloseBtnClick);
        }
        
        DismissToday.onValueChanged.AddListener(isOn =>
        {
            isTodayOn = isOn;
        });
        remoteBev.gameObject.SetActive(false);

    }

    protected override void OnDestroy()
    {   
        base.OnDestroy();
        
    }

    public void SetPopupData(WebtoolNewsData data)
    {
        isTodayOn = false;
        curPopupData = data;
        // 缓存弹窗事件
        EventTracking.LoadEvent.AddReportData(curPopupData.popupId, -1);
        //推送数据
        EventTracking.LoadEvent.pushreportData();
        DownLoadTex(data.coverUrl);
        
        GoBg.color = DataUtil.DeSerializeColorCheckHash(data.buttonColor);
        switch ((NewsMainType)data.skipType)
        {
            case NewsMainType.TipsUpdate:
                TextAccept.text = "更新";
                CloseBtn.gameObject.SetActive(true);
                DismissToday.gameObject.SetActive(true);
                break;
            case NewsMainType.ForceUpdate:
                TextAccept.text = "更新";
                CloseBtn.gameObject.SetActive(false);
                DismissToday.gameObject.SetActive(false);
                break;
            default:
                TextAccept.text = "查看";
                CloseBtn.gameObject.SetActive(true);
                DismissToday.gameObject.SetActive(true);
                break;
        }
    }

    private void DownLoadTex(string coverUrl)
    {
        _curCoverUrl = coverUrl;
   
        // remoteBev.gameObject.SetActive(false);
        remoteBev.Load(_curCoverUrl, true, (fromCache, success) => {
            remoteBev.gameObject.SetActive(true);
        });
    }

    private void OnCloseBtnClick()
    {   


        if ((NewsMainType)curPopupData.skipType == NewsMainType.ForceUpdate)
        {
            return;
        }
        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_Cancel_C2);
        CloseSelf();
        OnClose?.Invoke();

        if (isTodayOn)
        {
            OnFinish?.Invoke();
        }
        
    }

    public void OnGoBtnClick()
    {
        if ((NewsMainType)curPopupData.skipType == NewsMainType.ForceUpdate)
        {
            WebtoolNewsSkipManager.Inst.HandleSkip(curPopupData);
            return;
        }
        //缓存点击事件
        EventTracking.LoadEvent.AddReportData(-1, curPopupData.popupId);
        //推送数据
        EventTracking.LoadEvent.pushreportData();

        CloseSelf();
       // OnClose?.Invoke();
        OnFinish?.Invoke();

        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ConfirmButton_A2);
        WebtoolNewsSkipManager.Inst.HandleSkip(curPopupData);
        
        isTodayOn = true;
        // JObject req = new JObject
        // {
        //     ["popupId"] = curPopupData.popupId,
        // };
        //
        // NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.HideLobbyPopup,
        //     HttpMethod.POST,
        //     JsonConvert.SerializeObject(req),
        //     onReceive: arg0 =>
        //     {
        //       
        //     }, onFail: arg0 =>
        //     {
        //     });
        
        
    }
}