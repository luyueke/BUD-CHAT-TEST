using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using System;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
public class InteractNode1RoleItem : MonoBehaviour
{
    public Image iconImage;
    public RemoteImageBehaviour remoteIcon;
    public Text text;

    public Button addBtn;
    public Button deleteBtn;
    public GameObject selectedGo;

    public Action<pEmoteData> onAddAction; //添加事件
    public Action<pEmoteData> onDeleteAction; //删除事件
    pEmoteData data;

    public string id;


    void Awake()
    {
        addBtn.onClick.AddListener(OnAddBtnClick);
        deleteBtn.onClick.AddListener(OnDeleteBtnClick);
        deleteBtn.gameObject.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        selectedGo.SetActive(selected);
    }

    public void Init(pEmoteData _pEmoteData)
    {
        data = _pEmoteData;
        selectedGo.SetActive(false);
        if (!string.IsNullOrEmpty(data.emoteId))
        {
            iconImage.gameObject.SetActive(true);
            remoteIcon.gameObject.SetActive(false);
            iconImage.sprite = PgcUtils.LoadEmoteIcon(data.emoteId, this.gameObject);
            if (iconImage.sprite == null)
            {
                // leisure/default 等特殊待机 ID 无配置表记录，直接以 emoteId 为 Sprite 名查图集
                var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
                iconImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, data.emoteId, this.gameObject);
            }
            string emoteName = PgcUtils.GetEmoteName(data.emoteId);
            if (string.IsNullOrEmpty(emoteName))
            {
                if (data.emoteId == "leisure")
                {
                    emoteName = "待机";
                }
                else if (data.emoteId == "default")
                {
                    emoteName = "站立";
                }
                else
                {
                    emoteName = data.emoteId;
                }
            }
            text.text = emoteName.Length > 6 ? emoteName.Substring(0, 5) + "…" : emoteName;


        }
        else if (!string.IsNullOrEmpty(_pEmoteData.ugcData?.cover))
        {
            remoteIcon.gameObject.SetActive(true);
            remoteIcon.Load(_pEmoteData.ugcData.cover, true);
            iconImage.gameObject.SetActive(false);
            AssetsDataManager.GetUgcAnimInfo(_pEmoteData.ugcData.id, (isSuccess, serverData) =>
            {
                if (!isSuccess || serverData == null) return;
                if (this == null) return;
                var name = serverData.animInfo?.name ?? string.Empty;
                text.text = name.Length > 6 ? name.Substring(0, 5) + "…" : name;
            });
        }

    }

    void OnDeleteBtnClick()
    {
        onDeleteAction?.Invoke(data);
    }

    void OnAddBtnClick()
    {
        onAddAction?.Invoke(data);
    }




}
