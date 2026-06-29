using Com.TheFallenGames.OSA.Util.IO;
using System.Collections;
using System.Collections.Generic;
using UI.TopList;
using UnityEngine;
using UnityEngine.UI;

public class MapTopListShowMapView : MonoBehaviour
{
    public Text rankText;
    public Text mapText;
    RankItem _rankitem;
    public Image rankNumberBGImage;
    public RawImage mapImage;

    bool isInit = false;

    string atlasPath = "Assets/Arts/UI/UIPanel/MapTopListPanel/TopList.spriteatlas";
    string rankAlxsPath = "Assets/Loadable/UI/UIPanel/MapTopListPanel/RankKing.spriteatlas";

    public Transform center;
    public GameObject userItem;

    public List<UserMapShowItem> items;

    private const int FIXED_ITEM_COUNT = 3;
    private const string DEFAULT_MAP_IMAGE = "DefaultMapImage";

    public Button ClickBittun;

    public void Awake()
    {
            ClickBittun.onClick.AddListener(OnClick);
    }

    

    void OnClick()
    {
       UIManager.Inst.SwapPanel(PanelId.AIHospitalUgcMapInfoPanel, _rankitem.mapInfo.id);
    }

    public void InitUI(RankItem rankitem)
    {

        _rankitem = rankitem;
        UpdateUI(rankitem);
        isInit = true;
        
    }

    public void UpdateUI(RankItem rankitem)
    {
        if (rankitem == null) return;

        _rankitem = rankitem;
        // 更新排名和地图名称
        rankText.text = rankitem.rank.ToString();

         mapText.text = rankitem.mapInfo?.name ?? "";
            

         SelectImage(rankitem.rank);
         // 更新用户头像

         UpdateUserAvatars();

        // 更新地图图片
        var remoteRewardRawImg = mapImage.GetComponent<RemoteImageBehaviour>();

        if (remoteRewardRawImg != null)
        {
            remoteRewardRawImg.Load(rankitem.mapInfo.cover, true, (fromCache, success) =>
            {
                if (!success)
                {
                    LoggerUtils.LogError($"无法加载地图图片: {rankitem.mapInfo.cover}");
                }
            });
        }


    }

    private void UpdateUserAvatars()
    {   
        for (int i = 0; i < FIXED_ITEM_COUNT; i++)
        {
            if (items[i] != null)
            {
                
                 items[i].Init();
                
            }
        }

        for (int i = 0; i < FIXED_ITEM_COUNT; i++)
        {
            if (items[i] != null)
            {
                if (_rankitem.mapInfo.gameSetting.aIGameConfig.hospitalNPCs != null && i < _rankitem.mapInfo.gameSetting.aIGameConfig.hospitalNPCs.Count) {
                    var npc = _rankitem.mapInfo.gameSetting.aIGameConfig.hospitalNPCs[i];
                    items[i].InitUI(npc.cover);
                }
            }
        }
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
                BGname = "MapBGimage_3";
                NumberBGname = "";
                break;
        }

        if (NumberBGname == "") {
            rankNumberBGImage.gameObject.SetActive(false);
        }
        else
        {
            rankNumberBGImage.gameObject.SetActive(true);
            rankNumberBGImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(rankAlxsPath, NumberBGname, gameObject);
        }
         
        
    }
}
