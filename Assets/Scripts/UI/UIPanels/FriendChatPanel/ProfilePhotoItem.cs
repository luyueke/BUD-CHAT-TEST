using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePhotoItem : MonoBehaviour
{
    [SerializeField] private Button photoBtn;
    [SerializeField] private HeadViewWidget headViewWidget;
    public string Uid = "";
    private void Awake()
    {
        photoBtn.onClick.AddListener(OnClickPhotoBtn);
    }

    public void InitData(string uid, string portraitUrl, int avatarFrame)
    {
        Uid = uid;

        headViewWidget.InitHeadCycle(uid, portraitUrl, avatarFrame);
    }
    public void OnClickPhotoBtn()
    {
        if (string.IsNullOrEmpty(Uid))
        {
            return;
        }
        ProfilePanel profilePanel = UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, Uid);
    }
}
