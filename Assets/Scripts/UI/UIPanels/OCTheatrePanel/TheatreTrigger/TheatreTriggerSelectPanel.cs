using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UI.Base;
using UI.BaseWidgets;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class TheatreTriggerSelectResult
{
    public int TriggerType;
    public string TheatreId;
    public string TheatreName;
    public string TheatreCover;
    public string ActorId;
    public string ActorName;
    public int ClothesIndex;
    public string ClothesName;
}

public class TheatreTriggerSelectPanel : BasePanel<TheatreTriggerSelectPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private Text titleText;
    [SerializeField] private CButton backBtn;
    [SerializeField] private CButton confirmBtn;

    [SerializeField] private TheatreTriggerTheatreInfo theatreInfo;
    [SerializeField] private TheatreTriggerActorInfo actorInfo;

    [SerializeField] private GameObject clothsListRoot;
    [SerializeField] private TheatreTriggerClothsAdapter clothsAdapter;

    private int _triggerType;
    private int _step;
    private DraftListItem _selectedTheatre;
    private DraftListItem _selectedActor;
    private OTCAvatarClothes _selectedClothes;
    private Action<TheatreTriggerSelectResult> _onConfirm;

    public override void OnCreate()
    {
        InitBG();
        backBtn.onClick.AddListener(OnBackClick);
        confirmBtn.onClick.AddListener(OnConfirmClick);

        theatreInfo.SetOnSelectAct(OnTheatreSelected);
        actorInfo.SetOnSelectAct(OnActorSelected);

        clothsAdapter.Data = new SimpleDataHelper<OTCAvatarClothes>(clothsAdapter);
        clothsAdapter.Init();
        clothsAdapter.OnSelectItemAct = OnClothesSelected;
    }

    private void InitBG()
    {
        if (_trans_Bg == null) return;
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_trans_Bg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "theatre_icon1", "theatre_icon2"
        });
        item.gameObject.SetActive(true);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _triggerType = args.Length > 0 && args[0] is int t ? t : 0;
        _step = 0;
        _selectedTheatre = null;
        _selectedActor = null;
        _selectedClothes = null;
        ShowStep();
    }

    public void SetOnConfirm(Action<TheatreTriggerSelectResult> onConfirm)
    {
        _onConfirm = onConfirm;
    }

    private void ShowStep()
    {
        theatreInfo.gameObject.SetActive(false);
        actorInfo.gameObject.SetActive(false);
        clothsListRoot.SetActive(false);

        if (_triggerType == 1)
        {
            switch (_step)
            {
                case 0:
                    titleText.text = "选择演员";
                    actorInfo.gameObject.SetActive(true);
                    actorInfo.SetSelectedId(_selectedActor?.actorInfo?.id);
                    actorInfo.GetData();
                    confirmBtn.interactable = _selectedActor?.actorInfo != null;
                    break;
                case 1:
                    titleText.text = "选择衣服";
                    clothsListRoot.SetActive(true);
                    LoadClothes();
                    confirmBtn.interactable = _selectedClothes != null;
                    break;
                case 2:
                    titleText.text = "选择剧场";
                    theatreInfo.gameObject.SetActive(true);
                    theatreInfo.SetSelectedId(_selectedTheatre?.theatreInfo?.id);
                    theatreInfo.GetData();
                    confirmBtn.interactable = _selectedTheatre?.theatreInfo != null;
                    break;
            }
        }
        else
        {
            titleText.text = "选择剧场";
            theatreInfo.gameObject.SetActive(true);
            theatreInfo.SetSelectedId(_selectedTheatre?.theatreInfo?.id);
            theatreInfo.GetData();
            confirmBtn.interactable = _selectedTheatre?.theatreInfo != null;
        }
    }

    private void LoadClothes()
    {
        var clothes = _selectedActor?.actorInfo?.avatarClothes ?? new List<OTCAvatarClothes>();
        clothsAdapter.SelectedIndex = _selectedClothes?.clothesIndex ?? -1;
        clothsAdapter.Data.ResetItems(clothes);
        clothsAdapter.Refresh(false);
    }

    private void OnTheatreSelected(DraftListItem item)
    {
        _selectedTheatre = item;
        theatreInfo.SetSelectedId(item?.theatreInfo?.id);
        confirmBtn.interactable = item?.theatreInfo != null;
    }

    private void OnActorSelected(DraftListItem item)
    {
        _selectedActor = item;
        actorInfo.SetSelectedId(item?.actorInfo?.id);
        confirmBtn.interactable = item?.actorInfo != null;
    }

    private void OnClothesSelected(OTCAvatarClothes item)
    {
        _selectedClothes = item;
        clothsAdapter.SelectedIndex = item?.clothesIndex ?? -1;
        clothsAdapter.Refresh(false);
        confirmBtn.interactable = item != null;
    }

    private void OnConfirmClick()
    {
        if (_triggerType == 1 && _step < 2)
        {
            _step++;
            ShowStep();
            return;
        }
        Complete();
    }

    private void Complete()
    {
        var result = new TheatreTriggerSelectResult
        {
            TriggerType = _triggerType,
            TheatreId = _selectedTheatre?.theatreInfo?.id ?? "",
            TheatreName = _selectedTheatre?.theatreInfo?.name ?? "",
            TheatreCover = _selectedTheatre?.theatreInfo?.cover ?? "",
            ActorId = _selectedActor?.actorInfo?.id ?? "",
            ActorName = _selectedActor?.actorInfo?.name ?? "",
            ClothesIndex = _selectedClothes?.clothesIndex ?? 0,
            ClothesName = _selectedClothes?.clothesName ?? "",
        };
        CloseSelf();
        _onConfirm?.Invoke(result);
    }

    private void OnBackClick()
    {
        if (_triggerType == 1 && _step > 0)
        {
            _step--;
            ShowStep();
        }
        else
        {
            CloseSelf();
        }
    }

    public override void OnHidden() { }
    public override void OnWindowBeCovered(bool isCover) { }
    public override void OnWindowBeFocused() { }
    public override void OnWindowShow() { }
    public override void OnWindowPop() { }
}
