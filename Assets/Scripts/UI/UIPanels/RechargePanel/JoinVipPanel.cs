
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class JoinVipPanel : BasePanel<JoinVipPanel>
{
    public Text Txt_Title;
    public CButton joinVipBtn;
    public CButton closeBtn;
    public List<GameObject> vipIconList;
    public List<string> vipIconName;
    public override void OnCreate()
    {
        base.OnCreate();
        joinVipBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.VipMonthPack);
        });
        var spriteatlasPath = RechargePanel.RechargePanelAtlas;
        closeBtn.GetComponent<Image>().sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip_close", gameObject);

        for (int i = 0; i < vipIconList.Count; i++)
        {
            if(vipIconName.Count >i)
            {
                vipIconList[i].GetComponent<Image>().sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, vipIconName[i], gameObject);
                var vipImage = vipIconList[i].transform.Find("Image");
                if (vipImage != null)
                {
                    vipImage.GetComponent<Image>().sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip", gameObject);
                }
                
            }    
        }

        closeBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args!=null && args.Length > 0)
        {
            var title = (string)args[0];
            Txt_Title.SetText(title);
            
            var typeList = args[1] as List<JoinVipType>;
            ShowVIPIcon(typeList);
        }
    }

    private void ShowVIPIcon(List<JoinVipType> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            vipIconList[(int)list[i]].SetActive(true);
        }
    }
}
public enum JoinVipType{

    Image,
    MusicScoreTwentyTwoKey,
    MusicScorePartAdd,
    VIP_Tone,
    VIP_InstrumentAnim,
    ChangeUGCType,
    VIP_Long_Time,
    VIP_Multi_Frame,
    VIP_Multi_Prop,
    VIP_Multi_Audiotrack,
    VIP_Multi_Clip,
    VIP_Loop,
    VIP_Multi_Bind,
    VIP_Pose,
    VIP_CameraFilter,
}