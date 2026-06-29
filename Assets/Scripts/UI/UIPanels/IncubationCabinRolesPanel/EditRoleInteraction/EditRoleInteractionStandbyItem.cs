using System;
using UnityEngine;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util.IO;
using UI.Manager;
using UI.BaseWidgets;
public class EditRoleInteractionStandbyItem : MonoBehaviour
{
    public Image icon;
    public RemoteImageBehaviour remoteIcon;

    public Button addBtn;

    public GameObject Selected;

    public Text txt_name;
    public LoadingButton btn_del;
    public Action<pEmoteData> onAddAction;
    public Action onDeleteSuccess;

    pEmoteData data;
    bool _isLoop;

    void Awake()
    {
        addBtn.onClick.AddListener(OnAddBtnClick);
        btn_del.onClick.AddListener(OnDelBtnClick);
    }

    public void SetSelected(bool selected)
    {
        Selected.SetActive(selected);
    }

    public void Init(pEmoteData _pEmoteData, bool isLoop)
    {
        data = _pEmoteData;
        _isLoop = isLoop;
        Selected.SetActive(false);

        // 图标
        if (!string.IsNullOrEmpty(data.emoteId))
        {
            icon.gameObject.SetActive(true);
            remoteIcon.gameObject.SetActive(false);

            if (data.emoteId == "leisure" || data.emoteId == "default")
            {
                // 特殊待机 ID 无 PGC 配置表记录，直接以 emoteId 为名从图集加载，避免 PgcUtils 内部报错
                var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
                icon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, data.emoteId, gameObject);
            }
            else
            {
                icon.sprite = PgcUtils.LoadEmoteIcon(data.emoteId, gameObject);
            }
        }
        else if (data.ugcData != null && !string.IsNullOrEmpty(data.ugcData.cover))
        {
            remoteIcon.gameObject.SetActive(true);
            remoteIcon.Load(data.ugcData.cover, true);
            icon.gameObject.SetActive(false);
        }

        // 动画名
        CabinTools.SetInteractEmoteNameText(
            txt_name,
            data.emoteId,
            data.ugcData?.id,
            data.ugcData?.cover);
    }

    void OnAddBtnClick()
    {
        onAddAction?.Invoke(data);
    }

    void OnDelBtnClick()
    {
        onDeleteSuccess?.Invoke();
    }
}
