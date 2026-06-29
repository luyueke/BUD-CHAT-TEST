using Com.TheFallenGames.OSA.Util.IO;
using System;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
public class RoleInteractNode1RoleItem : MonoBehaviour
{
    public Image iconImage;
    public RemoteImageBehaviour remoteIcon;

    public Button addBtn;

    public GameObject selectedGo;

    public Action<pEmoteData> onAddAction; //添加事件

    pEmoteData data;

    public Text txt_name;

    public GameObject Lock;

    public string id;


    void Awake()
    {
        addBtn.onClick.AddListener(OnAddBtnClick);

    }

    public void SetSelected(bool selected)
    {
        selectedGo.SetActive(selected);
    }

    public void Init(pEmoteData _pEmoteData)
    {
        data = _pEmoteData;
        selectedGo.SetActive(false);
        // 图标
        if (!string.IsNullOrEmpty(data.emoteId))
        {
            iconImage.gameObject.SetActive(true);
            remoteIcon.gameObject.SetActive(false);

            if (data.emoteId == "leisure" || data.emoteId == "default")
            {
                // 特殊待机 ID 无 PGC 配置表记录，直接以 emoteId 为名从图集加载，避免 PgcUtils 内部报错
                var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
                iconImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, data.emoteId, gameObject);
            }
            else
            {
                iconImage.sprite = PgcUtils.LoadEmoteIcon(data.emoteId, gameObject);
            }
        }
        else if (data.ugcData != null && !string.IsNullOrEmpty(data.ugcData.cover))
        {
            remoteIcon.gameObject.SetActive(true);
            remoteIcon.Load(data.ugcData.cover, true);
            iconImage.gameObject.SetActive(false);
        }

        // 动画名
        CabinTools.SetInteractEmoteNameText(
            txt_name,
            data.emoteId,
            data.ugcData?.id,
            data.ugcData?.cover);
    }


    public void RefreshLock(string ugcId, string creator)
    {
        CabinSkinLockHelper.ApplyLock(Lock, ugcId, creator);
    }

    void OnAddBtnClick()
    {
        onAddAction?.Invoke(data);
    }
}
