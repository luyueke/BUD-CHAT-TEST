using System.Collections.Generic;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

public class IncubationCabinInteractNode4Manager : MonoBehaviour
{
    [SerializeField] internal ScrollRect interact_node4_scrollRect;

    private IncubationCabinPanel _panel;
    private IncubationCabinInteractNode _interactNode;
    private List<bool> _isDialogueCommandsOpenList = new List<bool>();

    internal void Init(IncubationCabinPanel panel, IncubationCabinInteractNode interactNode)
    {
        _panel = panel;
        _interactNode = interactNode;
    }

    public void Refresh()
    {
        // 当前 Node4（语音对话）逻辑为注释状态，保留空实现待后续启用
        // 原注释代码已从 IncubationCabinPanel_interact.cs 迁移至此处备用：
        // var dialogueCommandsList = CabinNetManager.Inst.GetNetCabinCharacterUgcInfo().dialogues;
        // var trans = interact_node4_scrollRect.content.transform;
        // int count = trans.childCount;
        // for (int i = 0; i < count; i++)
        // {
        //     int idx = i;
        //     if (idx >= _isDialogueCommandsOpenList.Count)
        //     {
        //         _isDialogueCommandsOpenList.Add(false);
        //     }
        //     var item = trans.GetChild(i).GetComponent<InteractNode4RoleItem>();
        //     item.Init(dialogueCommandsList[i], i, _isDialogueCommandsOpenList[i]);
        //     item.onCommonSelectBtnClick = () =>
        //     {
        //         _isDialogueCommandsOpenList[idx] = !_isDialogueCommandsOpenList[idx];
        //         GlobalFuncExtensions.RefreshLayout(interact_node4_scrollRect.content);
        //     };
        // }
    }
}
