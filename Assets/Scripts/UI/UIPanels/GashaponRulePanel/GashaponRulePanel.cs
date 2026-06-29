using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class RuleInfo
{
   public string ruleType ;
   public string content;
}

public class RuleData
{
   public string panelTitle;
   public List<RuleInfo> rules;
}

public class GashaponRulePanel : BasePanel<GashaponRulePanel>
{
   [SerializeField] private Text panelTitle;
   [SerializeField] private Transform contentRoot;
   [SerializeField] private Text titlePrefab;
   [SerializeField] private SuperTextMesh descPrefab;
   [SerializeField] private CButton backBtn;
   [SerializeField] private Button blankBtn;
   public override void OnCreate()
   {
      base.OnCreate();
      backBtn.onClick.AddListener(OnBackBtnClick);
      blankBtn.onClick.AddListener(OnBackBtnClick);
   }

   public override void OnShow(params object[] args)
   {
      base.OnShow(args);
      if (args != null && args.Length > 0)
      {
         string path = (string)args[0];
         LoadFromPath(path);
      }
   }

   public void LoadFromPath(string path)
   {
      var textAsset = Loader.Load<TextAsset>(path, gameObject);
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
            if (ruleInfo.ruleType == "Title")
            {
               AddTitle(ruleInfo.content);
            }
            else if (ruleInfo.ruleType == "Desc")
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

   // 创建标题
   public void AddTitle(string title)
   {
      Text titleItem = Instantiate(titlePrefab, contentRoot);
      titleItem.SetLocalText(title);
   }

   public void AddDesc(string desc)
   {
      SuperTextMesh descItem = Instantiate(descPrefab, contentRoot);
      descItem.SetLocalText(desc);
   }

   public void AddDescList(List<string> descs)
   {
      // 创建正文
      foreach (string desc in descs)
      {
         AddDesc(desc);
      }
   }

   private void OnBackBtnClick()
   {
      CloseSelf();
   }

}
