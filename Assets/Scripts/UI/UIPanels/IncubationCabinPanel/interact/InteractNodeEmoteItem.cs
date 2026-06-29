using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using System;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
public class InteractNodeEmoteItem : MonoBehaviour
{
    public GameObject NullBg;
    public GameObject DataBg;

    public Image iconImg;
    public Text nameText;
    public RemoteImageBehaviour remoteIcon;

    public Button selectBtn;

    public GameObject selectImgGo;
    public GameObject[] multiSelectGo;

    /// <summary>已添加状态标签，由预制体绑定；为 null 时忽略</summary>
    public GameObject addedLabelGo;

    private Action<GoodsData> onSelectAction; //选择事件

    private GoodsData goodsData;


    void Awake()
    {
        selectImgGo.SetActive(false);
        selectBtn.onClick.AddListener(OnSelectBtnClick);
    }

    public void Init(GoodsData data, bool isPgc, bool isMultiSelect, Action<GoodsData> onSelectAction)
    {
        goodsData = data;
        bool hasData = !string.IsNullOrEmpty(goodsData.Id);
        NullBg.SetActive(!hasData);
        DataBg.SetActive(hasData);

        // 已添加状态：显示"已添加"标签（反选提示），选择按钮保持可见，允许选中后反选删除
        bool isAdded = data.IsAdded;
        if (addedLabelGo != null)
        {
            addedLabelGo.SetActive(isAdded);
        }
        this.onSelectAction = onSelectAction;

        foreach (var item in multiSelectGo)
        {
            item?.SetActive(isMultiSelect);
        }

        if (nameText != null)
        {
            string rawName;
            if (!string.IsNullOrEmpty(data.Name))
            {
                rawName = data.Name;
            }
            else if (data.Assets?.Count > 0)
            {
                rawName = data.Assets[0].Name ?? string.Empty;
            }
            else
            {
                rawName = string.Empty;
            }
            nameText.text = FormatName(rawName);
        }

        if (isPgc)
        {
            iconImg.gameObject.SetActive(true);
            remoteIcon.gameObject.SetActive(false);

            if (data.ButtonType == ButtonType.EmoteIdle)
            {
                var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
                string spriteName = data.Id;
                if (data.ProductId != 0)
                {
                    spriteName = data.Id + data.ProductId;
                }
                var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, gameObject);
                if (sprite != null) iconImg.sprite = sprite;
            }
            else
            {
                if (data.Assets?.Count > 0)
                {
                    var resourceType = data.Assets[0].ResourceType;
                    var sprite = PgcUtils.GetIconSpriteByPgcId(data.Id, gameObject);
                    if (sprite != null) iconImg.sprite = sprite;
                }
            }
        }
        else
        {
            if (data.Assets?.Count > 0)
            {
                var assetsData = data.Assets[0];
                if (assetsData.UgcInfo != null)
                {
                    TrySetNameText(assetsData.UgcInfo.UgcInfo?.name);
                    var cover = assetsData.UgcInfo.UgcInfo?.cover;
                    if (!string.IsNullOrEmpty(cover))
                    {
                        remoteIcon.Load(cover, onCompleted: (bool fromCache, bool success) =>
                        {
                            if (this == null) return;
                            iconImg.gameObject.SetActive(false);
                            remoteIcon.gameObject.SetActive(true);
                        });
                    }
                }
                else
                {
                    // UgcInfo 为空（购买的动画来自背包路径），异步拉取封面和名称
                    AssetsDataManager.GetUgcAnimInfo(assetsData.Id, (isSuccess, serverData) =>
                    {
                        if (!isSuccess || serverData == null) return;
                        assetsData.UgcInfo = serverData;
                        if (this == null) return;
                        TrySetNameText(serverData.animInfo?.name);
                        var cover = serverData.UgcInfo?.cover;
                        if (!string.IsNullOrEmpty(cover))
                        {
                            remoteIcon.Load(cover, onCompleted: (bool fromCache, bool success) =>
                            {
                                if (this == null) return;
                                iconImg.gameObject.SetActive(false);
                                remoteIcon.gameObject.SetActive(true);
                            });
                        }
                    });
                }
            }
        }
    }

    // 仅在 nameText 为空时写入，避免覆盖已有名称
    private void TrySetNameText(string name)
    {
        if (nameText == null || !string.IsNullOrEmpty(nameText.text)) return;
        if (string.IsNullOrEmpty(name)) return;
        nameText.text = FormatName(name);
    }

    private static string FormatName(string name)
    {
        return name.Length > 6 ? name.Substring(0, 5) + "…" : name;
    }

    public void Unselect()
    {
        selectImgGo.SetActive(false);
    }

    void OnSelectBtnClick()
    {
        if (!string.IsNullOrEmpty(goodsData.Id))
        {
            selectImgGo.SetActive(true);
        }
        onSelectAction?.Invoke(goodsData);
    }
}
