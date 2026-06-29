using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CommunityGamesSearchView : MonoBehaviour
{
   private CButton backButton;
   private BaseSectionInfoPanel _sectionInfoPanel;
   private CButton inputBtn;
   private Text inputText;
   private CButton confirmBtn;
   private CButton clearBtn;
   private string searchHandle = "搜索地图设计码/作者ID/地图名";
   private GameObject _loadingGo;
   private GameObject _emptyGo;

    private Action _action;
    private ActivityCenterBgItem activityBgItem;
    [SerializeField] private Transform _reccommendBg;
    public void Init(bool searchAIGame = false)
    {
        backButton = GameObjectEx.FindChildByName(this.transform, "BackToPanelButton").GetComponent<CButton>();
        _sectionInfoPanel = GameObjectEx.FindChildByName(this.transform, "SearchInfoPanel").GetComponent<BaseSectionInfoPanel>();
        _loadingGo = GameObjectEx.FindChildByName(this.transform, "CommunitySearchEmpty").gameObject;
        _emptyGo = GameObjectEx.FindChildByName(this.transform, "EmptyGo").gameObject;
        inputBtn = GameObjectEx.FindComponentByName<CButton>(transform, "SearchInput");
        confirmBtn = GameObjectEx.FindComponentByName<CButton>(transform, "SearchButton");
        clearBtn = GameObjectEx.FindComponentByName<CButton>(transform, "ClearButton");
        inputText = GameObjectEx.FindComponentByName<Text>(transform, "InputText");
        backButton.onClick.AddListener(Hide);
        inputBtn.onClick.AddListener(OnInputBtnClick);
        confirmBtn.onClick.AddListener(OnConfirmClick);
        clearBtn.onClick.AddListener(OnClearClick);
        InitRecommendBg();
        if (searchAIGame)
        {
            searchHandle = "作者ID/地图名";
        }
    }


    private void InitRecommendBg()
    {
        if (_reccommendBg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_reccommendBg);
        activityBgItem = itemObj.GetComponent<ActivityCenterBgItem>();
        activityBgItem.InitCustomBgItem("#433E3C", atlasPath, new List<string>()
            {
                "S9BgElement1", "S9BgElement2", "S9BgElement3"
            });
        activityBgItem.gameObject.SetActive(true);
        activityBgItem.SetBgImageVisible(false);
        activityBgItem.transform.SetAsFirstSibling();
       
    }

    private void OnInputBtnClick()
   {
      KeyBoardInfo keyBoardInfo = new KeyBoardInfo
      {
         type = 0,
         placeHolder = LocalizationManager.Inst.GetLocalizedText(searchHandle),
         inputMode = (int)KeyBoardInputMode.All,
         maxLength = 30,
         inputFlag = 0,
         lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
         defaultText = "",
         returnKeyType = (int)ReturnType.Done,
         textSecurity = 1
      };
      MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
      MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
   }
   private void OnKeyboard(string input)
   {
      if (string.IsNullOrEmpty(input))
      {
         SetInputDef();
         return;
      }
      inputText.color =Color.black;
      inputText.SetText(input);
      OnConfirmClick();
      clearBtn.gameObject.SetActive(true);
   }
   private void SetInputDef()
   {
      inputText.color = DataUtil.DeSerializeColor("9E9E9E");
      inputText.SetLocalText(searchHandle);
      clearBtn.gameObject.SetActive(false);
   }
   public void OnConfirmClick()
   {
      if (string.IsNullOrEmpty(inputText.text)||(inputText.text == searchHandle&&inputText.color == DataUtil.DeSerializeColor("9E9E9E")))
      {
         TipPanel.ShowToast("请输入搜索内容");
         return;
      }
      _loadingGo.SetActive(true);
      _emptyGo.SetActive(false);
      _sectionInfoPanel.ResetAdpater();
      _sectionInfoPanel.OnSelectSection(inputText.text, 0,OnGetDatas,HasDatas);
   }
   public void OnClearClick()
   {
      SetInputDef();
      _emptyGo.SetActive(false);
      _sectionInfoPanel.ResetAdpater();
   }
   private void OnGetDatas()
   {
      _loadingGo.SetActive(false);
   }
   private void HasDatas(bool isHas)
   {
     _emptyGo.SetActive(!isHas);
   }
   public void Show()
   {
      SetInputDef();
      _loadingGo.SetActive(false);
      _emptyGo.SetActive(false);
      gameObject.SetActive(true);
      _sectionInfoPanel.ResetAdpater();
      activityBgItem?.SetRoration(new Vec3(0, 0, -15));
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        _sectionInfoPanel.ResetAdpater();
        _action?.Invoke();
    }

    public void RegisterAction(Action action)
    {
        if (_action != null || action == null) return;
        _action = action;
    }


    public void UnRegisterAction()
    {
        _action = null;
    }

    public void OnDestroy()
    {
        UnRegisterAction();
    }
}
