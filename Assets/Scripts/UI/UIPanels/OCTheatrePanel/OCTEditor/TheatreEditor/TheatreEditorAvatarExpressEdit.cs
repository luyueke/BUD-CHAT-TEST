using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorAvatarExpressEdit : TheatreEditorUIBase<POCTheatreSection>
{
    [SerializeField] private Transform avatarExpressListRoot;
    [SerializeField] private TheatreEditorExpressItemList expressListItemPrefab;
    [SerializeField] private ScrollRect expressListScrollRect;
    [SerializeField] private Button goAvatarBtn;
    [SerializeField] private GameObject zeroAvatarHint;

    private POCTheatreSection sectionData;
    private readonly List<TheatreEditorExpressItemList> expressLists = new();

    public override void OnInit(POCTheatreSection param)
    {
        base.OnInit(param);
        goAvatarBtn?.onClick.RemoveAllListeners();
        goAvatarBtn?.onClick.AddListener(() => Panel?.ShowAvatarEdit());
    }

    public override void OnShow(POCTheatreSection param)
    {
        base.OnShow(param);
        sectionData = param;
        RefreshAll();
    }

    private void RefreshAll()
    {
        ClearLists();
        if (sectionData == null) return;

        var dc = Panel?.DataCenter;
        if (dc == null) return;

        zeroAvatarHint?.SetActive(false);

        string curAvatarId = sectionData.AvatarId;
        string curAvatarType = sectionData.AvatarType;

        // 旁白角色始终排在首位
        if (expressListItemPrefab != null && avatarExpressListRoot != null)
        {
            var narratorObj = Instantiate(expressListItemPrefab.gameObject, avatarExpressListRoot);
            var narratorList = narratorObj.GetComponent<TheatreEditorExpressItemList>();
            if (narratorList != null)
            {
                bool narratorSelected = string.IsNullOrEmpty(curAvatarId)
                    || curAvatarId == TheatreEditorDataCenter.NarratorAvatarId;
                narratorList.InitAsNarrator(narratorSelected, (selectedId, selectedType) =>
                {
                    dc.SetSectionAvatar(sectionData, selectedId, selectedType);
                    Panel?.BackToSectionEdit();
                });
                expressLists.Add(narratorList);
                narratorObj.SetActive(true);
            }
            else Destroy(narratorObj);
        }

        foreach (var avatarId in dc.SelectedAvatarIds)
        {
            if (!dc.AvatarInfoCache.TryGetValue(avatarId, out var avatarInfo)) continue;
            if (expressListItemPrefab == null || avatarExpressListRoot == null) break;

            var obj = Instantiate(expressListItemPrefab.gameObject, avatarExpressListRoot);
            var listItem = obj.GetComponent<TheatreEditorExpressItemList>();
            if (listItem == null) { Destroy(obj); continue; }

            listItem.Init(avatarId, avatarInfo, curAvatarId, curAvatarType, (selectedAvatarId, selectedExpressType) =>
            {
                dc.SetSectionAvatar(sectionData, selectedAvatarId, selectedExpressType);
                Panel?.BackToSectionEdit();
            });

            expressLists.Add(listItem);
            obj.SetActive(true);
        }
    }

    private void ClearLists()
    {
        foreach (var list in expressLists)
            if (list != null) Destroy(list.gameObject);
        expressLists.Clear();
    }

    public override void OnHide() { base.OnHide(); }
}
