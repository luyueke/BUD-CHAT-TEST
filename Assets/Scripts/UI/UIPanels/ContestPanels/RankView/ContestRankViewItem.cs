/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-06-12 11:48:13
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-06-14 15:55:59
 * @ Description: 排行榜Item
 */

using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData;

public class ContestRankViewItem : MonoBehaviour
{
    public Text userNameTxt;
    public Text rankTxt;
    public Text scoreTxt;
    public Image scoreImg;
    public GameObject certificationGo;
    public RemoteImageBehaviour headImg;
    public Button headBtn;

    private string m_Uid;

    private void Awake()
    {
        headBtn?.onClick.AddListener(OnHeadClick);
    }

    public void SetData(ContestEntryInfo entryInfo, bool loadHead = false)
    {
        if (entryInfo == null)
        {
            return;
        }
        m_Uid = entryInfo.creator?.uid;
        userNameTxt.text = entryInfo?.creator?.nickname;
        SetRank(entryInfo?.scoreInfo?.rank ?? 0);
        var likeNum = GameUtils.ToBudCommonNumString(entryInfo.scoreInfo?.score ?? 0);
        SetValue(likeNum);

        if (loadHead)
        {
            var portraitUrl = entryInfo?.creator?.portraitUrl;
            if (string.IsNullOrEmpty(portraitUrl))
            {
                return;
            }
            headImg.Load(portraitUrl);
        }
    }
    
    public void SetCertification(bool flag)
    {
        certificationGo.SetActive(flag);
    }

    private void SetValue(string value)
    {
        scoreTxt.text = value;
    }

    private void SetRank(int rank)
    {
        if (rank > 0)
        {
            rankTxt.text = rank.ToString();
        }
        else if (rank == 0)
        {
            rankTxt.text = "-";
        }
    }

    private void SetUIStyle(Color scoreColor)
    {
        rankTxt.color = Color.white;
        userNameTxt.color = Color.black;
        scoreTxt.color = scoreColor;
        scoreImg.color = scoreColor;
    }

    private void SetSelfUIStyle(Color rankColor, Color scoreColor)
    {
        rankTxt.color = rankColor;
        userNameTxt.color = Color.black;
        scoreTxt.color = scoreColor;
        scoreImg.color = scoreColor;
    }

    private void OnHeadClick()
    {
        if (string.IsNullOrEmpty(m_Uid))
        {
            return;
        }
        UIManager.Inst.SwapPanel(PanelId.ProfilePanel, m_Uid);
    }
}