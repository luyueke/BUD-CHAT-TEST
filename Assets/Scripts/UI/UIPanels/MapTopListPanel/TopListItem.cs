using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;
using UI.UIWidgets;
using UI.TopList;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;

public class TopListItem : MonoBehaviour
{
    RankItem itemdata;
    public CButton ClickBt;

    public Text userName;
    public Text mapName;
    public Text fireText;
    public Text rankText;

    public Image fireImage;
    public RawImage userImage;
    public RawImage mapImage;
    public RawImage bgImage;
    public RawImage topLeftImage;


    public Image selectImage;

    public Image rankKingImage;
    public Image rankBGImage;
    public Image rankNumberBGImage;



    string fireImageName = "0FIRE";

    string fireAlxsPath = "Assets/Loadable/UI/UIPanel/MapTopListPanel/FireAtlas.spriteatlas";
    string rankAlxsPath = "Assets/Loadable/UI/UIPanel/MapTopListPanel/RankKing.spriteatlas";

    public void InitData(RankItem _itemdata)
    {
        itemdata = _itemdata;
        UpdateUI();
    }
    void UpdateUI()
    {
        userName.text = itemdata.userInfo.nickname;
        mapName.text = itemdata.mapInfo.name;
        fireText.text = itemdata.score.ToString();
        if (itemdata.score < 500) {
            fireImageName = "0FIRE";
        }
        else if(itemdata.score < 5000) {
            fireImageName = "500FIRE";
        }
        else
        {
            fireImageName = "5000FIRE";
        }

        fireImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(fireAlxsPath, fireImageName, gameObject);

        rankText.text = itemdata.rank.ToString();
        SelectImage(itemdata.rank);
        LoadImag(userImage, itemdata.userInfo.portraitUrl);

        // 设置默认地图图片
        if (mapImage != null)
        {
            var remoteRewardRawImg = mapImage.GetComponent<RemoteImageBehaviour>();
            if (remoteRewardRawImg != null)
            {
                remoteRewardRawImg.Load(itemdata.mapInfo.cover, true, (fromCache, success) =>
                {
                    if (!success)
                    {
                        LoggerUtils.LogError("无法加载图片");
                    }
                });
            }
        }
        ClickBt.onClick.AddListener(OnclickMap);
    }

    void SelectImage(int rank)
    {
        string kingname;
        string BGname;
        string NumberBGname;
        switch (rank)
        {
            case 1:
                kingname = "Rank1";
                BGname = "MapBGimage_1";
                NumberBGname = "Rank1BG";
                break;
            case 2:
                kingname = "Rank2";
                BGname = "MapBGimage_2";
                NumberBGname = "Rank2BG";
                break;
            case 3:
                kingname = "Rank3";
                BGname = "MapBGimage_3";
                NumberBGname = "Rank3BG";
                break;
            default:
                kingname = "";
                BGname = "MapBGimage_4";
                NumberBGname = "";
                break;
        }
        if (kingname == "")
        {
            rankKingImage.gameObject.SetActive(false);
        }
        else
        {
            rankKingImage.gameObject.SetActive(true);
            rankKingImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(rankAlxsPath, kingname, gameObject);
        }

        if (NumberBGname == "")
        {
            rankNumberBGImage.gameObject.SetActive(false);
        }
        else
        {
            rankNumberBGImage.gameObject.SetActive(true);
            rankNumberBGImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(rankAlxsPath, NumberBGname, gameObject);
        }

        rankBGImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(rankAlxsPath, BGname, gameObject);
    }


    void LoadImag(RawImage charactreImage ,string url)
    {
        // 确保charactreImage不为null
        if (charactreImage != null)
        {
            var remoteRewardRawImg = charactreImage.GetComponent<RemoteImageBehaviour>();
            if (remoteRewardRawImg != null)
            {
                remoteRewardRawImg.Load(
                    url,
                    true,
                    (fromCache, success) => {
                        if (!success)
                        {
                            LoggerUtils.LogError("无法加载图片");
                        }
                    }
                );
            }
        }
    }

    public void OnclickMap()
    {
        MapTopListPanel.Instan.ChangeComponent(itemdata);
    }


}
