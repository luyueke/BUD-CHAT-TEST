using Basic.Extensions;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Utils;
using UI.BaseWidgets;
using UI.UIPanels.GameEdit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class TrapBoxSubView : BasePropertyEditSubView
{
    [SerializeField] private CButton reduceHpBtn;

    [Header("重置玩家位置选项")]
    [SerializeField]private CButton backToSpawnBtn; 
    [SerializeField]private ToggleGroup resetPositionGroup; 
    [SerializeField]private Toggle checkPointToggle; // 返回存档点
    [SerializeField]private CButton checkPointTipBtn; // 返回存档点提示按钮
    [SerializeField]private Toggle initialSpawnToggle; // 返回初始出生点
    [SerializeField]private CButton initialSpawnTipBtn; // 返回初始出生点提示按钮
    [SerializeField]private Toggle customSpawnToggle; // 返回自定义出生点
    [SerializeField]private CButton customSpawnTipBtn; // 返回自定义出生点提示按钮

    [SerializeField] private Toggle textToggle;
    [SerializeField] private Text customText;
    [SerializeField] private Text numText;
    [SerializeField] private Image textLine;
    
    private EventTrigger trigger;
    private const string DefaultInputStr = "哎呀！你触发了一个陷阱！";
    private string defaultText = DefaultInputStr;
    private int maxNum = 60;
    private Color enColor = Color.white;
    private Color disColor = new Color(1, 1, 1, 0.38f);

    private bool backToSpawnState = false;
    private bool reduceHpState = false;
    protected override void OnInit()
    {
        defaultText = LocalizationManager.Inst.GetLocalizedText(DefaultInputStr);
        backToSpawnBtn.onClick.AddListener(OnBackToSpawnClick);
        initialSpawnTipBtn.onClick.AddListener(OnNoOtherCheckPointTipClick);
        customSpawnTipBtn.onClick.AddListener(OnNoOtherCheckPointTipClick);
        checkPointToggle.onValueChanged.AddListener(OnCheckPointToggleChange);
        initialSpawnToggle.onValueChanged.AddListener(OnInitialSpawnToggleChange);
        customSpawnToggle.onValueChanged.AddListener(OnCustomSpawnToggleChange);
        textToggle.onValueChanged.AddListener(OnTextSelect);
        reduceHpBtn.onClick.AddListener(OnReduceHpClick);

        trigger = GetComponentInChildren<EventTrigger>();
        EventTrigger.Entry onSelect = new EventTrigger.Entry();
        onSelect.eventID = EventTriggerType.PointerClick;
        onSelect.callback.AddListener(SelectText);
        trigger.triggers.Add(onSelect);

        checkPointTipBtn.gameObject.SetActive(false);
        initialSpawnTipBtn.gameObject.SetActive(false);
        customSpawnTipBtn.gameObject.SetActive(false);
    }


    public override void OnSelectEntity(SceneEntity entity)
    {
        var tComp = entity.GetComp<TrapBoxComponent>();
        //LoggerUtils.Log("SetEntity getBackSpawnBtnState  tComp.rePos = " +  tComp.rePos);

        var isResetPlayer = tComp.TransType != (int)GameGlobalEnum.TrapBoxTrans.NoTrans;
        SetBackSpawnBtnState(isResetPlayer);
        if (isResetPlayer)
        {
            checkPointToggle.SetIsOnWithoutNotify(tComp.TransType == (int)GameGlobalEnum.TrapBoxTrans.CheckPoint);
            initialSpawnToggle.SetIsOnWithoutNotify(tComp.TransType == (int)GameGlobalEnum.TrapBoxTrans.MapSpawn);
            customSpawnToggle.SetIsOnWithoutNotify(tComp.TransType == (int)GameGlobalEnum.TrapBoxTrans.CustomSpawn);
        }
        resetPositionGroup.gameObject.SetActive(isResetPlayer);
        textToggle.isOn = tComp.HasTips == 1;
        SetReduceHpBtnState(tComp.HitState == 1);
        SetTextEnable(tComp.HasTips == 1);
        SetContent(tComp.TipsStr);

        //TODO:需要跟存档点联调
        // // https://pointone.feishu.cn/docx/EWDjd08uVod5uuxnUYYcx2wDnCb#part-HWujdmClUoD9D3x81IWc3oEInXa
        // // -需求： 需要检查当前场景中是否还存在return to check point的陷阱盒，如果不存在则不允许进行改操作，并弹toast
        // if (tComp.TransType == (int)TrapBoxTrans.CheckPoint && ArchivePointManager.Inst.HavePoint())
        // {
        //     var noOtherCheckPointOption = GlobalNodeManager.Inst.Get<TrapBoxManager>().GetHaveCheckPintOptionCount() == 1;
        //     initialSpawnTipBtn.gameObject.SetActive(noOtherCheckPointOption);
        //     customSpawnTipBtn.gameObject.SetActive(noOtherCheckPointOption);
        //     initialSpawnToggle.interactable = !noOtherCheckPointOption;
        //     customSpawnToggle.interactable = !noOtherCheckPointOption;
        // }
    }
    
     public void SetContent(string content)
    {
        var tComp = selectEntity.GetComp<TrapBoxComponent>();
        if (tComp.HasTips == 1 && !string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(content.Trim()))
        {
            string str = content.TrimStart().TrimEnd();
            SetTextContent(str, str.Length, enColor);
        }
        else
        {
            SetTextContent(defaultText, 0, disColor);
        }
    }

    private void SetTextContent(string content, int conNum, Color textColor)
    {
        customText.SetText(content);
        numText.text = string.Format("{0}/{1}", Mathf.Min(conNum, maxNum), maxNum);

        customText.color = textColor;
        numText.color = textColor;
    }

    private void OnCheckPointToggleChange(bool isOn)
    {
        if (isOn)
        {
            OnResetPlayerPositionSelect(GameGlobalEnum.TrapBoxTrans.CheckPoint);
        }
    }

    private void OnCustomSpawnToggleChange(bool isOn)
    {
        if (isOn)
        {
            OnResetPlayerPositionSelect(GameGlobalEnum.TrapBoxTrans.CustomSpawn);
        }
    }

    private void OnInitialSpawnToggleChange(bool isOn)
    {
        if (isOn)
        {
            OnResetPlayerPositionSelect(GameGlobalEnum.TrapBoxTrans.MapSpawn);
        }
    }

    private void OnBackToSpawnClick()
    {
        var isClickNew = !backToSpawnState;
        if (!isClickNew && !reduceHpState)
        {
            ShowLimitSelectToast();
            return;
        }
        resetPositionGroup.gameObject.SetActive(isClickNew);
        SetBackSpawnBtnState(isClickNew);
        var tComp = selectEntity.GetComp<TrapBoxComponent>();
        if (!isClickNew)
        {
            tComp.TransType = (int)GameGlobalEnum.TrapBoxTrans.NoTrans;
        } else
        {
            initialSpawnToggle.SetIsOnWithoutNotify(true);
            tComp.TransType = (int)GameGlobalEnum.TrapBoxTrans.MapSpawn;
        }
    }

    void OnNoOtherCheckPointTipClick()
    {
        string tipsText = "您无法移除这个陷阱盒，因为您在游戏场景中还有检查点。";
        TipPanel.ShowToast(tipsText);
    }

    void OnCheckPointTipClick()
    {
        string tipsText = "您需要先添加一个检查点";
        TipPanel.ShowToast(tipsText);
    }

    private void OnReduceHpClick()
    {
        var tComp = selectEntity.GetComp<TrapBoxComponent>();
        if (tComp.HitState == 0)
        {
            SetReduceHpBtnState(true);
            tComp.HitState = 1;
        }
        else
        {
            if(backToSpawnState == false)
            {
                ShowLimitSelectToast();
                return;
            }
            SetReduceHpBtnState(false);
            tComp.HitState = 0;
        }
    }

    private void OnResetPlayerPositionSelect(GameGlobalEnum.TrapBoxTrans optionType)
    {
        var tComp = selectEntity.GetComp<TrapBoxComponent>();
        var optionInt = (int)optionType;
        if (tComp.TransType == optionInt) return;
        
        //LoggerUtils.Log($"OnResetPlayerPositionSelect tComp.rePos = {optionType}");
        var oldRePos = tComp.TransType;
        tComp.TransType = optionInt;
        if (oldRePos == (int)GameGlobalEnum.TrapBoxTrans.CustomSpawn) // 上一次选择的是自定义，就删除自定义出生点
        {
            HandleCustomSpawnPoint(false);
        }
        if (optionType == GameGlobalEnum.TrapBoxTrans.CustomSpawn) // 本次选择的是自定义，就创建自定义出生点
        {
            HandleCustomSpawnPoint(true);
        }
        
        //TODO:需要跟存档点联调
        // if (optionType == TrapBoxTrans.CheckPoint && !ArchivePointManager.Inst.HavePoint())
        if (optionType == GameGlobalEnum.TrapBoxTrans.CheckPoint)
        {
            OnCheckPointTipClick();
        }
        
    }

    private void HandleCustomSpawnPoint(bool isCreate)
    {
        var gComp = selectEntity.GetComp<GameObjectComponent>();
        var tBehav = gComp.BindGo.GetComponent<TrapBoxBehaviour>();
        if (isCreate)
        {
            GlobalNodeManager.Inst.Get<TrapBoxManager>().CreateTrapSpawn(tBehav);
        }
        else
        {
            GlobalNodeManager.Inst.Get<TrapBoxManager>().DestroyTrapSpawn(tBehav);
            // var tComp = selectEntity.GetComp<TrapBoxComponent>();
            // var spawnBehav = GlobalNodeManager.Inst.Get<TrapSpawnManager>().GetPointGo(tComp.PointId);
            // if (spawnBehav != null)
            // {
            //     var pointTarget = spawnBehav.entity.GetComp<GameObjectComponent>().BindGo;
            //     GlobalNodeManager.Inst.Get<TrapSpawnManager>().RemoveTrapSpawn(tComp.PointId);
            //     // GamePropNodeManager.Inst.DestroyNodeToSecondCache(pointTarget);
            //     GamePropNodeManager.Inst.DestroyNode(pointTarget);
            // }
        }
        tBehav.RefreshShowId();
    }

    private void OnTextSelect(bool isOn)
    {
        int state = isOn ? 1 : 0;
        var tComp = selectEntity.GetComp<TrapBoxComponent>();
        if (tComp.HasTips == state)
        {
            return;
        }
        tComp.HasTips = state;
        SetTextEnable(isOn);
        
        if (!isOn)
        {
            tComp.TipsStr = "";
            SetContent(tComp.TipsStr);
        }
    }

   
    private void SetTextEnable(bool state)
    {
        trigger.enabled = state;
        textLine.color = state ? enColor : disColor;
    }

    private void SelectText(BaseEventData data)
    {
        var tComp = selectEntity.GetComp<TrapBoxComponent>();
        string str = string.IsNullOrEmpty(tComp.TipsStr) ? "" : tComp.TipsStr.TrimStart().TrimEnd();
        string limitStr = LocalizationManager.Inst.GetLocalizedText("超出字符限制");
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = maxNum,
            inputFlag = 0,
            lengthTips = limitStr,
            defaultText = str,
            textSecurity = 0,
            returnKeyType = (int)ReturnType.Done
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, ShowKeyBoard);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    //必须至少勾选一个选项
    private void ShowLimitSelectToast()
    {
        string tipsText = "必须至少勾选一个选项";
        TipPanel.ShowToast(tipsText);
    }

    public void ShowKeyBoard(string str)
    {
        var tComp = selectEntity.GetComp<TrapBoxComponent>();
        string content = string.IsNullOrEmpty(str) || string.IsNullOrEmpty(str.Trim()) ? "" : str;
        content = content.TrimStart().TrimEnd();
        tComp.TipsStr = content;
        SetContent(content);
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
    }

    public void SetBackSpawnBtnState(bool isSelected)
    {
        SetBtnState(backToSpawnBtn,isSelected);
        backToSpawnState = isSelected;
    }

    public void SetReduceHpBtnState(bool isSelected)
    {
        SetBtnState(reduceHpBtn,isSelected);
        reduceHpState = isSelected;
    }

    public void SetBtnState(Button btn,bool isSelected)
    {
        Transform checkImg = GameObjectEx.FindChildByName(btn.gameObject,"Checkmark");
        if(checkImg)
        {
            checkImg.gameObject.SetActive(isSelected);
        }
    }
}
