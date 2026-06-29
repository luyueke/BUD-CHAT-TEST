using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingTwoChooseItem : SettingItem
{
    private Text choose1Text;
    private Text choose2Text;
    private Button choose1Btn;
    private Button choose2Btn;
    private Image choose1Img;
    private Image choose2Img;
    public Action<int> OnChooseChange;
    public Func<bool> OnEnableRefreshView;
    private static Color COLOR_UNSELECT = new Color(1f, 1f, 1f, 0.3f);
    private static Color COLOR_SELECT = new Color(1f, 1f, 1f, 1);
    private SettingTwoChooseItemData data;

    public Sprite selectSprite;
    public Sprite unSelectSprite;

    private void OnEnable()
    {
        if (OnEnableRefreshView != null)
        {
            SetSelected(OnEnableRefreshView.Invoke() ? 0 : 1);
        }
    }

    public override void FindViews()
    {
        base.FindViews();
        choose1Text = transform.Find("BGButton/Choose1/Content").GetComponent<Text>();
        choose1Img = transform.Find("BGButton/Choose1/Back").GetComponent<Image>();
        choose1Btn = transform.Find("BGButton/Choose1").GetComponent<Button>();
        choose2Text = transform.Find("BGButton/Choose2/Content").GetComponent<Text>();
        choose2Img = transform.Find("BGButton/Choose2/Back").GetComponent<Image>();
        choose2Btn = transform.Find("BGButton/Choose2").GetComponent<Button>();
        choose1Btn.onClick.AddListener(() => { OnButtonClick(0); });
        choose2Btn.onClick.AddListener(() => { OnButtonClick(1); });
    }

    private void OnButtonClick(int i)
    {
        if (GetSelectedIndex() == i)
        {
            return;
        }
        //拦截当前想要选择的选项，有可能现在的逻辑不允许选择当前选项
        if (data.intercept != null)
        {
            SettingTwoItemInterceptData interceptData = new SettingTwoItemInterceptData()
            {
                src = GetSelectedIndex(),
                toSet = i
            };
            if (data.intercept(interceptData))
            {
                return;
            }
        }
        SetSelected(i);
        OnChooseChange?.Invoke(i);
    }

    private int GetSelectedIndex()
    {
        if (choose1Text.color == COLOR_SELECT)
        {
            return 0;
        }

        return 1;
    }

    public override void Init(SettingItemData settingItemData)
    {
        base.Init(settingItemData);
        if (settingItemData is SettingTwoChooseItemData data)
        {
            choose1Text.SetLocalText(data.firstChoose);
            choose2Text.SetLocalText(data.secondChoose);
            SetSelected(data.defaultChoose);
            OnChooseChange = data.OnChooseChange;
            OnEnableRefreshView = data.OnEnableRefreshView;
            this.data = data;
        }
    }

    public void SetSelected(int selectIndex)
    {
        if (selectIndex == 0)
        {
            choose1Img.sprite = selectSprite;
            choose2Img.sprite = unSelectSprite;
            choose1Text.color = COLOR_SELECT;
            choose2Text.color = COLOR_UNSELECT;
        }
        else if (selectIndex == 1)
        {
            choose1Img.sprite = unSelectSprite;
            choose2Img.sprite = selectSprite;
            choose1Text.color = COLOR_UNSELECT;
            choose2Text.color = COLOR_SELECT;
        }
    }

    public class SettingTwoItemInterceptData
    {
        public int src;
        public int toSet;
    }
}