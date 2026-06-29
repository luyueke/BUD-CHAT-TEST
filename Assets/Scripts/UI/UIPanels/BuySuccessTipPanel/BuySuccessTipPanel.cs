using Com.TheFallenGames.OSA.Util.IO;
using Game.Audio;
using Game.Store;
using System.Collections;
using System.Collections.Generic;
using Game.AIResData;
using GameData.Base;
using UI.Base;
using UI.Manager;
using UI.UIPanels.CommonConfirm;
using UnityEngine;
using UnityEngine.UI;
using EventTracking;

namespace UI.UIPanels.FittingRoom
{
    public class BuySuccessTipPanel : BasePanel<BuySuccessTipPanel>
    {
        [SerializeField] private Animator anim;
        [SerializeField] private SuperTextMesh Title;
        [SerializeField] private ContentSizeFitter titleSizeFitter;
        [SerializeField] private GameObject IconTsf;
        [SerializeField] private Image Icon;
        [SerializeField] private RemoteImageBehaviour ugcIcon;
        [SerializeField] private BuySuccessAniEvent buySuccessAniEvent;
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine playAniCo = null;

        public override void OnCreate()
        {
            anim.enabled = false;
            buySuccessAniEvent.HideTextEvent = StartHideTextAni;
            buySuccessAniEvent.HidePanelEvent = OnHide;
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
            {
                //上报是否首次购买
                if (LobbyInfoManager.Inst.LobbyInfo.hasNotPurchasedSkin == 1)
                {
                    LoadEvent.ReportPopupStatus("FirstPurchase", "PaySuccessfulPurchase");
                }
                else
                {
                    LoadEvent.ReportPopupStatus("NotFirstPurchase", "PaySuccessfulPurchase");
                }
                
            }
        }

        //试衣间商品
        public void InitData(GoodsData goodsData, string content)
        {
            PlayEffect();
            switch (goodsData.GoodsType)
            {
                case GoodsType.SinglePgc:
                    SetIcon(PgcUtils.GetIconSpriteByPgcId(goodsData.Id, gameObject));
                    SetTitle(goodsData.Name, content);
                    break;
                case GoodsType.SingleUgc:
                    SetIcon(goodsData.GetFirstAsset<AssetsData>().UgcInfo.UgcInfo.cover);
                    SetTitle(goodsData.Name, content);
                    break;
                case GoodsType.BundleUgc:
                    SetIcon(goodsData.UgcBundleInfo.skinInfo.cover);
                    SetTitle(goodsData.Name, content);
                    break;
            }
        }

        //普通UGC商品
        public void InitData(UgcBaseInfo ugcInfo, string content)
        {
            PlayEffect();
            SetIcon(ugcInfo.cover);
            SetTitle(ugcInfo.name, content);
        }

        //购买Slot卡位
        public void InitData(Slot data)
        {
            PlayEffect();
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "SlotIcon", gameObject);
            SetIcon(sprite);
            Title.SetLocalText("成功购买<c=#9764FF>{0}</c>个卡位", data.slotAmount);
            CheckTextSize();
        }

        public void InitData(AIResType aiType,AIResourcePayData data)
        {
            PlayEffect();
            var spriteName = aiType ==  AIResType.AIChat ? "chat" : "game";
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, spriteName, gameObject);
            SetIcon(sprite);
            string formatContent = aiType ==  AIResType.AIChat ? "购买成功，本日对话句数上限+{0}" : "购买成功，本日游玩局数上限+{0}";
            Title.SetLocalText(formatContent, data.amount);
            CheckTextSize();
        }

        public void InitData(string content,Sprite sprite)
        {
            PlayEffect();
            if (sprite != null)
            {
                SetIcon(sprite);
            }
            SetTitle(content);
            CheckTextSize();
        }

        private void PlayEffect()
        {
            anim.enabled = true;
            anim.Play("PlayAni", 0, 0);

            StopPlayAniCo();
            playAniCo = StartCoroutine(PlayTextAlphaAni());

            AkSoundManager.Inst.PlayUIEffectSound("Play_UI_PurchasedPopup_D3");
        }

        private void OnHide()
        {
            anim.enabled = false;
            CloseSelf();
        }

        #region PlayAni
        private IEnumerator PlayTextAlphaAni()
        {
            int waitFrame = 10;
            int playFrame = 15;

            for (int i = 0; i < waitFrame; i++)
            {
                yield return null;
            }

            for (int i = 1; i < playFrame + 1; i++)
            {
                canvasGroup.alpha = 1 * i / 15f;
                yield return null;
            }

            playAniCo = null;
        }

        private void StartHideTextAni()
        {
            StopPlayAniCo();
            playAniCo = StartCoroutine(PlayHideTextAni());
        }

        private IEnumerator PlayHideTextAni()
        {
            int playFrame = 20;

            for (int i = 1; i < playFrame + 1; i++)
            {
                canvasGroup.alpha = (playFrame - i) / 20f;
                yield return null;
            }

            playAniCo = null;
        }

        private void StopPlayAniCo()
        {
            if (playAniCo != null)
            {
                StopCoroutine(playAniCo);
                playAniCo = null;
            }
        }
        #endregion

        public void SetIcon(Sprite icon)
        {
            ShowNormalIcon(true);
            Icon.sprite = icon;
        }

        public void SetIcon(string url)
        {
            ShowNormalIcon(false);
            ugcIcon.Load(url);
        }

        public void SetTitle(string itemName, string content)
        {
            Title.text = string.Format("<c=#9764FF>{0}</c> {1}", itemName, LocalizationManager.Inst.GetLocalizedText(content));

            CheckTextSize();
        }

        public void SetTitle(string content)
        {
            PlayEffect();
            IconTsf.SetActive(false);
            Title.SetLocalText(content);
            CheckTextSize();
        }

        private void ShowNormalIcon(bool isShow)
        {
            Icon.gameObject.SetActive(isShow);
            ugcIcon.gameObject.SetActive(!isShow);
        }

        private void CheckTextSize()
        {
            titleSizeFitter.enabled = true;
            RectTransform titleRt = Title.transform as RectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(titleRt);

            if (titleRt.sizeDelta.x > 1230)
            {
                titleSizeFitter.enabled = false;
                titleRt.sizeDelta = new Vector2(1230f, titleRt.sizeDelta.y);
            }
        }
    }
}
