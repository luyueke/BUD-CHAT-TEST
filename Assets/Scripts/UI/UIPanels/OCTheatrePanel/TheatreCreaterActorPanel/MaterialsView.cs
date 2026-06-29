using GameData.BaseInfo;
using Message;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

public class MaterialsView : MonoBehaviour
{
    string[] character_name ={ "冷静", "冲动", "乐观", "悲观", "温柔", "强势", "内向", "外向", "理性", "感性", "幽默", "严谨", "随性", "固执", "善良", "勇敢" };

    private ScrollRect SV;
    private CButton txt_input_name;
    private Transform character;
    private Toggle tog_character;
    private CButton txt_input_dsc;
    private CButton txt_input_bg;
    private Button btn_add_h;
    private Transform root_ImportantPeople;
    private Text txt_zydr;
    private Transform item_ImportantPeople;

    private Button btn_add_ImportantPeople;
    private int importantPeopleCount = 5; //最大五个总要的人
    private string searchHandle = "请输入名字";
    private Toggle tog_sex0;
    private Toggle tog_sex1;
    private Toggle tog_sex2;
    
    
    private void Awake()
    {
    
    
        SV = GameObjectEx.FindComponentByName<ScrollRect>(transform,"ScrollView");
        txt_input_name = GameObjectEx.FindComponentByName<CButton>(transform, "txt_input_name");
        character = GameObjectEx.FindComponentByName<Transform>(transform, "character");
        character.gameObject.SetActive(false);
        tog_character = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_character");
        txt_input_dsc = GameObjectEx.FindComponentByName<CButton>(transform, "txt_input_dsc");
        txt_input_bg = GameObjectEx.FindComponentByName<CButton>(transform, "txt_input_bg");
        root_ImportantPeople = GameObjectEx.FindComponentByName<Transform>(transform, "root_ImportantPeople");
        btn_add_ImportantPeople = GameObjectEx.FindComponentByName<Button>(transform, "btn_add_ImportantPeople");
        txt_zydr = GameObjectEx.FindComponentByName<Text>(transform, "txt_zydr");
        item_ImportantPeople = GameObjectEx.FindComponentByName<Transform>(transform, "item_ImportantPeople");
        tog_sex0 = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_sex0");
        tog_sex1 = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_sex1");
        tog_sex2 = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_sex2");
        
        
        item_ImportantPeople.gameObject.SetActive(false);
        SV.gameObject.SetActive(false);
        
       
       

        
        tog_sex0.onValueChanged.AddListener((arg0 =>
        {
            if(arg0)
            {
                OCTheatreActorEditorDataManager.Inst.SetGender(0);
            }
        }));
        tog_sex1.onValueChanged.AddListener((arg0 =>
        {
            if(arg0)
            {
                OCTheatreActorEditorDataManager.Inst.SetGender(1);
            }
        }));
        tog_sex2.onValueChanged.AddListener((arg0 =>
        {
            if(arg0)
            {
                OCTheatreActorEditorDataManager.Inst.SetGender(2);
            }
        }));
        txt_input_name.onClick.AddListener((() =>
        {
            var actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("输入名称"),
                inputMode = (int)KeyBoardInputMode.All,
                maxLength = 16,
                inputFlag = 0,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
                defaultText = actor.name ?? "",
                returnKeyType = (int)ReturnType.Done,
                textSecurity = 1
            };

            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str =>
            {
                txt_input_name.transform.Find("InputText").gameObject.GetComponent<Text>().text = str;
                SetInputColor(txt_input_name, str == "");
                UpdateLengthText(txt_input_name, str, 16);
                OCTheatreActorEditorDataManager.Inst.SetActorName(str);
            }));
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));

        }));
        txt_input_bg.onClick.AddListener(OnInputBgBtnClick);
        txt_input_dsc.onClick.AddListener(OnInputDscBtnClick);

        var initActor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        SetInputColor(txt_input_name, string.IsNullOrEmpty(initActor?.name));
        SetInputColor(txt_input_dsc, string.IsNullOrEmpty(initActor?.desc));
        SetInputColor(txt_input_bg, string.IsNullOrEmpty(initActor?.backgroundDes));
        btn_add_ImportantPeople.onClick.AddListener((() =>
        {
            if (root_ImportantPeople.childCount - 2 >= importantPeopleCount)
            {
                TipPanel.ShowToast($"最多可添加6个重要的人！");
                return;
            }

            UIManager.Inst.OpenPanel(PanelId.CommonSetNamePanel, new CommonSetNamePanelData()
            {
                title = "添加重要的人",
                inputTxt = "",
                btn_close_action = ((_) => { UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel); }),
                btn_ok_action = ((str) =>
                {
                    UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel);
                    OCTheatreActorEditorDataManager.Inst.AddImportantPersons(str, () =>
                    {
                        TipPanel.ShowToast("已存在相同的人！");
                    });
                })
            });

        }));
        tog_character.gameObject.SetActive(false);
        for (int i = 0; i < character_name.Length; i++)
        {
            var go1 = Instantiate(tog_character, character);
            go1.gameObject.SetActive(true);
            string c_name = character_name[i];
            go1.transform.Find("Label").gameObject.GetComponent<Text>().text = c_name;

            go1.GetComponent<Toggle>().onValueChanged.AddListener(((bool isOn) =>
            {
                if (isOn)
                {
                    OCTheatreActorEditorDataManager.Inst.AddPersonalities(c_name);
                }
                else
                {
                    OCTheatreActorEditorDataManager.Inst.RemovePersonalities(c_name);
                }
            }));
        }

        SV.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(SV.content);
        SV.content.anchoredPosition = new Vector2(SV.content.anchoredPosition.x, -755);

        MessageHelper.AddListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, RefreshData);
        RefreshData(null);
    }

    private void SetInputColor(CButton input, bool isEmpty)
    {
        var txt = input.transform.Find("InputText").GetComponent<Text>();
        ColorUtility.TryParseHtmlString(isEmpty ? "#807B7B" : "#3A3A3A", out var color);
        txt.color = color;
    }

    private static void UpdateLengthText(CButton input, string value, int maxLen)
    {
        var tf = input.transform.Find("InputTextInfo");
        if (tf == null) return;
        if (!tf.TryGetComponent<Text>(out var lenTxt)) return;
        lenTxt.text = $"{(value != null ? value.Length : 0)}/{maxLen}";
    }

    private void RefreshData(OCTheatreAvatarInfo acotr)
    {
        var actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();

        // 名字
        bool nameEmpty = string.IsNullOrEmpty(actor.name);
        txt_input_name.transform.Find("InputText").GetComponent<Text>().text = nameEmpty ? "请输入昵称" : actor.name;
        SetInputColor(txt_input_name, nameEmpty);
        UpdateLengthText(txt_input_name, actor.name, 16);

        // 性别
        tog_sex0.SetIsOnWithoutNotify(actor.gender == 0);
        tog_sex1.SetIsOnWithoutNotify(actor.gender == 1);
        tog_sex2.SetIsOnWithoutNotify(actor.gender == 2);

        // 角色描述
        bool descEmpty = string.IsNullOrEmpty(actor.desc);
        txt_input_dsc.transform.Find("InputText").GetComponent<Text>().text = descEmpty ? "请输入描述" : actor.desc;
        SetInputColor(txt_input_dsc, descEmpty);
        UpdateLengthText(txt_input_dsc, actor.desc, 100);

        // 背景描述
        bool bgEmpty = string.IsNullOrEmpty(actor.backgroundDes);
        txt_input_bg.transform.Find("InputText").GetComponent<Text>().text = bgEmpty ? "请输入背景" : actor.backgroundDes;
        SetInputColor(txt_input_bg, bgEmpty);
        UpdateLengthText(txt_input_bg, actor.backgroundDes, 50);
        // 重要的人 — 清空后重建
        foreach (Transform child in root_ImportantPeople)
        {
            if (child != item_ImportantPeople && child != btn_add_ImportantPeople.transform)
                Destroy(child.gameObject);
        }
        if (actor.importantPersons != null)
        {
            foreach (var person in actor.importantPersons)
                OnInputNameBtnClick(person);
        }

        // 性格标签
        var personalities = actor.personalities;
        foreach (Transform child in character)
        {
            var tog = child.GetComponent<Toggle>();
            if (tog == null) continue;
            string label = child.Find("Label").GetComponent<Text>().text;
            tog.SetIsOnWithoutNotify(personalities != null && personalities.Contains(label));
        }

         LayoutRebuilder.ForceRebuildLayoutImmediate(SV.content);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, RefreshData);
    }

    

    private void OnInputNameBtnClick(string str)
    {
        var go = Instantiate(item_ImportantPeople, root_ImportantPeople);
        go.gameObject.SetActive(true);   
        go.transform.Find("Label").GetComponent<Text>().text = str;
        CButton cbtn_del = GameObjectEx.FindComponentByName<CButton>(go.transform,"cbtn_del");
        cbtn_del.onClick.AddListener(() =>
        {
            OCTheatreActorEditorDataManager.Inst.RemoveImportantPersons(str);
            Destroy(go.gameObject);
        });
    }
    private void OnInputBgBtnClick()
    {
        var actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("输入背景"),
            inputMode = (int)KeyBoardInputMode.All,
            maxLength = 50,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
            defaultText = actor.backgroundDes ?? "",
            returnKeyType = (int)ReturnType.Done,
            textSecurity = 1
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str =>
        {
            txt_input_bg.transform.Find("InputText").gameObject.GetComponent<Text>().text = str;
            SetInputColor(txt_input_bg, str == "");
            UpdateLengthText(txt_input_bg, str, 50);
            OCTheatreActorEditorDataManager.Inst.SetBackgroundDes(str);
        }));
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    private void OnInputDscBtnClick()
    {
        var actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("输入介绍"),
            inputMode = (int)KeyBoardInputMode.All,
            maxLength = 100,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
            defaultText = actor.desc ?? "",
            returnKeyType = (int)ReturnType.Done,
            textSecurity = 1
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str =>
        {
            txt_input_dsc.transform.Find("InputText").gameObject.GetComponent<Text>().text = str;
            SetInputColor(txt_input_dsc, str == "");
            UpdateLengthText(txt_input_dsc, str, 100);
            OCTheatreActorEditorDataManager.Inst.SetDesc(str);
        }));
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }
}
