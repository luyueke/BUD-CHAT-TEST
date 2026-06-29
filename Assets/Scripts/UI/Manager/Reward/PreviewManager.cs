using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.PgcData;
using UnityEngine;

public class PreviewManager : GlobalInstance<PreviewManager>
{
    public void ShowPreview(string pgcId, string name, string spriteatlasPath, string color, List<string> icons)
    {
        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        if (string.IsNullOrEmpty(spriteatlasPath))
        {
            spriteatlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
        }
        if (string.IsNullOrEmpty(color))
        {
            color = "#a645f6";
        }
        if (icons == null)
        {
            icons = new List<string>();
            icons.Add("pinktask_bg1");
            icons.Add("pinktask_bg2");
        }

        panel.SetEventPreviewBg(new List<string> { pgcId }, name, spriteatlasPath, color, icons);
    }

    public void ShowPreview(RewardPreviewInfo rewardPreviewInfo)
    {
        var previewType = rewardPreviewInfo.GetPreviewType();
        switch (previewType)
        {
            case PreviewType.Currency:
                ShowCurrencyPreview((CurrencyType)rewardPreviewInfo.currencyType);
                break;
            case PreviewType.Avatar:
                ShowEmotePreview(rewardPreviewInfo);
                break;
            case PreviewType.Emote:
                ShowEmotePreview(rewardPreviewInfo);
                break;
            case PreviewType.AvatarFrame:
                ShowAvatarFramePreview(rewardPreviewInfo);
                break;
            case PreviewType.ChatBubble:
                ShowChatBubblePreview(rewardPreviewInfo);
                break;
            case PreviewType.HomePageSkin:
                ShowHomePageSkinPreview(rewardPreviewInfo);
                break;
        }

    }
    public void ShowAvatarFramePreview(int id)
    {
        ShowAvatarFramePreviewById(id);
    }
    //货币预览
    public void ShowCurrencyPreview(CurrencyType rewardType)
    {
        int type = (int)rewardType;
        if (true || type != 1001)
        {
            //这么处理是因为有些货币类型和物品类型对应的枚举值不同
            if (type == 22) type = 9;
            if (type == 7) type = 5;

            UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel, (CurrencyType)type);
        }
        else
        {
            //TODO: 泰迪套装 是做了什么处理?
            // var bundleShowpanel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
            // bundleShowpanel.SetEventPreview(new List<string>() {
            //         "10400490",
            //         "10900490"}, "泰迪套装",Icon5.sprite, "#BF8DFF",bgRawTex.texture);
        }
    }
    //动作预览
    public void ShowEmotePreview(RewardPreviewInfo rewardPreviewInfo)
    {
        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        if (string.IsNullOrEmpty(rewardPreviewInfo.spriteatlasPath))
        {
            var spriteatlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
            var icons = rewardPreviewInfo.icons;
            if (icons == null)
            {
                icons = new List<string>();
                // icons.Add("pinktask_bg1");
                // icons.Add("pinktask_bg2");
            }
            panel.SetEventPreviewBg(new List<string> { rewardPreviewInfo.pgcId }, rewardPreviewInfo.name, spriteatlasPath, rewardPreviewInfo.color, icons);
        }
        else
        {
            panel.SetEventPreview(new List<string> { rewardPreviewInfo.pgcId }, rewardPreviewInfo.name, "", rewardPreviewInfo.spriteatlasPath, "", rewardPreviewInfo.color, "bg");
        }
    }

    //头像框预览
    public void ShowAvatarFramePreview(RewardPreviewInfo rewardPreviewInfo)
    {
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        try
        {
            panel.PreviewAvatarFrame((AvatarFrameType)UserUIWidgetManager.Inst.GetAvatarTypeIdByPgcId(rewardPreviewInfo.pgcId));
        }
        catch (System.Exception e)
        {
            Debug.Log("ShowAvatarFramePreview rewardPreviewInfo.pgcId: " + rewardPreviewInfo.pgcId);
            Debug.Log("ShowAvatarFramePreview error: " + e.Message);
            throw;
        }

    }

    public void ShowAvatarFramePreviewById(int id)
    {
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        try
        {
            panel.PreviewAvatarFrame((AvatarFrameType)UserUIWidgetManager.Inst.GetAvatarTypeIdById(id));
        }
        catch (System.Exception e)
        {
            Debug.Log("ShowAvatarFramePreview rewardPreviewInfo.id: " + id);
            Debug.Log("ShowAvatarFramePreview error: " + e.Message);
            throw;
        }

    }

    //聊天气泡预览
    public void ShowChatBubblePreview(RewardPreviewInfo rewardPreviewInfo)
    {
        var bubblePreviewPanel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        try
        {
            bubblePreviewPanel.PreviewChatBubble((ChatBubblesType)UserUIWidgetManager.Inst.GetChatBubbleTypeIdByPgcId(rewardPreviewInfo.pgcId), rewardPreviewInfo.title, rewardPreviewInfo.des);

        }
        catch (System.Exception e)
        {
            Debug.Log("ShowChatBubblePreview rewardPreviewInfo.pgcId: " + rewardPreviewInfo.pgcId);
            Debug.Log("ShowChatBubblePreview error: " + e.Message);
            throw;
        }
    }

    public void ShowAvatarPreview(RewardPreviewInfo rewardPreviewInfo)
    {
    //    var bundleShowpanel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
    //             bundleShowpanel.SetEventPreview(new List<string>() {
    //                 "10900492"}, "童心甜梦睡帽", "Bundle_103", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
    //             bgRawTex.texture);
    }

    //主页皮肤预览
    public void ShowHomePageSkinPreview(RewardPreviewInfo rewardPreviewInfo)
    {

    }


}

