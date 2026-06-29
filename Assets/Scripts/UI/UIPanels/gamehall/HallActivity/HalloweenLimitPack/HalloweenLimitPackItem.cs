using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class HalloweenLimitPackItem : MonoBehaviour
{

    private int _index = 1;

    public CButton btnItem;

    public RawImage bgRawTex;

    string spriteatlasPath = "Assets/Loadable/UI/UIPanel/HalloweenLimitPackView/HalloweenLimitPackView.spriteatlas";

    private void SetPriview(int index)
    {
        switch (index)
        {
            case 1:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "10800093" }, "魔女罗斯玛丽头发", "icon_1", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 2:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "10400368" }, "魔女罗斯玛丽裙子", "icon_2", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 3:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "10900367" }, "魔女罗斯玛丽帽子", "icon_3", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 4:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "11000153" }, "魔法药水", "icon_4", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 5:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "12000017" }, "魔女罗斯玛丽手套", "icon_5", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 6:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "11300267" }, "魔女罗斯玛丽靴子", "icon_6", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 7:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "10100076" }, "魔女罗斯玛丽斗篷", "icon_7", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 8:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "10600084" }, "魔女罗斯玛丽眼睛", "icon_8", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 9:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "10200037" }, "魔女罗斯玛丽尾巴", "icon_9", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
            case 10:
                btnItem.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string>() { "40100071" }, "喝咖啡", "icon_10", spriteatlasPath, "万圣节好礼", "#BF8DFF", bgRawTex.texture);
                });
                break;
        }
    }

    public void SetData(int index)
    {
        _index = index;
        SetPriview(index);
    }

}
