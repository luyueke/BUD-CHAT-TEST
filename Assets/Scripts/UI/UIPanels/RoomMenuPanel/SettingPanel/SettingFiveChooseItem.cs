using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingFiveChooseItem : SettingItem
{
    private Text[] chooseTexts;
    private Button[] chooseBtns;
    private Image[] chooseImgs;
    public Action<int> OnChooseChange;
    private static Color COLOR_UNSELECT = new Color(1f, 1f, 1f, 0.3f);
    private static Color COLOR_SELECT = new Color(1f, 1f, 1f, 1);
    public Sprite selectBack;
    public Sprite unselectBack;

    public override void FindViews()
    {
        base.FindViews();
        chooseTexts = new Text[5];
        chooseBtns = new Button[5];
        chooseImgs = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            chooseTexts[i] = transform.Find($"BGButton/Choose{i+1}/Content").GetComponent<Text>();
            chooseImgs[i] = transform.Find($"BGButton/Choose{i+1}/Back").GetComponent<Image>();
            chooseBtns[i] = transform.Find($"BGButton/Choose{i+1}").GetComponent<Button>();
            int index = i;
            chooseBtns[i].onClick.AddListener(() => { OnButtonClick(index); });
        }
    }

    private void OnButtonClick(int i)
    {
        if (GetSelectedIndex() == i)
        {
            return;
        }
        SetSelected(i);
        OnChooseChange?.Invoke(i);
    }

    private int GetSelectedIndex()
    {
        for (int i = 0; i < chooseTexts.Length; i++)
        {
            if (chooseTexts[i].color == COLOR_SELECT)
            {
                return i;
            }
        }
        return -1;
    }

    public override void Init(SettingItemData settingItemData)
    {
        base.Init(settingItemData);
        var atlasPath = "Assets/Loadable/Prefabs/UIPanel/GlobalSettingPanel/GlobalSettingPanel.spriteatlas";
        if (settingItemData is SettingFiveChooseData data)
        {
            for (int i = 0; i < chooseTexts.Length; i++)
            {
                chooseTexts[i].text = data.choose[i];
            }
            SetSelected(data.defaultChoose);
            OnChooseChange = data.OnChooseChange;
        }
    }

    private void SetSelected(int selectIndex)
    {
        for (int i = 0; i < 5; i++)
        {
            chooseImgs[i].sprite = selectIndex == i ? selectBack : unselectBack;
            chooseTexts[i].color = selectIndex == i ? COLOR_SELECT : COLOR_UNSELECT;
        }
    }
}