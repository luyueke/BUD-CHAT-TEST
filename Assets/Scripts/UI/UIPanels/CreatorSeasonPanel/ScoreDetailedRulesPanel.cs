using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDetailedRulesPanel : BasePanel<ScoreDetailedRulesPanel>
{
    public Button btn_close;
    public Transform contentRoot;
    public Text score;
    public List<Toggle> togs;

    private List<CreatorUserScoreInfoData> _dataList;

    public List<Image> _progressImgs = new List<Image>();
    private bool _slotsInitialized;
    public GameObject bg;
    public Text titleTxt;

    public override void OnCreate()
    {
        if (bg != null)
        {
            bg.transform.localScale = new Vector3(0f, 0f, 0f);
            bg.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        }
        base.OnCreate();

        btn_close.onClick.AddListener(CloseSelf);
        for (int i = 0; i < togs.Count; i++)
        {
            int index = i;
            togs[i].onValueChanged.AddListener(isOn => { if (isOn) RefreshContent(index); });
        }
    }

    // 只执行一次，对 _progressImgs 做冒泡排序，使 [0] 在最前（sibling 最大）、[last] 在最后（sibling 最小）。
    // 每次只 swap 两个目标图片的位置，其他兄弟节点最终位置不变。
    private void InitProgressSlots()
    {
        if (_slotsInitialized || _progressImgs == null || _progressImgs.Count < 2) return;
        _slotsInitialized = true;

        var valid = _progressImgs.Where(img => img != null).ToList();

        bool swapped;
        do
        {
            swapped = false;
            for (int i = 0; i < valid.Count - 1; i++)
            {
                int si  = valid[i].transform.GetSiblingIndex();
                int si1 = valid[i + 1].transform.GetSiblingIndex();
                if (si >= si1) continue; // valid[i] 已经比 valid[i+1] 靠前，不需要换

                // 安全 swap：先把 valid[i] 移到 valid[i+1] 的位置，再把 valid[i+1] 移回原来的 si
                // 这两步合起来等于互换，其他节点最终位置不受影响
                valid[i].transform.SetSiblingIndex(si1);
                valid[i + 1].transform.SetSiblingIndex(si);
                swapped = true;
            }
        } while (swapped);
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length > 0 && args[0] is List<CreatorUserScoreInfoData> scoreInfoList)
        {
            int defaultIndex = args.Length > 1 && args[1] is int idx ? idx : 0;
            if (titleTxt != null)
            {
                if (args.Length > 2 && args[2] is string customTitle)
                    titleTxt.text = customTitle;
                else
                {
                    bool isHistory = args.Length > 2 && args[2] is bool h && h;
                    titleTxt.text = isHistory ? "历史创作分数细则" : "当前创作分数细则";
                }
            }
            InitPanel(scoreInfoList, defaultIndex);
        }
    }

    public void InitPanel(List<CreatorUserScoreInfoData> scoreInfoList, int defaultIndex = 0)
    {
        _dataList = scoreInfoList;
        if (togs.Count > 0)
        {
            defaultIndex = Mathf.Clamp(defaultIndex, 0, togs.Count - 1);
            for (int i = 0; i < togs.Count; i++)
                togs[i].SetIsOnWithoutNotify(i == defaultIndex);
            RefreshContent(defaultIndex);
        }
    }

    private void RefreshProgressList(List<scoreDetails> details)
    {
        if (_progressImgs == null || _progressImgs.Count == 0) return;

        InitProgressSlots();

        foreach (var img in _progressImgs)
            if (img != null) img.fillAmount = 0f;

        if (details == null) return;

        float totalMax = details.Sum(d => d.maxScore);
        if (totalMax <= 0f) return;

        // 按 type 字段建立查找表，避免 details 顺序与 _progressImgs 下标不一致导致错位
        // _progressImgs[i] 对应 type=i（"0"=base "1"=创作分 "2"=活动 "3"=商城）
        var scoreByType = new Dictionary<string, float>();
        foreach (var d in details)
            if (float.TryParse(d.score, out float s))
                scoreByType[d.type] = s;

        float cumulative = 0f;
        for (int i = 0; i < _progressImgs.Count; i++)
        {
            var img = _progressImgs[i];
            if (img == null) continue;
            float score = scoreByType.TryGetValue(i.ToString(), out float v) ? v : 0f;
            cumulative += score / totalMax;
            img.fillAmount = Mathf.Clamp01(cumulative);
        }
    }

    private void RefreshContent(int categoryIndex)
    {
        var data = _dataList?.Find(d => d.category == categoryIndex);
        var details = data?.scoreDetails;

        RefreshProgressList(details);

        float total = details?.Sum(d => float.TryParse(d.score, out float s) ? s : 0f) ?? 0f;
        if (score != null) score.text = $"积分：{(int)total}";

        if (details == null) return;

        for (int i = 0; i < details.Count; i++)
        {
            var detail = details[i];
            var itemRoot = contentRoot.Find("scoreDetails_" + detail.type);
            if (itemRoot == null) continue;
            var scoreText = GameObjectEx.FindComponentByName<Text>(itemRoot, "score");
            var maxScoreText = GameObjectEx.FindComponentByName<Text>(itemRoot, "maxScore");
            var progressImg = GameObjectEx.FindComponentByName<Image>(itemRoot, "progress");

            if (scoreText != null) scoreText.text = detail.score;
            if (maxScoreText != null) maxScoreText.text = "/" + detail.maxScore.ToString();

            float current = float.TryParse(detail.score, out float s) ? s : 0f;
            float fill = detail.maxScore > 0 ? Mathf.Clamp01(current / detail.maxScore) : 0f;
            if (progressImg != null) progressImg.fillAmount = fill;

            if (detail.type == "3")
            {
                GameObjectEx.FindComponentByName<Text>(itemRoot, "txt_map").text = "商城分";
                GameObjectEx.FindComponentByName<Text>(itemRoot, "txt_map_play").text = "发布作品数";
                GameObjectEx.FindComponentByName<Text>(itemRoot, "txt_all").text = "总销售额";
                if (categoryIndex == 4)
                {
                    GameObjectEx.FindComponentByName<Text>(itemRoot, "txt_map").text = "游玩分";
                    GameObjectEx.FindComponentByName<Text>(itemRoot, "txt_all").text = "总游玩数";
                    GameObjectEx.FindComponentByName<Text>(itemRoot, "txt_map_play").text = "发布地图数";
                }
                if (categoryIndex == 1 || categoryIndex == 2 || categoryIndex == 3)
                {
                    GameObjectEx.FindComponentByName<Text>(itemRoot, "txt_all").text = "总销售额";
                }
                if(categoryIndex == 5)
                {
                   GameObjectEx.FindComponentByName<Text>(itemRoot, "txt_all").text = "总销量"; 
                }
                var showInfoBtn = GameObjectEx.FindComponentByName<Button>(itemRoot, "btn_showInfo");
                showInfoBtn.onClick.RemoveAllListeners();
                showInfoBtn.onClick.AddListener(() =>
                {
                    var showInfo = GameObjectEx.FindChildByName(itemRoot, "showInfo").gameObject;
                    bool open = !showInfo.activeSelf;
                    showInfo.SetActive(open);
                    float targetZ = open ? 180f : 0f;
                    showInfoBtn.transform.DOLocalRotate(new Vector3(0f, 0f, targetZ), 0.25f).SetEase(Ease.OutQuad);

                    float targetH = open ? 363f : 264f;
                    var rt = showInfo.GetComponent<RectTransform>();
                    rt.DOSizeDelta(new Vector2(rt.sizeDelta.x, targetH), 0.25f).SetEase(Ease.OutQuad);
                });
                GameObjectEx.FindComponentByName<Text>(itemRoot, "publishNum").text = data.publishNum.ToString();
                GameObjectEx.FindComponentByName<Text>(itemRoot, "interactNum").text = data.interactNum.ToString();
            }
        }
    }
}
