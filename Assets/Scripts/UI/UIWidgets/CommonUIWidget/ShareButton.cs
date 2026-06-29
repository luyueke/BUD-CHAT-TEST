using Basic.Utils;
using Game;
using Network.Http;
using Network;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using UI.BaseWidgets;
using UnityEngine;
using Es;
using GameData.Base;

public class ShareButton : CommonUIWidget
{
    [SerializeField]private CButton _shareBtn;

    //public GameObject _;
    public CText Txt_shareAmount;

    public Action<int> energyNumChange;
    private string _ugcId;
    private int _shareAmount;

    private string toUid;

    static string _img_name = "碧优蒂的世界.png";
    static string _destination_path ;

    private bool _bShareMode = false;

    private void Awake()
    {
        _destination_path = Application.persistentDataPath + "/" + _img_name;
        _shareBtn.onClick.AddListener(OnBtnClick);
    }

    public override void SetData(params object[] args)
    {
        base.SetData(args);

        toUid = (string)args[0];
        _ugcId = (string)args[1];
        _shareAmount = (int)args[2];

        RefreshEnergyAmount();
    }

    public void SetLocalTex(Texture2D sourceTex)
    {
        try
        {
            // 直接创建目标纹理
            int width = sourceTex.width;
            int height = sourceTex.height;
            Texture2D tempTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // 获取原始纹理的像素
            Color[] pixels = sourceTex.GetPixels();
            tempTex.SetPixels(pixels);
            tempTex.Apply();

            // 编码并保存
            byte[] bytes = tempTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(_destination_path, bytes);

            // 使用转换后的纹理数据
            System.IO.File.WriteAllBytes(_destination_path, bytes);
            // 清理
            Destroy(tempTex);
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError($"SetLocalTex failed: {e.Message}");
        }
    }

    private void RefreshEnergyAmount()
    {
        var likeNumStr = GameUtils.ToBudCommonNumString(_shareAmount);
        if(Txt_shareAmount!= null) Txt_shareAmount.text = likeNumStr;
    }

    private void OnBtnClick()
    {
//#if UNITY_ANDROID&&! UNITY_EDITOR
//        TipPanel.ShowToast("功能敬请期待");
//        return;
//#endif

        //LoggerUtils.LogError($" version compare needVersion =>{AIGameHospitalConfig.shareNeedVersion} , cur =>{DeviceInfoManager.Inst.DeviceBaseData.version}, ");
        if (DeviceInfoManager.Inst.DeviceBaseData.CompareVersion(AIGameHospitalConfig.shareNeedVersion) >0)
        {
            UIManager.Inst.OpenPanel(PanelId.AIHospitalSharePanel,_ugcId,EShareType.Normal);
            //_bShareMode = true;
            //SunShineNativeShare.instance.ShareSingleFile(_destination_path,SunShineNativeShare.TYPE_IMAGE,"","碧优蒂的世界");
            ShareReq();
        }
        else
            UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel,ForceUpdate.NeedUpdateFeature);
            //SunShineNativeShare.instance.ShareText("","");
            //todo 发起请求统计分享
    }


    private void ShareReq()
    {
        JObject req = new()
        {
            ["id"] = _ugcId,
            ["from"] = new JArray { (int)EShareType.Normal }
        };
        var paramStr = JsonConvert.SerializeObject(req);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UgcGameShare, HttpMethod.POST, paramStr, (content) =>
        {
            var s9UgcShareRtn = JsonConvert.DeserializeObject<S9UgcShareRtn>(content);
            if (s9UgcShareRtn.result != 0)
            {
                LoggerUtils.LogError(s9UgcShareRtn.rmsg);
                return;
            }
            // 转换格式 把s9UgcShareRtn.data.rewards转换为TaskRewardData    
            if (s9UgcShareRtn.data != null && s9UgcShareRtn.data.rewards != null)
            {
                //var rewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                //List<TaskRewardData> rewardList = s9UgcShareRtn.data.rewards.Select(x => new TaskRewardData()
                //{
                //    rewardType = x.rewardType,
                //    num = x.amount
                //}).ToList();
                //rewardPanel.ShowRewards(rewardList);
            }
            if (s9UgcShareRtn.isFirst)
            {
                _shareAmount++;
                RefreshEnergyAmount();
            }
            _bShareMode = false;
            //TipPanel.ShowToast("尝试普通分享成功");
        }, (err) => { LoggerUtils.Log("s9游戏普通分享失败", err); _bShareMode = false; }, null, 0, 3);
    }


    private BudTimer timer;

    private void CheckShareTimer(bool value, string source)
    {
        if (!_bShareMode)
        {
            LoggerUtils.Log("不属于分享模式");
            return;
        }
        if (!value)
        {
            TimerManager.Inst.Stop(timer);
            timer = TimerManager.Inst.RunOnce("shareTimer", 2, ()=>
            {
                LoggerUtils.LogError($"分享定时执行成功 来源 {source} ,value = {value}");
                TipPanel.ShowToast("分享定时执行成功，向服务器请求数据");
            });
        }
        else
        {
            LoggerUtils.LogError($"分享状态被打断 来源 {source} ,value = {value}");
            TipPanel.ShowToast($"分享状态被打断 来源 {source} ,value = {value}");
            TimerManager.Inst.Stop(timer);
            _bShareMode = false;
        }
    }

    public void OnApplicationFocus(bool focus)
    {
        CheckShareTimer(focus, "OnApplicationFocus");
    }

    public void OnApplicationPause(bool pause)
    {
        CheckShareTimer(!pause, "OnApplicationPause");
    }
}
