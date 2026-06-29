using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


public class ActivityRuleData {
    public string panelTitle;
    public List<string> rules;
}

public class ActivityRulePanel : BasePanel<ActivityRulePanel> {
    [SerializeField] private Text panelTitle;
    [SerializeField] private CButton backBtn;
    [SerializeField] private Button blankBtn;
    [SerializeField] private GameObject ruleItemObj;

    public override void OnCreate() {
        base.OnCreate();
        backBtn.onClick.AddListener(OnBackBtnClick);
        blankBtn.onClick.AddListener(OnBackBtnClick);
    }

    public override void OnShow(params object[] args) {
        base.OnShow(args);
        if (args != null && args.Length > 0) {
            string path = (string)args[0];
            LoadFromPath(path);
        }
        ruleItemObj.gameObject.SetActive(false);
    }

    public void LoadFromPath(string path) {
        var textAsset = Loader.Load<TextAsset>(path, gameObject);
        LoadFromJson(textAsset.text);
    }

    public void LoadFromJson(string jsonStr) {
        if (string.IsNullOrEmpty(jsonStr)) return;
        var ruleData = JsonConvert.DeserializeObject<ActivityRuleData>(jsonStr);
        if (ruleData != null && ruleData.rules != null) {
            for (int i = 0; i < ruleData.rules.Count; i++) {
                AddRule(i + 1, ruleData.rules[i]);
            }
        }

        if (ruleData != null && !string.IsNullOrEmpty(ruleData.panelTitle)) {
            SetPanelTitle(ruleData.panelTitle);
        }
    }

    public void SetPanelTitle(string title) {
        panelTitle.SetLocalText(title);
    }


    private void OnBackBtnClick() {
        CloseSelf();
    }


    public void AddRule(int index, string content) {
        var obj = Instantiate(ruleItemObj, ruleItemObj.transform.parent);
        var ruleText = obj.GetComponent<SuperTextMesh>();
        ruleText.SetLocalText(content);

        var indexText = GameObjectEx.FindComponentByName<Text>(obj, "Index");
        indexText.SetText($"{index}.");
        obj.gameObject.SetActive(true);

    }
}
