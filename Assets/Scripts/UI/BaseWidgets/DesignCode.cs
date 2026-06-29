using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using UGCAsset;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class DesignCode : MonoBehaviour
{
    public Text codeName;
    public Text codeText;
    public CButton copyBtn;
    public CButton showcodeBtn;
    public CButton hidecodeBtn;
    public Image copyImage;
    public Image bgImage;
    private UgcBaseInfo _ugcBaseInfo;

    public void SetCodeInfo(UgcBaseInfo info, PaymentInfo paymentInfo = null)
    {
        _ugcBaseInfo = info;
        if (info==null||string.IsNullOrEmpty(info.designCode))
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        if (info is SkinInfo)
        {
            codeName.SetLocalText("皮肤设计码:");
            
        }
        else if(info is PropInfo)
        {
            codeName.SetLocalText("素材设计码:");
        }
        else if(info is MapInfo)
        {
            codeName.SetLocalText("地图设计码:");
        }
        else if(info is MaterialInfo)
        {
            codeName.SetLocalText("材质设计码:");
        }
        else if(info is MusicScoreInfo)
        {
            codeName.SetLocalText("乐谱设计码:");
        } 
        else if (info is ToneInfo)
        {
            codeName.SetLocalText("音色设计码:");
        }
        else if (info is PoseInfo)
        {
            codeName.SetLocalText("姿势设计码:");
        }
        else if (info is AnimInfo)
        {
            codeName.SetLocalText("动作设计码:");
        }
        else if (info is AnimMusicInfo)
        {
            codeName.SetLocalText("音色设计码:");
        }
        else if (info is AINpcInfo)
        {
            codeName.SetLocalText("NPC设计码:");
        }
        else if (info is VehicleInfo)
        {
            codeName.SetLocalText("载具设计码:");
        }
        else if(info is OCTheatreAvatarInfo)
        {
            codeName.SetLocalText("演员设计码:");
        }
        else if(info is OCTheatreInfo)
        {
            codeName.SetLocalText("剧本设计码:");
        }
        else if (info is CabinCharacterUgcInfo)
        {
            codeName.SetLocalText("伙伴设计码:");
        }
        else if (info is CabinToneInfo)
        {
            codeName.SetLocalText("音色设计码:");
        }
        showcodeBtn.gameObject.SetActive(false);
        hidecodeBtn.gameObject.SetActive(false);
        codeText.SetText(info.designCode);
        copyBtn.onClick.RemoveAllListeners();
        copyBtn.onClick.AddListener(() =>
        {
            GUIUtility.systemCopyBuffer = info.designCode;
            TipPanel.ShowToast("已复制，去分享给好友吧");
        });

        if (paymentInfo is { currencyType: CurrencyType.Gem })
        {
            copyImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common,
                "Copy_white", gameObject);
            codeText.color = Color.white;
            codeName.color = Color.white;
            bgImage.color = DataUtil.DeSerializeColorCheckHash("#8250EE");
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());
        HandlePrivateOrderLogic();
    }

    private void HandlePrivateOrderLogic()
    {
        if (_ugcBaseInfo is SkinInfo == false)
            return;
        
        var skinInfo = (SkinInfo)_ugcBaseInfo;
        if (skinInfo.isPrivateOrder == 1)
        {
            codeText.SetText("*******");
            copyBtn.enabled = false;
            showcodeBtn.gameObject.SetActive(true);
            showcodeBtn.onClick.AddListener(OnShowCodeBtnClick);
            hidecodeBtn.onClick.AddListener(OnHideCodeBtnClick);
        }
    }

    private void OnShowCodeBtnClick()
    {
        codeText.SetText(_ugcBaseInfo.designCode);
        copyBtn.enabled = true;
        
        showcodeBtn.gameObject.SetActive(false);
        hidecodeBtn.gameObject.SetActive(true);
    }

    private void OnHideCodeBtnClick()
    {
        codeText.SetText("*******");
        copyBtn.enabled = false;
        
        showcodeBtn.gameObject.SetActive(true);
        hidecodeBtn.gameObject.SetActive(false);
    }
}
