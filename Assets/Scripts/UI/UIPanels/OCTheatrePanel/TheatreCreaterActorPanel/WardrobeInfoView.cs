using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using Message;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WardrobeInfoView : MonoBehaviour
{
    private Transform wardrobeInfoItem;
    private Transform itemParent;
    private Transform wardrobeInfo_null;

    // key: clothesIndex, value: 对应的item GameObject
    private readonly Dictionary<string, GameObject> _itemMap = new();

    private void Awake()
    {
        wardrobeInfoItem = GameObjectEx.FindChildByName(transform, "wardrobeInfo_item");
        itemParent = GameObjectEx.FindChildByName(transform, "Content");
        wardrobeInfo_null = GameObjectEx.FindChildByName(transform, "wardrobeInfo_null");
        wardrobeInfoItem.gameObject.SetActive(false);

        MessageHelper.AddListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, SyncItems);
        SyncItems(null);
    }

    private void SyncItems(OCTheatreAvatarInfo actor)
    {
        List<OTCAvatarClothes> clothesList;
        if(actor != null)
            clothesList = actor.avatarClothes;
        else
            clothesList = OCTheatreActorEditorDataManager.Inst.GetClothesList();

        // 销毁所有已有 item（跳过 template）
        foreach (var go in _itemMap.Values)
            DestroyImmediate(go);
        _itemMap.Clear();

        bool isEmpty = clothesList == null || clothesList.Count == 0;
        wardrobeInfo_null.gameObject.SetActive(isEmpty);
        if (isEmpty) return;

        foreach (var clothes in clothesList)
            CreateItem(clothes);
    }

    private void CreateItem(OTCAvatarClothes clothes)
    {
        var go = Instantiate(wardrobeInfoItem, itemParent);
        go.gameObject.SetActive(true);
        go.localPosition = Vector2.zero;

        var nameText = GameObjectEx.FindComponentByName<Text>(go.transform, "name");
        if (nameText != null)
            nameText.text = clothes.clothesName;

        var icon = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(go.transform, "icon");
        var img_add = GameObjectEx.FindChildByName(go.transform, "img_add");
        if(!string.IsNullOrEmpty(clothes.clothesURL))
        {
            icon.ResetRawImage();
            icon.Load(clothes.clothesURL);
            img_add.gameObject.SetActive(false);
            icon.gameObject.SetActive(true);
        }
        else
        {
            icon.ResetRawImage();
            icon.gameObject.SetActive(false);
            img_add.gameObject.SetActive(true);
        }

        var btn = go.GetComponent<Button>() ?? go.gameObject.AddComponent<Button>();
        var capturedClothes = clothes;
        btn.onClick.AddListener(() =>
            MessageHelper.Broadcast<OTCAvatarClothes>(MessageName.ActorCardWardrobeItemClicked, capturedClothes));

        _itemMap[clothes.clothesIndex.ToString()] = go.gameObject;
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, SyncItems);
    }
}
