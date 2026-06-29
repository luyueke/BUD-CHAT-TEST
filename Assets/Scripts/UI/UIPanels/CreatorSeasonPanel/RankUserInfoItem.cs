using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class RankUserInfoItem : MonoBehaviour
{
    [SerializeField] private SuperTextMesh UserNameText;
    [SerializeField] private Text UserScoreText;
    [SerializeField] private GameObject NoneTips;
    [SerializeField] private HeadViewWidget HeadInfo;
    [SerializeField] private CButton UserBtn;

    private UserRankData _userRankData;

    private void Awake()
    {
        UserBtn.onClick.AddListener(OnUserBtnClick);
    }

    private void OnUserBtnClick()
    {
        var userInfo = _userRankData?.userInfo;
        if (userInfo == null)
        {
            return;
        }

        string uid = userInfo.uid;
        if (string.IsNullOrEmpty(uid))
        {
            return;
        }

        UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, uid);
    }

    public void SetData(UserRankData userRankData)
    {
        _userRankData = userRankData;
        if (userRankData == null)
        {
            NoneTips.SetActive(true);
            HeadInfo.gameObject.SetActive(false);
            UserNameText.SetText("");
            UserScoreText.text = "";
            return;
        }
        NoneTips.SetActive(false);
        HeadInfo.gameObject.SetActive(true);
        UserNameText.SetText(userRankData.userInfo.nickname);
        UserScoreText.text = userRankData.userRank.score.ToString();
        if (HeadInfo != null && userRankData.userInfo != null)
        {
            HeadInfo.InitHeadCycle(userRankData.userInfo);
        }
    }
}
