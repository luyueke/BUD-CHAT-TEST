using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using GameData;

public class ContestBanner : MonoBehaviour, ILowMemoryInterface
{
    [HideInInspector]
    public float scrollSpeed = 2f;  // 滚动速度
    [HideInInspector]
    public float pauseDuration = 7f;  // 停顿时长

    public RectTransform BannerRoot;
    public RectTransform BannerToggleRoot;
    public Toggle BannerTogglePrefab;
    public ContestBannerItem BannerLeft;
    public ContestBannerItem BannerCenter;
    public ContestBannerItem BannerRight;
    private List<ContestInfo> m_Infos;
    [HideInInspector]
    public int m_Index = -1;
    private bool IsDrag = false;
    private bool AutoRoll = false;
    private bool IsNear = false;
    [HideInInspector]
    public IPool texturePool;
    public Vector2 LeftPosition = new Vector2(-596, 108);
    public Vector2 CenterPosition = new Vector2(-192, 108);
    public Vector2 RightPosition = new Vector2(212, 108);
    public int Distance = 202;
    public Action OnBannerClick;
    private void Awake()
    {
        texturePool = new FIFOCachingPool(20, TextureDestoryer);
    }

    private void OnDestroy()
    {
        texturePool.Clear();
        BannerRoot.DOKill();
    }

    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            GameObject.Destroy(asUnityObject);
    }

    private Coroutine scrollCoroutine;
    private Coroutine waitCoroutine;
    private void RollNext()
    {
        if (m_Infos == null || m_Infos.Count <= 1) return;
        if (IsDrag) return;
        if (!gameObject || !gameObject.activeInHierarchy) return;
        SetIndex(m_Index + 1);
        BannerRoot.anchoredPosition = RightPosition;
        AutoRoll = true;
        BannerRoot.DOKill(true);
        BannerRoot.DOAnchorPosX(CenterPosition.x, scrollSpeed).OnComplete(() =>
        {
            AutoRoll = false;
        });
    }

    private void RollNear()
    {
        IsNear = true;
        if (BannerRoot.anchoredPosition.x < LeftPosition.x + Distance)
        {
            BannerRoot.DOKill(true);
            BannerRoot.DOAnchorPosX(LeftPosition.x, 0.3f).OnComplete(() =>
            {
                IsNear = false;
                SetIndex(m_Index + 1);
                BannerRoot.anchoredPosition = CenterPosition;
                StartRollNext();
            });
        }
        else if (BannerRoot.anchoredPosition.x > RightPosition.x - Distance)
        {
            BannerRoot.DOKill(true);
            BannerRoot.DOAnchorPosX(RightPosition.x, 0.3f).OnComplete(() =>
            {
                IsNear = false;
                SetIndex(m_Index - 1);
                BannerRoot.anchoredPosition = CenterPosition;
                StartRollNext();
            });
        }
        else
        {
            BannerRoot.DOKill(true);
            BannerRoot.DOAnchorPosX(CenterPosition.x, 0.3f).OnComplete(() =>
            {
                IsNear = false;
                StartRollNext();
            });
        }
    }

    public void ClearBanner()
    {
        ResetAll();
        texturePool?.Clear();
    }

    public void SetBanner(List<ContestInfo> infos)
    {
        ResetAll();
        m_Infos = infos;
        SetBannerToggle();
        SetBannerInfo();
        StartRollNext();
    }

    private void ResetAll()
    {
        BannerRoot.DOKill(true);
        m_Index = -1;
        IsDrag = false;
        AutoRoll = false;
        IsNear = false;
    }

    public void StartRollNext()
    {
        if (IsInvoking("RollNext")) CancelInvoke("RollNext");
        InvokeRepeating("RollNext", pauseDuration, pauseDuration);
    }

    public void BeginDrag(ContestBannerItem item, Vector2 delta)
    {
        if (m_Infos == null)
        {
            return;
        }

        if (m_Infos.Count <= 1) return;
        if (item != BannerCenter) return;
        if (AutoRoll) return;
        if (IsNear) return;
        IsDrag = true;
    }

    public void Drag(ContestBannerItem item, Vector2 delta)
    {
        if (m_Infos == null)
        {
            return;
        }
        if (item != BannerCenter) return;
        if (!IsDrag) return;
        var trans = BannerRoot.anchoredPosition;
        trans.x += delta.x;
        trans.x = Math.Clamp(trans.x, LeftPosition.x, RightPosition.x);
        BannerRoot.anchoredPosition = trans;
    }

    public void EndDrag(ContestBannerItem item, Vector2 delta)
    {
        if (m_Infos == null)
        {
            return;
        }
        if (item != BannerCenter) return;
        if (!IsDrag)
        {
            OnBannerClick?.Invoke();
            return;
        }
        IsDrag = false;
        RollNear();
    }

    public void SetBannerToggle()
    {
        if (m_Infos.Count <= 1)
        {
            BannerToggleRoot.parent.gameObject.SetActive(false);
            return;
        }
        else
        {
            BannerToggleRoot.parent.gameObject.SetActive(true);
        }

        for (int i = 0, C = Math.Max(BannerToggleRoot.childCount, m_Infos.Count); i < C; i++)
        {
            if (i < m_Infos.Count)
            {
                if (i < BannerToggleRoot.childCount)
                {
                    BannerToggleRoot.GetChild(i).gameObject.SetActive(true);
                }
                else
                {
                    Instantiate(BannerTogglePrefab, BannerToggleRoot);
                }
            }
            else
            {
                if (i < BannerToggleRoot.childCount)
                {
                    BannerToggleRoot.GetChild(i).gameObject.SetActive(false);
                }
            }
        }
    }

    public void SetBannerInfo()
    {

        if (m_Infos.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        SetIndex(0);
    }
    
    public void ChangeRawImageState(bool isRelease)
    {
        BannerCenter.ChangeRawImageState(isRelease);
        BannerLeft.ChangeRawImageState(isRelease);
        BannerRight.ChangeRawImageState(isRelease);
    }

    public virtual void SetIndex(int index)
    {
        index = GetIndex(index);
        if (m_Index == index) return;
        m_Index = index;
        var cur = GetInfo(index);
        BannerCenter.RefreshItem(cur, texturePool);
        var last = GetInfo(index - 1);
        BannerLeft.RefreshItem(last, texturePool);
        var next = GetInfo(index + 1);
        BannerRight.RefreshItem(next, texturePool);
        BannerToggleRoot.GetChild(index).GetComponent<Toggle>().isOn = true; //.Set(true);
    }

    public ContestInfo GetInfo(int index)
    {
        if (m_Infos.Count == 0) return null;
        return m_Infos[GetIndex(index)];
    }

    public int GetIndex(int index)
    {
        if (m_Infos.Count == 0) return 0;
        while (index < 0)
        {
            index += m_Infos.Count;
        }
        while (index >= m_Infos.Count)
        {
            index -= m_Infos.Count;
        }
        return index;
    }

    public void StopAllScrollCorotine()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
    }
}
