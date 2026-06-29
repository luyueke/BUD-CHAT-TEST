using Network.Http;
using Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using Message;

// ── aiCreditDetail 接口数据模型 ─────────────────────────────────────────────

public class AICreditDetailDaily
{
    public int usedAmount;
    public int totalAmount;
}

public class AICreditDetailExpiringItem
{
    public int amount;
    public string desc;
    public int duration; // 剩余秒数
}

public class AICreditDetailExpiring
{
    public int totalAmount;
    public List<AICreditDetailExpiringItem> details;
}

public class AICreditDetailPermanent
{
    public int totalAmount;
}

public class AICreditDetailData
{
    public AICreditDetailDaily daily;
    public AICreditDetailExpiring expiring;
    public AICreditDetailPermanent permanent;
}

// ── 旧礼包接口数据模型（保留兼容）───────────────────────────────────────────

/// <summary>伙伴礼包赠送的限时能量条目（按到期时间升序排列，临期优先消耗）</summary>
public class AICreditGiftPackInfo
{
    public string packName;      // 礼包名称，如 "xx礼包"
    public int remainAmount;     // 剩余能量值
    public int expireTimestamp;  // 过期 Unix 时间戳（秒）
}

public class AICreditGiftPackListData
{
    public List<AICreditGiftPackInfo> list;
}

public class AICreditManager : GlobalInstance<AICreditManager>
{
    /// <summary>是否AI能量月卡用户（buddyMonthlyCard.isValid == 1）</summary>
    public bool isCreditMonthCardUser()
    {
        return IAPDataManager.Inst.subscribeStatusRsp?.buddyMonthlyCard?.isValid == 1;;
    }

    /// <summary>获取 AI 能量明细（daily/expiring/permanent）</summary>
    public void GetAICreditDetail(Action<AICreditDetailData> onSuccess, Action<string> onFail = null)
    {
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.aiCreditDetail,
            HttpMethod.GET,
            "",
            onReceive: content =>
            {
                var data = JsonConvert.DeserializeObject<AICreditDetailData>(content);
                onSuccess?.Invoke(data ?? new AICreditDetailData());
            },
            onFail: err => onFail?.Invoke(err)
        );
    }


    /// <summary>将剩余秒数格式化为"x天后过期"等中文描述</summary>
    public static string FormatDuration(int durationSeconds)
    {
        if (durationSeconds <= 0) return "<color=#FF5555>(即将过期)</color>";
        int days = durationSeconds / 86400;
        if (days == 0) return "<color=#FF5555>(今天过期)</color>";
        if (days == 1) return "<color=#FF5555>(一天后过期)</color>";
        return $" {days}天后过期";
    }

    /// <summary>将过期时间戳格式化为"x天后过期"等中文描述</summary>
    public static string FormatExpiry(int expireTimestamp)
    {
        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int diff = expireTimestamp - now;
        if (diff <= 0) return "<color=#FF5555>(即将过期)</color>";
        int days = diff / 86400;
        if (days == 0) return "<color=#FF5555>(今天过期)</color>";
        if (days == 1) return "<color=#FF5555>(一天后过期)</color>";
        return $" {days}天后过期";
    }

    public override void Release()
    {
        base.Release();
    }
}
