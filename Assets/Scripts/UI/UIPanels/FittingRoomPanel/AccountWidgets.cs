using DG.Tweening;
using Game.Utils;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class AccountWidgets : MonoBehaviour
    {
        public GameObject CoinRoot;
        public GameObject BadgeRoot;
        public GameObject GemRoot;
        public GameObject SkinTicketRoot;
        private Dictionary<Transform, Transform> originalParents = new Dictionary<Transform, Transform>();
        private Dictionary<Transform, int> originalSiblingIndexes = new Dictionary<Transform, int>();

        private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
        private void Start()
        {
            Message.MessageHelper.AddListener< RectTransform, CurrencyType , int>(Message.MessageName.GetCurrencyAnimation, GetCurrencyAnimation);
        }

        private void OnDestroy()
        {
            Message.MessageHelper.RemoveListener<RectTransform, CurrencyType, int>(Message.MessageName.GetCurrencyAnimation, GetCurrencyAnimation);
        }

        public void Reset()
        {
            CoinRoot.SetActive(false);
            BadgeRoot.SetActive(false);
            GemRoot.SetActive(false);
            SkinTicketRoot.SetActive(false);
        }

        public void SetMainTabs(MainTabs.Tab tab)
        {
            Reset();
            switch (tab)
            {
                case MainTabs.Tab.Ugc:
                    SkinTicketRoot.SetActive(true);
                    GemRoot.SetActive(true);
                    break;
                case MainTabs.Tab.Bag:
                    break;
                default:
                    CoinRoot.SetActive(true);
                    BadgeRoot.SetActive(true);
                    GemRoot.SetActive(true);
                    break;
            }
        }
        //生成对应货币飞向货币栏
        public void GetCurrencyAnimation(RectTransform startPoint, CurrencyType currencyType, int number)
        {
            if (!gameObject.activeInHierarchy) {
                Debug.LogError("Not Show!");
                return;
            }
            

            RectTransform targetTransform = null;
            switch (currencyType) //找到目标
            {
                case CurrencyType.Coin:
                    CoinRoot.SetActive(true);
                    // 强制刷新布局
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>() as RectTransform);
                    targetTransform = CoinRoot.transform.Find("Layout/Icon").GetComponent<RectTransform>();
                    break;
                case CurrencyType.Badge:
                    BadgeRoot.SetActive(true);
                    // 强制刷新布局
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>() as RectTransform);
                    targetTransform = BadgeRoot.transform.Find("Layout/Icon").GetComponent<RectTransform>();
                    break;
                case CurrencyType.Gem:
                    GemRoot.SetActive(true);
                    // 强制刷新布局
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>() as RectTransform);
                    targetTransform = GemRoot.transform.Find("Layout/Icon").GetComponent<RectTransform>();
                    break;
                case CurrencyType.PinkCoin:
                    SkinTicketRoot.SetActive(true);
                    // 强制刷新布局
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>() as RectTransform);
                    targetTransform = SkinTicketRoot.transform.Find("Layout/Icon").GetComponent<RectTransform>();
                    break;
                default:
                    return;
            }
            // --- Save Original State & Prepare for Animation ---
            // Store the original parent and position if we haven't already.
            // This ensures that even with concurrent animations, we always know where to return the target.
            if (!originalParents.ContainsKey(targetTransform))
            {
                originalParents[targetTransform] = targetTransform.parent.parent.parent;
                originalSiblingIndexes[targetTransform] = targetTransform.parent.parent.GetSiblingIndex();
            }
            Transform originalParent = originalParents[targetTransform];
            int originalSiblingIndex = originalSiblingIndexes[targetTransform];


            if (targetTransform == null) {

                Debug.LogError("Can't Find Targer");
                return;
            }


            if (!originalScales.ContainsKey(targetTransform))
            {
                originalScales[targetTransform] = targetTransform.localScale;
            }
            Vector3 originalScale = originalScales[targetTransform];

            BaseWindow topWindow = UIManager.Inst.GetCurWindow();
            GameObject currencyFlyContainer = new GameObject("CurrencyFlyContainer");
            currencyFlyContainer.transform.SetParent(topWindow.transform, false);
            currencyFlyContainer.transform.SetAsLastSibling();
            targetTransform.parent.parent.SetParent(topWindow.transform, true); // Use 'true' to maintain world position.
            for (int i = 0; i < number; i++)
            {
                Vector3 randomOffset = (Vector3)Random.insideUnitCircle * 0.5f;

                GameObject currencyIconObj = new GameObject("CurrencyIcon_" + i, typeof(Image));
                currencyIconObj.transform.SetParent(currencyFlyContainer.transform, false);
                Image currencyImage = currencyIconObj.GetComponent<Image>();
                currencyImage.rectTransform.position = startPoint.position + randomOffset;
                currencyImage.rectTransform.localScale = Vector3.one * 0.8f;
                currencyImage.raycastTarget = false;

                currencyImage.sprite = PgcUtils.LoadCurrencyIcon(currencyType , currencyIconObj);

                float delay = i * 0.05f;

                // Add random rotation during the flight.
                currencyIconObj.transform.DORotate(new Vector3(0, 0, Random.Range(360, 720) * (Random.value > 0.5f ? 1f : -1f)), 0.8f, RotateMode.FastBeyond360)
                    .SetDelay(delay)
                    .SetEase(Ease.Linear); // Linear rotation looks more natural for a continuous spin.

                currencyIconObj.transform.DOMove(targetTransform.position, 0.8f)
                    .SetDelay(delay)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        // --- Rhythmic Hit Effect ---
                        // Each arriving icon retriggers the punch animation.
                        if (targetTransform != null)
                        {
                            // Kill any ongoing animation and reset scale before playing the new one.
                            targetTransform.DOKill();
                            targetTransform.localScale = originalScale;
                            DOTween.Sequence()
                                .Append(targetTransform.DOScale(originalScale * 1.5f, 0.15f))
                                .Append(targetTransform.DOScale(originalScale, 0.15f));
                        }
                        
                        // Destroy the icon immediately upon arrival.
                        if (currencyIconObj != null)
                        {
                            Destroy(currencyIconObj);
                        }
                    });
            }

            // Adjust the container's destruction delay since the icon's own delay is removed.
            DOVirtual.DelayedCall(number * 0.05f + 1.0f, () =>
            {
                // --- Restore Target Icon ---
                // Before destroying the container, return the target icon to its original parent.
                // We check if the target is still our child, to prevent issues with multiple concurrent animations.
                TimerManager.Inst.RunOnce("CoinAnimation", 1f, () =>
                {
                    if (targetTransform != null)
                    {
                        targetTransform.parent.parent.SetParent(originalParent);
                        targetTransform.parent.parent.SetSiblingIndex(originalSiblingIndex);
                        targetTransform.parent.parent.localScale = originalScale; // Reset scale to its base value.
                    }
                    targetTransform.parent.parent.gameObject.SetActive(false);
                });
                if (currencyFlyContainer != null)
                {
                    Destroy(currencyFlyContainer);
                }
            });
        }
    }
}
