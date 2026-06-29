using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class HalloweenLimitPackRulePanel : BasePanel<HalloweenLimitPackRulePanel>
{
    [SerializeField] private Text panelTitle;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private CButton backBtn;
    [SerializeField] private Button blankBtn;
    [SerializeField] private SuperTextMesh descPrefab;
    public override void OnCreate()
    {
        base.OnCreate();
        backBtn.onClick.AddListener(OnBackBtnClick);
        blankBtn.onClick.AddListener(OnBackBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var textAsset = Loader.Load<TextAsset>(HalloweenLimitPackMgr.ViewBasePath, gameObject);
        LoadFromJson(textAsset.text);
    }

    public void LoadFromJson(string jsonStr)
    {
        if (string.IsNullOrEmpty(jsonStr)) return;
        var ruleData = JsonConvert.DeserializeObject<RuleData>(jsonStr);
        if (ruleData != null && ruleData.rules != null)
        {
            foreach (var ruleInfo in ruleData.rules)
            {
                if (ruleInfo.ruleType == "Desc")
                {
                    AddDesc(ruleInfo.content);
                }
            }
        }

        if (!string.IsNullOrEmpty(ruleData.panelTitle))
        {
            SetPanelTitle(ruleData.panelTitle);
        }
    }

    public void SetPanelTitle(string title)
    {
        panelTitle.SetLocalText(title);
    }

    public void AddDesc(string desc)
    {
        SuperTextMesh descItem = Instantiate(descPrefab, contentRoot);
        descItem.SetLocalText(desc);
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
    }
}
