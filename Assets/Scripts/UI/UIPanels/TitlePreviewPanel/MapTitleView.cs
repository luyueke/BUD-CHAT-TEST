using DG.Tweening;
using Game.Avatar;
using GameSync.Manager;
using Message;
using Pb.Base;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
namespace MapTitle {

    public class TitleData
    {
        public FeatureItem feature;
        public string name;
    }




    public class MapTitleView : MonoBehaviour
    {
        public Text titleTxt;
        public Text nameTxt;
        public Image icon;
        public Image bgImage;
        public Image effImage;
        public CharacterData mtitleData;
        public PlayerInfo mPlayerData;

        public GameObject liziObj;
        public GameObject animationObj;
        List<TitleData> itemDatas;

        public bool isPlaying = true;
        TitleData curItem;


        public string PlayerName;
        string titleAlxs = "Assets/Loadable/UI/UIPanel/TitlePreviewPanel/TitleAlts.spriteatlas";
        object[] datas;

        public void Awake()
        {
            itemDatas = new List<TitleData>();
            isPlaying = true;
            MessageHelper.AddListener<string>(MessageName.PlayUserTitle, UpdateTitle);
        }

        void UpdateTitle(string Playeruid)
        {
            var data = ClientManager.Inst.PlayerInfosManager.GetTitleData(Playeruid);
            if (data == null)
            {
                LoggerUtils.Log("无称号信息！");
                return;
            }
            FeatureItem fitem = null;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].type == 1)
                {
                    fitem = data[i];
                    break;
                }
            }
            if(fitem == null)
            {
                LoggerUtils.Log("无称号信息！");
                return;
            }
            var title = new TitleData();
            var playerinfo = ClientManager.Inst.PlayerInfosManager.GetPlayerInfoById(Playeruid);
            if (playerinfo == null)
            {
                LoggerUtils.Log("无玩家信息！");
                return;
            }
            title.name = ClientManager.Inst.PlayerInfosManager.GetPlayerInfoById(Playeruid).Name;
            title.feature = fitem;
            itemDatas.Add(title);
        }
        private void Update()
        {
            if (!isPlaying && itemDatas.Count > 0)
            {
                curItem = itemDatas[itemDatas.Count - 1];
                itemDatas.RemoveAt(itemDatas.Count - 1);
                isPlaying = true;
                SetTitleData();
                TimerManager.Inst.RunOnce("title", 3f, ()=> {
                    isPlaying = false;
                });
            }
        }

        public void SetTitleData()
        {
            nameTxt.text = curItem.name;
            if (curItem.feature.type == 1)
            {
                string spriteName = "";
                string bgSpriteName = "";
                string effspriteName = "";
                switch (curItem.feature.id)
                {
                    case "1001":
                    case "1":
                        spriteName = "planet";
                        titleTxt.text = "紫梦守护者";
                        bgSpriteName = "PurpleBG";
                        effspriteName = "PurpleEff";
                        nameTxt.color = new Color32(255, 119, 177, 255);
                        liziObj.SetActive(true);
                        break;
                    case "1002":
                    case "2":
                        spriteName = "star";
                        titleTxt.text = "繁星守护者";
                        bgSpriteName = "BlueBG";
                        effspriteName = "BlueEff";
                        nameTxt.color = new Color32(119, 215, 255, 255);
                        break;
                    case "1003":
                    case "3":
                        spriteName = "moon";
                        titleTxt.text = "绮梦守护者";
                        bgSpriteName = "GreenBG";
                        nameTxt.color = new Color32(154, 213, 94, 255);
                        break;
                    case "1004":
                    case "4":
                        spriteName = "planet";
                        titleTxt.text = "紫梦设计师";
                        bgSpriteName = "PurpleBG";
                        effspriteName = "PurpleEff";
                        nameTxt.color = new Color32(255, 119, 177, 255);
                        liziObj.SetActive(true);
                        break;
                    case "1005":
                    case "5":
                        spriteName = "star";
                        titleTxt.text = "繁星设计师";
                        bgSpriteName = "BlueBG";
                        effspriteName = "BlueEff";
                        nameTxt.color = new Color32(119, 215, 255, 255);
                        break;
                    case "1006":
                    case "6":
                        spriteName = "moon";
                        titleTxt.text = "绮梦设计师";
                        bgSpriteName = "GreenBG";
                        nameTxt.color = new Color32(154, 213, 94, 255);
                        break;
                }
                XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(titleAlxs, spriteName, icon.gameObject, (sp) => {
                    icon.sprite = sp;
                });
                XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(titleAlxs, bgSpriteName, bgImage.gameObject, (sp) => {
                    bgImage.sprite = sp;
                });
                if (effspriteName == "")
                {
                    effImage.gameObject.SetActive(false);

                }
                else
                {
                    XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(titleAlxs, effspriteName, effImage.gameObject, (sp) =>
                    {
                        effImage.sprite = sp;
                    });
                }
                //刷一下可见度播放动画
                animationObj.SetActive(false);
                animationObj.SetActive(true);

            }
            else if(curItem.feature.type ==2)
            {

                return;
            }
        }

        void OnDestroy()
        {
            // 确保在对象销毁时删除注册事件，防止潜在问题
            MessageHelper.RemoveListener<string>(MessageName.PlayUserTitle, UpdateTitle);
        }


    }





}
