using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CreatorScoreRulePanel : BasePanel<CreatorScoreRulePanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private List<Toggle> TypeToggles;
    [SerializeField] private List<GameObject> TypeObjs;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        for(int i = 0; i < TypeToggles.Count; i++)
        {
            int index = i;
            TypeToggles[i].onValueChanged.AddListener((isOn) => OnTypeToggleValueChanged(isOn, index));
        }
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        SelectDefaultType();
    }

    private void SelectDefaultType()
    {
        if (TypeToggles == null || TypeToggles.Count <= 0) return;
        if (TypeObjs == null || TypeObjs.Count <= 0) return;

        // 每次打开界面默认选中第一个
        for (int i = 0; i < TypeToggles.Count; i++)
        {
            if (TypeToggles[i] == null) continue;
            TypeToggles[i].SetIsOnWithoutNotify(i == 0);
        }

        OnTypeToggleValueChanged(true, 0);
    }

    private void OnTypeToggleValueChanged(bool isOn, int index)
    {
        for(int i = 0; i < TypeObjs.Count; i++)
        {
            TypeObjs[i].SetActive(false);
        }
        if(isOn && index >= 0 && index < TypeObjs.Count)
        {
            TypeObjs[index].SetActive(true);
        }
    }
}
