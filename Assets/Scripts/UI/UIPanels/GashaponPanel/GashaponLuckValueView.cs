using System;
using GameData.Gashapon;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GashaponLuckValueView : MonoBehaviour
{
    [SerializeField] private Text gashaponLuckValue;
    [SerializeField] private Text SRemainingTimes;


    private void Start() {
#if PACKAGE_TYPE_US
        // 海外服重新刷新一遍 默认文案
        gashaponLuckValue.SetLocalText("幸运值进度：{0}/{1}",0,80);
        SRemainingTimes.SetLocalText("今日剩余次数：{0}",50);
#endif
        SRemainingTimes.gameObject.SetActive(false);
    }

    public void Refresh(string gashaId, CurrencyType currencyType)
    {
        if (string.IsNullOrEmpty(gashaId))
        {
            return;
        }
        
        //不显示剩余次数
        SRemainingTimes.gameObject.SetActive(false);
        JObject req = new JObject()
        {
            ["lotteryId"] = gashaId
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.gashaInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            (response) =>
            {
                GashaponInfoRsp rsp = JsonConvert.DeserializeObject<GashaponInfoRsp>(response);
                if (this != null)
                {
                    AdjustUI(rsp, currencyType);
                }
            }, (fail) => { LoggerUtils.Log("扭蛋信息拉取失败 ：" + fail); }, retryCount: 3);
    }

 
    public void AdjustUI(GashaponInfoRsp infoRsp, CurrencyType currencyType = CurrencyType.None)
    {
        if (infoRsp == null)
        {
            return;
        }

        var start = infoRsp.luckyProgressInfo?.start ?? 0;
        var end = infoRsp.luckyProgressInfo?.end ?? 80;
        var restGachaNum = infoRsp.restGachaNum;

        // 幸运值进度：0/80
        // 今日剩余次数：29
        var fixedLuck = Math.Min(end, start);
        fixedLuck = Math.Max(0, fixedLuck);
        gashaponLuckValue.SetLocalText("幸运值进度：{0}/{1}",fixedLuck,end);
        SRemainingTimes.SetLocalText("今日剩余次数：{0}",restGachaNum);
    }
}
