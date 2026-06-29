using System;
using Game.Store;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


    public class NewbieRewardData
    {
        public string pgcId;
    }

    public class NewbieCumulativeRechargeItem : MonoBehaviour
    {
        [SerializeField] private Image bgImg;
        [SerializeField] private Image pgcIcon;
        [SerializeField] private Image currencyIcon;
        [SerializeField] private CButton clickBtn;
        [SerializeField] private Text numText;
        [SerializeField] private GameObject ownObj;
        [SerializeField] private Image outLine; //用于选中状态
        [SerializeField] private Image ownBg; //用于选中状态
        [SerializeField] private GameObject loadingGo;
        private Action<NewbieCumulativeRechargeItem, NewbieRewardData, int> _clickAction;
        private NewbieRewardData _data;
        private int itemIndex;

        private string spriteatlasPath => RechargePanel.RechargePanelAtlas;


        public void Awake()
        {
            clickBtn.onClick.AddListener(OnItemClick);
        }

        public void Init(NewbieRewardData data, int itemIndex, string spriteName,
            Action<NewbieCumulativeRechargeItem, NewbieRewardData, int> click)
        {

            string spriteatlasPath = "Assets/Loadable/UI/UIPanel/GashaponPanel/GashaponPanelAtlas.spriteatlas";
            Sprite sp1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "empty_circle", gameObject);
            Sprite sp2 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "item_bg", gameObject);

            bgImg.sprite = sp1;
            bgImg.transform.Find("Mask").GetComponent<Image>().sprite = sp1;
            outLine.sprite = sp2;


            InitSprite(spriteName);
            _data = data;
            _clickAction = click;
            this.itemIndex = itemIndex;
            bgImg.color = DataUtil.DeSerializeColor("f3ad68");
            bool isOwned = AssetsDataManager.IsOwned(data.pgcId);
            ownObj.SetActive(isOwned);
            SetLoadingVisible(false);

       
        }

        private void InitSprite(string spriteName)
        {
            pgcIcon.gameObject.SetActive(false);
            currencyIcon.gameObject.SetActive(false);
           // spriteName = "vip_multi_prop";
            Sprite sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
            if (this != null && pgcIcon != null && sp != null)
            {
                pgcIcon.gameObject.SetActive(true);
                pgcIcon.sprite = sp;
 
            }
        }

        public NewbieRewardData GetBindData()
        {
            return _data;
        }

        public void OnItemClick()
        {
            _clickAction?.Invoke(this, _data, itemIndex);
            SetSelectStatus(true);
        }

        public void SetLoadingVisible(bool value)
        {
            loadingGo?.SetActive(value);
        }

        public void SetSelectStatus(bool isSelect)
        {
            outLine.color = DataUtil.DeSerializeColor(isSelect ? "FFD400" : "FFFFFF");
            ownBg.color = DataUtil.DeSerializeColor(isSelect ? "FFD400" : "FFFFFF");
        }
    }
