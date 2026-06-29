using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class GameHallChatItem : MonoBehaviour
{
    [SerializeField] private ProfilePhotoItem profilePhotoItem;
    [SerializeField] private Transform nickBgRoot;
    [SerializeField] private SuperTextMesh name;
    [SerializeField] private SuperTextMesh name2;
    [SerializeField] private SuperTextMesh content;
    [SerializeField] private CButton copyBtn;
    [SerializeField] private Image bg;
    [SerializeField] private HorizontalLayoutGroup bgLayoutComp;
    [SerializeField] private Dictionary<string, GameObject> bubEffDict;
    private OfflineMessageItem activeData;
    private string matchCode = "";

    int Id = -1;
    GameObject NickBg;
    private void Awake()
    {
        copyBtn?.onClick.AddListener((() =>
        {
            GUIUtility.systemCopyBuffer = matchCode;
            TipPanel.ShowToast("复制成功");
        }));
    }

    public void Init(OfflineMessageItem cData)
    {
        activeData = cData;
        profilePhotoItem.InitData(cData.uid, cData?.portraitUrl, cData.avatarFrame);
        name.text = cData.nickname;
        if (name2 != null)
        {
            name2.text = cData.nickname;
        }
        copyBtn?.gameObject.SetActive(false);
        TextChatData textChatData = null;
        try {
            textChatData = JsonConvert.DeserializeObject<TextChatData>(cData.data);
        } catch (Exception e) {
            LoggerUtils.LogError(e.Message);
            LoggerUtils.LogError("data:", cData.data);
        }

        if (textChatData != null)
        {
            content.text = textChatData.message;
            if (!AccountDataManager.Inst.IsSelf(cData.uid))
            {
                string matchCode = ChatDataManager.GetValidSubstring(textChatData.message);
                if (!string.IsNullOrEmpty(matchCode))
                {
                    this.matchCode = matchCode;
                    copyBtn?.gameObject.SetActive(true);
                }
            }
        }

        SetNickNameBg(activeData.nicknameFrame);
    }

    public void AddChatContent(string value) {
        if (string.IsNullOrEmpty(value)) {
            return;
        }
        string chatMessage = content.text + value;
        content.text = chatMessage;
        if (!AccountDataManager.Inst.IsSelf(activeData.uid))
        {
            string code = ChatDataManager.GetValidSubstring(chatMessage);
            if (!string.IsNullOrEmpty(code))
            {
                matchCode = code;
                copyBtn?.gameObject.SetActive(true);
            }
        }
    }
    public void ClearBub()
    {
        if (bubEffDict != null)
        {
            foreach (var bubEff in bubEffDict)
            {
                if (bubEff.Value != null)  // 检查对象是否为空
                {
                    // 立即销毁对象
                    DestroyImmediate(bubEff.Value);
                }
            }
            bubEffDict.Clear();
        }
    }
    public void SetBubbleStyle(int bubbleId, bool isSelf = false)
    {
        var data = UserUIWidgetManager.Inst.GetHallChatBubbleData(bubbleId, this.gameObject);
        bg.sprite = data.Sp;
        bgLayoutComp.padding = data.Offset;
        if (bubEffDict==null)
        {
            bubEffDict = new Dictionary<string, GameObject>();
        }
        else
        {
            ClearBub();
        }
        if (data.CornerEffectPrefabPaths != null)
        {
            foreach (var assetPath in data.CornerEffectPrefabPaths)
            {
                if (string.IsNullOrEmpty(assetPath))
                {
                    LoggerUtils.LogError("预制体路径未指定!");
                    continue;
                }
                if (bubEffDict != null && bubEffDict.ContainsKey(assetPath))//不重复创建
                {
                    continue;
                }
                var path = UserUIWidgetManager.Inst.GetGameChatBubbleEffPath(bubbleId) + assetPath + ".prefab";
                AssetWrapper<GameObject> wrapper = Loader.Load<GameObject>(path);

                // 检查加载是否成功，并从 wrapper.request.asset 获取预制体
                if (wrapper != null && wrapper.request != null && wrapper.request.isDone && wrapper.request.result == xasset.Request.Result.Success)
                {
                    GameObject loadedPrefab = wrapper.request.asset as GameObject;

                    if (loadedPrefab != null)
                    {
                        var Obj = Instantiate(loadedPrefab, bg.transform, false);
                        bubEffDict.Add(assetPath, Obj);
                    }
                    else
                    {
                        LoggerUtils.LogError($"通过 Loader 加载成功，但 wrapper.request.asset 为空: {assetPath}");
                    }
                }
                else
                {
               //     LoggerUtils.Log("无法找到气泡特效：" + path);
                }
            }
        }
        else
        {
            ClearBub();
        }

        if (bubbleId == 0 && isSelf)
        {
            bg.color = DataUtil.DeSerializeColorByHex("#A982FF");
        }
        else
        {
            bg.color = Color.white;
        }
    }

    public void SetNickNameBg(int id)
    {
        if (Id == id)
        {
            return;
        }
        Id = id;
        if (NickBg != null)
        {
            GameObject.DestroyImmediate(NickBg.gameObject);
            NickBg = null;
        }
        var config = UserUIWidgetManager.Inst.GetNicknameData((int)Id);
        if (config != null && nickBgRoot != null && nickBgRoot.transform.childCount <= 0)
        {
            if (config.Id != 0)
            {
                var o = Loader.Load<GameObject>(config.Prefab, gameObject);
                NickBg = GameObject.Instantiate(o, nickBgRoot.transform);
                NickBg.transform.SetAsFirstSibling();
            }
            Color textColor = DataUtil.DeSerializeColorCheckHash(config.NameColor);
            name.color = textColor;
            if (name2 != null)
            {
                name2.color = textColor;
            }
            name.gameObject.SetActive(config.Id != 0);
            name2?.gameObject.SetActive(config.Id == 0);
        }
    }
}
