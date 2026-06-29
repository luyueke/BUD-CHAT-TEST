using System;
using System.Linq;
using Es;
using UnityEngine;
using UnityEngine.UI;
using UI.UIWidgets;
public class InteractNode4RoleItem : MonoBehaviour
{
    public GameObject openGo;
    public GameObject closeGo;

    public GameObject hori_contentGo;
    public GameObject itemPrefabGo;

    public Text common_titleTxt;
    public Button common_selectBtn;
    bool _isOpen = false;

    int _idx = -1;
    public Action onCommonSelectBtnClick; //选择事件

    dialogueCommands _dialogueCommands;
    void Awake()
    {
        common_selectBtn.onClick.AddListener(OnCommonSelectBtnClick);
    }

    public void Init(dialogueCommands dialogueCommands, int idx, bool isOpen)
    {
        _dialogueCommands = dialogueCommands;
        _idx = idx;
        _isOpen = isOpen;
        openGo.SetActive(isOpen);
        closeGo.SetActive(!isOpen);

        int count = hori_contentGo.transform.childCount;
        for (int i = 0; i < count; i++)
        {
            var itemGo = hori_contentGo.transform.GetChild(i).gameObject;
            Destroy(itemGo);
        }


        itemPrefabGo.SetActive(false);
        var item = Instantiate(itemPrefabGo, hori_contentGo.transform);
        item.SetActive(true);
        item.GetComponent<InteractNode4BottomRoleItem>().SetData(0, "");
        for (int i = 0; i < dialogueCommands.emoteIds.Count; i++)
        {
            var itemGo = Instantiate(itemPrefabGo, hori_contentGo.transform);
            itemGo.SetActive(true);
            itemGo.GetComponent<InteractNode4BottomRoleItem>().SetData(1, dialogueCommands.emoteIds[i]);
        }
    }



    void OnOpenLayer3DeleteBtnClick()
    {
        // var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == _voiceCommands.emoteId);
        // if (emoAniDataList == null || emoAniDataList.Count == 0)
        // {
        //     return;
        // }
        // var name = emoAniDataList[0].name;
        // string tipStr = string.Format("确认删除【{0}】吗？", name);
        // //TODO:
        // CommonConfirmWithTitlePanel commonConfirmPanel =
        //    UIManager.Inst.OpenPanel<CommonConfirmWithTitlePanel>(PanelId.CommonConfirmWithTitlePanel);
        // commonConfirmPanel.SetLocalText("提示", tipStr, "确认", "取消");
        // commonConfirmPanel.SetOnClickAction(() =>
        // {
        //     //删除
        //     CabinNetManager.Inst.DeleteVoiceCommands(_idx, (isSuccess) =>
        //     {
        //         if (isSuccess)
        //         {
        //             LoggerUtils.Log("删除口令互动成功");
        //             CabinNetManager.Inst.RefreshInteractContent();
        //         }
        //         else
        //         {
        //             LoggerUtils.LogError("删除口令互动失败");
        //         }
        //     });
        // }, () =>
        // {
        //     //取消
        // });

        // commonConfirmPanel.HideCloseBtn();
    }

    void OnCommonSelectBtnClick()
    {
        _isOpen = !_isOpen;
        openGo.SetActive(_isOpen);
        closeGo.SetActive(!_isOpen);
        onCommonSelectBtnClick?.Invoke();
    }

}
