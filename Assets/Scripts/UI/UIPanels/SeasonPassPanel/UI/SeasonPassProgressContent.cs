
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassProgressContent : MonoBehaviour
{
    private Button btn_Skip;
    public Image EndIcon;
    public RectTransform ProgressScrollBar;
    private float itemSpace = 390;
    public GameObject NOT_AVAILABLE_SP;
    public GameObject AVAILABLE_SP;
    public Sprite EndIcon_NOT_AVAILABLE_SP;
    public Sprite EndIcon_AVAILABLE_SP;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";
    private List<GameObject> curIconList = new List<GameObject>();
    private float ProgressContentWidth;
    private Transform SkipConfirmPanel;

    public Action<Vector3> ConfirmSkipAction;

    private void Awake()
    {
        btn_Skip = this.transform.Find("Btn_Skip").GetComponent<Button>();
        btn_Skip.gameObject.SetActive(false);
        btn_Skip.onClick.AddListener(OnBtnSkipClick);
        SkipConfirmPanel = btn_Skip.transform.Find("InfoPanel");

        ProgressContentWidth = this.GetComponent<RectTransform>().sizeDelta.x;
    }

    public void InitProgress(List<SeasonPassItemInfo> rewardInfos, int tier)
    {
        for (int i = 0; i < curIconList.Count; i++)
        {
            GameObject.DestroyImmediate(curIconList[i]);
        }
        curIconList.Clear();

        if(rewardInfos == null)
            return;
        var rewardCount = rewardInfos.Count;
        for (int i = 0; i < rewardCount; i++)
        {
            var curSpriteName = rewardInfos[i].BudRewardStatus == BudRewardStatus.Lock
                ? NOT_AVAILABLE_SP
                : AVAILABLE_SP;
            var obj = Instantiate(curSpriteName, this.transform);
            obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(PosX(i) + itemSpace / 2, 0);
            obj.GetComponentInChildren<Text>().text = (i + 1).ToString();
            curIconList.Add(obj);
        }

        btn_Skip.transform.SetAsLastSibling();

        if (SeasonPassDataManager.Inst.FinishAllProgress(rewardInfos))
        {
            btn_Skip.gameObject.SetActive(false);
            EndIcon.sprite =  EndIcon_AVAILABLE_SP;
            ProgressScrollBar.sizeDelta = new Vector2(39000, ProgressScrollBar.sizeDelta.y);
        }
        else
        {
            EndIcon.sprite = EndIcon_NOT_AVAILABLE_SP;
            var curDay = SeasonPassDataManager.Inst.GetCurDay(rewardInfos);
            var progressWidth = PosX(tier) + itemSpace / 2;
            ProgressScrollBar.sizeDelta = new Vector2(39000 * progressWidth / ProgressContentWidth, ProgressScrollBar.sizeDelta.y);
            btn_Skip.GetComponent<RectTransform>().anchoredPosition = new Vector2(progressWidth, 0);
            btn_Skip.gameObject.SetActive(true);
        }
    }

    private float PosX(int index)
    {
        return index * itemSpace;
    }

    private void OnBtnSkipClick()
    {
        var panelPos = SkipConfirmPanel.transform.position;
        ConfirmSkipAction?.Invoke(panelPos);
    }
}
