using System.Text;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class RecommendItem : MonoBehaviour
{
    public RawImage mapCover;
    public BUD_Text mapName;
    public BUD_Text userName;
    public Text LikeNum;

    public Button button;
    public RemoteImageBehaviour iconRemoteImageBehaviour;
    private RecommendData data;

    public void Init(RecommendData d)
    {
        data = d;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        var subStr = GameUtils.SubStringByBytes(data.ugcInfo.name, 70, Encoding.Unicode);
        mapName.text = subStr;
        userName.text = data.creatorInfo.nickname;
        LikeNum.text = data.interactInfo.liked.ToString();
        string mapCoverUrl = data.ugcInfo.cover;
        iconRemoteImageBehaviour.Load(mapCoverUrl, true, null);
        gameObject.SetActive(true);
    }

    private void OnClick()
    {
        UIManager.Inst.SwapPanel(PanelId.MapDetailPanel,data.ugcInfo, data.creatorInfo, data.interactInfo, mapCover.texture);
        // mapDetailPanel.IniUIWithTexture(data.ugcInfo.id, mapCover.texture);
    }
}
