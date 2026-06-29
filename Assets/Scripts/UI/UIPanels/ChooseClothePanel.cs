using Game.Avatar;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Network.Http;
using Network;
using Newtonsoft.Json.Linq;

public class ChooseClothePanel : BasePanel<ChooseClothePanel>
{
    [SerializeField] private Button closeBtn;
    [SerializeField] private CText tipText;
    [SerializeField] private CButton resetBtn;
    [SerializeField] private CButton changeBtn;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private AvatarOcRoleEntry Entry;

    private CharacterWrap characterWrap;
    private CharacterData originData;

    private string currentCookie = "";
    private bool isEnd = false;
    private bool isRequest = false;

    public override void OnCreate()
    {
        base.OnCreate();

        closeBtn.onClick.AddListener(OnCloseBtnClick);
        resetBtn.onClick.AddListener(OnResetBtnClick);
        changeBtn.onClick.AddListener(OnChangeBtnClick);

        Entry.pullAction = () =>
        {
            if (CanLoadMore())
            {
                LoadData(currentCookie);
            }
        };

        Entry.clickAction = OnClickItem;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        ShowCharacter();
        RefreshData();

        AvatarController.Inst.SelfStateController.EnterState(PlayerState.ChangeClothes);
    }

    private void OnCloseBtnClick()
    {
        AvatarController.Inst.SelfStateController.ExitState(PlayerState.ChangeClothes);

        CloseSelf();
    }

    private void OnResetBtnClick()
    {
        characterWrap.RefreshAvatar(originData);
    }

    private void OnChangeBtnClick()
    {
        AvatarController.Inst.SelfStateController.ExitState(PlayerState.ChangeClothes);

        AvatarController.Inst.SelfWrap.RefreshAvatar(characterWrap.ChaData);
        AccountDataManager.Inst.SyncAvatarData(characterWrap.ChaData);

        CloseSelf();

        AvatarController.Inst.SelfStateController.EnterState(PlayerState.ChangeClothesAni);
    }

    private void OnClickItem(AvatarOcData data)
    {
        var json = data.ocInfo.avatarJson;
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        CharacterData cData = CharacterData.DeserializeObject(json);
        characterWrap.RefreshAvatar(cData);

        Entry.adapter.OnSelect(data);
    }

    private void ShowCharacter()
    {
        CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;

        characterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
        characterWrap.SetParent(characterRoot, true);

        originData = characterWrap.ChaData;
    }

    private bool CanLoadMore()
    {
        if (isRequest)
        {
            return false;
        }
        return !isEnd;
    }

    public void RefreshData()
    {
        currentCookie = "";
        LoadData(currentCookie, true);
    }

    private void LoadData(string cookie, bool forceRefresh = false)
    {
        // 调用后端接口
        var jb = new JObject
        {
            ["cookie"] = cookie
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ocList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                AvatarOcListRes serverData = JsonConvert.DeserializeObject<AvatarOcListRes>(arg0);
                currentCookie = serverData.cookie;
                isEnd = serverData.isEnd == 1;
                Entry.ReloadData(serverData.list, forceRefresh);

                if (Entry.HasData)
                    tipText.gameObject.SetActive(false);
                else
                    tipText.SetLocalText("快去保存属于你的设子吧！");
            }, onFail: arg0 =>
            {
                Entry.ReloadData(new List<AvatarOcData>(), false);
            });
    }
}