public class RewardPreviewInfo
{
    public BUDRewardType rewardType;
    public CurrencyType currencyType;
    public string pgcId;
    public string name;
    public string spriteatlasPath;
    public string color;
    public List<string> icons;

    public string exId; //某个特定奖励类型  对应的id  辅助展示

    public string title;
    public string des;
    public RewardPreviewInfo(BUDRewardType rewardType, CurrencyType currencyType, string pgcId, string name, string spriteatlasPath = "", string color = "#a645f6", List<string> icons = null)
    {
        this.rewardType = rewardType;
        this.currencyType = currencyType;
        this.pgcId = pgcId;
        this.name = name;
        this.spriteatlasPath = spriteatlasPath;
        this.color = color;
        this.icons = icons;
    }

    public void SetExId(string tempExId)
    {
        exId = tempExId;
    }

    public void SetTitleAndDes(string tempTitle, string tempDes)
    {
        title = tempTitle;
        des = tempDes;
    }

    public PreviewType GetPreviewType()
    {
        if (currencyType != CurrencyType.None)
        {
            //货币类型
            return PreviewType.Currency;
        }
        else if (rewardType == BUDRewardType.RewardAvatarFrame)
        {
            //头像框
            return PreviewType.AvatarFrame;
        }
        else if (rewardType == BUDRewardType.RewardChatBubbles)
        {
            //聊天气泡
            return PreviewType.ChatBubble;
        }
        else if (rewardType == BUDRewardType.RewardHomepageSkin)
        {
            //主页皮肤
            return PreviewType.HomePageSkin;
        }
        else if (rewardType == BUDRewardType.RewardPgcResource)
        {
            //动作还是 avatar
            var config = DataTables.GetGameResData(pgcId);
            switch ((ResourceType)config.ResourceType)
            {
                case ResourceType.Avatar:
                    return PreviewType.Avatar;
                case ResourceType.Emote:
                    return PreviewType.Emote;
            }
        }
        return PreviewType.None;
    }
}

public enum PreviewType
{
    None,
    Currency,
    Emote,
    AvatarFrame,
    ChatBubble,
    HomePageSkin,
    Avatar,
    Prop, //todo
}

