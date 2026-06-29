using UI.Base;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class CommonSetNamePanelData
{
    public string title = "修改文本";
    public string inputTxt = "";
    public int maxLength = 16;
    public UnityAction<string> btn_close_action;
    public UnityAction<string> btn_ok_action;
}

public class CommonSetNamePanel : BasePanel<CommonSetNamePanel>
{
    public Button btn_input_name;

    public Button btn_close;
    public Button btn_add;

    private Text InputText;
    public Text txt_title;

    private string cuttent_input_txt;
    private string searchHandle = "";
    private CommonSetNamePanelData m_data;
    public override void OnCreate()
    {
        base.OnCreate();
        InputText = btn_input_name.transform.Find("InputText").gameObject.GetComponent<Text>();
        btn_input_name.onClick.AddListener(OnInputNameBtnClick);
        // btn_close.onClick.AddListener((() => gameObject.SetActive(false)));
        // btn_add.onClick.AddListener((() =>
        // {
        //     gameObject.SetActive(false);
        //     MessageHelper.Broadcast<string>(MessageName.SetImportantPeople, InputText.text);//通知添加重要的人
        // }));
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        m_data = args[0] as CommonSetNamePanelData;
        txt_title.text = m_data.title;
        InputText.text = m_data.inputTxt;
        btn_close.onClick.AddListener(() =>
        {
            m_data.btn_close_action(cuttent_input_txt);
        });
        btn_add.onClick.AddListener( (() =>
        {
            if (string.IsNullOrEmpty(cuttent_input_txt))
            {
                TipPanel.ShowToast("请输入名字！");
                return;
            }

            m_data.btn_ok_action.Invoke(cuttent_input_txt);
        }));
    }

    private void OnInputNameBtnClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText(searchHandle),
            inputMode = (int)KeyBoardInputMode.All,
            maxLength = m_data.maxLength,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
            //defaultText = isFittingRoom ? (inputText.text == searchHandle ? "" : inputText.text) : "",
            returnKeyType = (int)ReturnType.Search,
            textSecurity = 1
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard,(str =>
        {
            InputText.text = str;
            cuttent_input_txt = str;
        }));
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }
}
